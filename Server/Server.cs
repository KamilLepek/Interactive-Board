using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    /// <summary>
    ///     Thread safe singleton
    /// </summary>
    internal class Server
    {
        private const int ConnectionServicePort = 27000;

        private static readonly Server ServerInstance = new Server();

        private readonly List<IPEndPoint> clients = new List<IPEndPoint>();

        private UdpClient connectionService;

        private Server()
        {
        }

        public static Server GetInstance()
        {
            return ServerInstance;
        }

        /// <summary>
        ///     Handle all the data send via UDP to the connectionServicePort.
        /// </summary>
        private void HandleClientsConnections()
        {
            while (true)
            {
                // Read the message and the IP end point that send it
                var clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                var data = this.connectionService.Receive(ref clientEndPoint);
                string msg = Encoding.ASCII.GetString(data, 0, data.Length);

                switch (msg)
                {
                    case "Connect":
                        this.HandleConnectionAttempt(clientEndPoint);
                        break;
                    case "Disconnect":
                        this.HandleDisconnectionAttempt(clientEndPoint);
                        break;
                    default:
                        Console.WriteLine($"Wrong message received: {msg}");
                        break;
                }
            }
        }

        private void HandleConnectionAttempt(IPEndPoint clientEndPoint)
        {
            if (this.clients.Contains(clientEndPoint))
            {
                Console.WriteLine($"Connection message send from {clientEndPoint}. This user is already connected!");
                return;
            }

            // Add client to list
            this.clients.Add(clientEndPoint);

            Console.WriteLine($"{clientEndPoint} connected!");
        }

        private void HandleDisconnectionAttempt(IPEndPoint clientEndPoint)
        {
            if (!this.clients.Contains(clientEndPoint))
            {
                Console.WriteLine($"Disconnection message send from {clientEndPoint}. This user wasn't connected!");
                return;
            }
            this.clients.Remove(clientEndPoint);
            Console.WriteLine($"{clientEndPoint} disconnected!");
        }

        private bool InitializeService()
        {
            try
            {
                this.connectionService = new UdpClient(new IPEndPoint(IPAddress.Any, ConnectionServicePort));
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize udp service. {ex.Message}");
                return false;
            }
        }

        public void Run()
        {
            Console.WriteLine("Server started...");

            if (!this.InitializeService())
                return;

            this.HandleClientsConnections();
        }
    }
}