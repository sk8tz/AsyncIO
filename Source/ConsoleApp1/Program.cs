using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AsyncIO;

namespace ConsoleApp1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CompletionPort completionPort = CompletionPort.Create();

            AutoResetEvent listenerEvent = new AutoResetEvent(false);
            AutoResetEvent clientEvent = new AutoResetEvent(false);
            AutoResetEvent serverEvent = new AutoResetEvent(false);

            AsyncSocket listener = AsyncSocket.Create(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);
            completionPort.AssociateSocket(listener, listenerEvent);

            AsyncSocket server = AsyncSocket.Create(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);
            completionPort.AssociateSocket(server, serverEvent);

            AsyncSocket client = AsyncSocket.Create(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);
            completionPort.AssociateSocket(client, clientEvent);

            Task.Factory.StartNew(() =>
            {
                CompletionStatus completionStatus;

                while (true)
                {
                    var result = completionPort.GetQueuedCompletionStatus(-1, out completionStatus);

                    if (result)
                    {
                        Console.WriteLine("{0} {1} {2}", completionStatus.SocketError,
                            completionStatus.OperationType, completionStatus.BytesTransferred);

                        if (completionStatus.State != null)
                        {
                            AutoResetEvent resetEvent = (AutoResetEvent)completionStatus.State;
                            resetEvent.Set();
                        }
                    }
                }
            });

            listener.Bind(IPAddress.Any, 5555);
            listener.Listen(1);

            client.Connect("localhost", 5555);

            listener.Accept(server);


            listenerEvent.WaitOne();
            clientEvent.WaitOne();

            byte[] sendBuffer = new byte[1] { 2 };
            byte[] recvBuffer = new byte[1];

            client.Send(sendBuffer);
            server.Receive(recvBuffer);

            clientEvent.WaitOne();
            serverEvent.WaitOne();

            server.Dispose();
            client.Dispose();
            Console.ReadLine();
        }
    }
}
