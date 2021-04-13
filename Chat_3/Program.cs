using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chat_3
{
    class Program
    {
        private static string serverIp = "127.0.0.1";
        private static Socket mainSocket;
        private static bool isConnecting = false;
        private static bool isDelivered = false;
        private static uint counter = 0;

        static void Main()
        {
            Console.Write("Enter the port to receive messages: ");
            int localPort = Int32.Parse(Console.ReadLine());
            Console.Write("Enter the port for sending messages: ");
            int remotePort = Int32.Parse(Console.ReadLine());
            Console.WriteLine("Waiting for connection");
            mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                EndPoint remotePoint = new IPEndPoint(IPAddress.Parse(serverIp), remotePort);
                IPEndPoint localIP = new IPEndPoint(IPAddress.Parse(serverIp), localPort);
                mainSocket.Bind(localIP);
                Connect(remotePoint);

                Task listeningTask = new Task(Listen);
                listeningTask.Start();

                while (true)
                {
                    Send(Console.ReadLine(), remotePoint);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Main: " + ex.Message);
            }
        }

        private static void Send(string message, EndPoint remotePoint)
        {
            counter++;
            isDelivered = true;
            var data = Encoding.Unicode.GetBytes(message + "|" + counter);
            while (isDelivered)
            {
                mainSocket.SendTo(data, remotePoint);
            }
        }

        private static void Connect(EndPoint remotePoint)
        {
            byte[] data = Encoding.Unicode.GetBytes("connect");
            byte[] buffer = new byte[data.Length];
            var recieveArgs = new SocketAsyncEventArgs() { RemoteEndPoint = remotePoint };
            recieveArgs.SetBuffer(buffer, 0, buffer.Length);
            while (!isConnecting)
            {
                try
                {
                    mainSocket.SendTo(data, remotePoint);
                    mainSocket.ReceiveAsync(recieveArgs);
                    if (Encoding.UTF8.GetString(buffer).Equals(Encoding.UTF8.GetString(Encoding.Unicode.GetBytes("connect"))))
                    {
                        mainSocket.SendTo(data, remotePoint);
                        isConnecting = true;
                        Console.WriteLine("Connection established");
                    }
                }
                catch (Exception e)
                { 
                }
            }
        }


        private static void Listen()
        {
            try
            {
                while (true)
                {
                    StringBuilder builder = new StringBuilder();
                    int bytes = 0;
                    byte[] data = new byte[256];
                    EndPoint remoteIp = new IPEndPoint(IPAddress.Any, 0);
                    do
                    {
                        bytes = mainSocket.ReceiveFrom(data, ref remoteIp);
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    }
                    while (mainSocket.Available > 0);
                    IPEndPoint remoteFullIp = remoteIp as IPEndPoint;

                    var message = builder.ToString().Split('|');
                    if (message.Length == 1)
                    {
                        if (message[0] == "connect")
                        {
                            isConnecting = true;
                            Console.WriteLine("Connection established");
                        }
                        else if (Int32.Parse(message[0]) == counter)
                        {
                            isDelivered = false;
                        }
                    }
                    else if (message.Length == 2)
                    {
                        var feedback = Encoding.Unicode.GetBytes(message[1]);
                        mainSocket.SendTo(feedback, remoteIp);
                        Console.WriteLine($"{message[1]}: {message[0]}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Listen: " + ex.Message);
            }
        }
    }
}
