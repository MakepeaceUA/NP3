using System.Net.Sockets;
using System.Net;
using System.Text;

namespace ConsoleApp33
{
    internal class Program
    {
        class CurrencyServer
        {
            private static Dictionary<string, double> Rates = new Dictionary<string, double>
    {
        {"USD/EU", 0.90},
        {"EU/USD", 1.00}
    };
            private static object LogLock = new object();
            private static Dictionary<string, (int count, DateTime resetTime)> ClientRequests = new Dictionary<string, (int, DateTime)>();
            private static int MaxRequests = 2;
            private static TimeSpan BanDuration = TimeSpan.FromMinutes(1);

            static async Task Main()
            {
                IPEndPoint EndPoint = new IPEndPoint(IPAddress.Any, 5000);
                Socket ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                ServerSocket.Bind(EndPoint);
                ServerSocket.Listen(10);
                Console.WriteLine("Сервер запущен...");

                while (true)
                {
                    Socket clientSocket = await ServerSocket.AcceptAsync();
                    Task.Run(() => HandleClient(clientSocket));
                }
            }

            private static async Task HandleClient(Socket ClientSocket)
            {
                IPEndPoint EndPoint = (IPEndPoint)ClientSocket.RemoteEndPoint;
                string ClientIP = EndPoint.Address.ToString();

                lock (LogLock)
                {
                    Console.WriteLine($"{EndPoint} подключился.");
                    File.AppendAllText("server_log.txt", $"{EndPoint} подключился.\n");
                }

                byte[] buffer = new byte[1024];
                try
                {
                    while (true)
                    {
                        int ByteRead = await ClientSocket.ReceiveAsync(buffer, SocketFlags.None);
                        if (ByteRead == 0) 
                        {
                            break;
                        }

                        string request = Encoding.UTF8.GetString(buffer, 0, ByteRead).Trim();
                        if (request.ToLower() == "exit")
                        {
                            break;
                        }
                        lock (ClientRequests)
                        {
                            if (ClientRequests.TryGetValue(ClientIP, out var data))
                            {
                                if (DateTime.Now < data.resetTime && data.count >= MaxRequests)
                                {
                                    string Limit = "Превышен лимит запросов. Попробуйте позже.";
                                    byte[] LimitData = Encoding.UTF8.GetBytes(Limit);
                                    ClientSocket.Send(LimitData);
                                    break;
                                }
                                else if (DateTime.Now >= data.resetTime)
                                {
                                    ClientRequests[ClientIP] = (1, DateTime.Now.Add(BanDuration));
                                }
                                else
                                {
                                    ClientRequests[ClientIP] = (data.count + 1, data.resetTime);
                                }
                            }
                            else
                            {
                                ClientRequests[ClientIP] = (1, DateTime.Now.Add(BanDuration));
                            }
                        }

                        string response = Rates.TryGetValue(request.ToUpper(), out double rate) ? rate.ToString("F2") : "Error.";
                        byte[] ResponseData = Encoding.UTF8.GetBytes(response);
                        await ClientSocket.SendAsync(ResponseData, SocketFlags.None);

                        lock (LogLock)
                        {
                            Console.WriteLine($"{EndPoint}: {request} -> {response}");
                            File.AppendAllText("server_log.txt", $"{EndPoint}: {request} -> {response}\n");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка: {ex.Message}");
                }
                finally
                {
                    lock (LogLock)
                    {
                        Console.WriteLine($"{EndPoint} отключился.");
                        File.AppendAllText("server_log.txt", $"{EndPoint} отключился.\n");
                    }
                    ClientSocket.Close();
                }
            }
        }
    }
}