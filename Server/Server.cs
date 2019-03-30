namespace Server
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;

    /// <summary>
    ///     Thread safe singleton
    /// </summary>
    internal class Server
    {
        private const int ConnectionServicePort = 27000;

        private const int DrawingCode = 1;

        private const int MessageCode = 0;

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

        public void Run()
        {
            if (!this.InitializeService())
                return;

            Console.WriteLine("Server started...");

            this.HandleDataReceived();
        }

        /// <summary>
        ///     Adds given client IP end point to clients list.
        /// </summary>
        /// <param name="clientEndPoint"> IP end point of the client. </param>
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

        /// <summary>
        ///     Handle all the data send via UDP to the connectionServicePort.
        /// </summary>
        private void HandleDataReceived()
        {
            while (true)
            {
                // Read the packet and the IP end point that send it
                var clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                var data = this.connectionService.Receive(ref clientEndPoint);

                if (data[0] == MessageCode)
                    this.HandleMessage(clientEndPoint, Encoding.ASCII.GetString(data, 1, data.Length - 1));
                else if (data[0] == DrawingCode)
                    this.HandleDrawingData(clientEndPoint, data.Skip(1).ToArray());
                else
                    Console.WriteLine($"Packet from {clientEndPoint} starts with unrecognized code: {data[0]}");
            }
        }

        /// <summary>
        ///     Removes given client IP end point from clients list.
        /// </summary>
        /// <param name="clientEndPoint"> IP end point of the client. </param>
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

        /// <summary>
        ///     Handles packets received which start with drawing code.
        /// </summary>
        /// <param name="client"> IP end point of the client. </param>
        /// <param name="data"> Packet received (drawing code is skipped). </param>
        private void HandleDrawingData(IPEndPoint client, byte[] data)
        {
            // To ensure that the client is connected even if the connection packet was lost due to UDP flaw.
            if (!this.clients.Contains(client))
                this.HandleConnectionAttempt(client);
            foreach (var ipEndPoint in this.clients.Where(v => v != client))
                this.connectionService.SendAsync(data, data.Length, ipEndPoint);
        }

        /// <summary>
        ///     Handles packets received which start with message code.
        /// </summary>
        /// <param name="clientEndPoint"> IP end point of the client. </param>
        /// <param name="msg"> Message received (message code is skipped). </param>
        private void HandleMessage(IPEndPoint clientEndPoint, string msg)
        {
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

        /// <summary>
        ///     Tries to initialize UDP client.
        /// </summary>
        /// <returns> Variable determining the success of the initialization. </returns>
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
    }
}