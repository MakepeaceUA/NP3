using System.Net.Sockets;
using System.Net;
using System.Text;

namespace ConsoleApp29
{
    internal class Program
    {
        class CurrencyClient
        {
            static void Main()
            {
                try
                {
                    IPEndPoint EndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000);
                    Socket ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    ClientSocket.Connect(EndPoint);
                    Console.WriteLine("Клиент подключился к серверу. Введите валютную пару (USD EURO) или 'exit' для выхода.");

                    while (true)
                    {
                        Console.Write("-> ");
                        string message = Console.ReadLine()?.Trim();
                        if (string.IsNullOrEmpty(message))
                        {
                            continue;
                        } 
                        message = message.Replace(" ", "/");

                        byte[] data = Encoding.UTF8.GetBytes(message);
                        ClientSocket.Send(data);

                        if (message.ToLower() == "exit")
                        { 
                            break; 
                        }

                        byte[] buffer = new byte[1024];
                        int ByteRead = ClientSocket.Receive(buffer);
                        string response = Encoding.UTF8.GetString(buffer, 0, ByteRead);

                        Console.WriteLine($"Сервер: {response}");
                    }

                    ClientSocket.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }
}