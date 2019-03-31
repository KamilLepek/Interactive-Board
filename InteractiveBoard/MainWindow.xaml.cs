namespace InteractiveBoard
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Shapes;

    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string ConnectionMessage = "Connect";

        private const string DisconnectionMessage = "Disconnect";

        private const int DrawingCode = 1;

        private const int MessageCode = 0;

        private readonly UdpClient udpClient = new UdpClient();

        private bool connected;

        private byte[] currentColor;

        private Point currentCursorPosition;

        private IPEndPoint serverEndPoint;

        public MainWindow()
        {
            this.InitializeComponent();
            this.currentColor = new byte[] {0, 0, 0};
        }

        /// <summary>
        ///     Sends a connection packet to the server
        /// </summary>
        private void Connect()
        {
            (bool result, IPAddress ip, int port) = this.ValidateConnectionInfo();
            if (!result)
            {
                MessageBox.Show("Invalid Server address!");
                return;
            }

            this.serverEndPoint = new IPEndPoint(ip, port);
            this.udpClient.Connect(this.serverEndPoint);
            var stringData = Encoding.ASCII.GetBytes(ConnectionMessage);
            var data = new byte[stringData.Length + 1];
            stringData.CopyTo(data, 1);
            data[0] = MessageCode;
            this.udpClient.Send(data, data.Length);

            this.NegateConnectionFlags();
            this.ReceiveData();
        }

        private void ConnectionButtonClick(object sender, RoutedEventArgs e)
        {
            this.Connect();
        }

        /// <summary>
        ///     Sends disconnection packet to the server
        /// </summary>
        private void Disconnect()
        {
            var stringData = Encoding.ASCII.GetBytes(DisconnectionMessage);
            var data = new byte[stringData.Length + 1];
            stringData.CopyTo(data, 1);
            data[0] = MessageCode;
            this.udpClient.Send(data, data.Length);
            this.NegateConnectionFlags();
        }

        private void DisconnectionButtonClick(object sender, RoutedEventArgs e)
        {
            this.Disconnect();
        }

        private void DrawLine(byte[] color, double[] points)
        {
            var line = new Line
                           {
                               Stroke = new SolidColorBrush(Color.FromRgb(color[0], color[1], color[2])),
                               X1 = points[0],
                               Y1 = points[1],
                               X2 = points[2],
                               Y2 = points[3]
                           };

            this.Paint.Children.Add(line);
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            if (this.connected)
                this.Disconnect();
        }

        /// <summary>
        ///     Changes the state of the app to the opposite.
        ///     Either from connected to disconnected or from disconnected to connected.
        /// </summary>
        private void NegateConnectionFlags()
        {
            this.connected = !this.connected;
            this.ConnectButton.IsEnabled = !this.ConnectButton.IsEnabled;
            this.ServerTextBox.IsEnabled = !this.ServerTextBox.IsEnabled;
            this.PortTextBox.IsEnabled = !this.PortTextBox.IsEnabled;
            this.DisconnectButton.IsEnabled = !this.DisconnectButton.IsEnabled;
        }

        private void Paint_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.currentCursorPosition = e.GetPosition(this.Paint);
        }

        private void Paint_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            double[] points =
                {
                    this.currentCursorPosition.X,
                    this.currentCursorPosition.Y,
                    e.GetPosition(this.Paint).X,
                    e.GetPosition(this.Paint).Y
                };

            this.DrawLine(this.currentColor, points);

            if (this.connected)
                this.SendDrawingData(this.currentColor, points);

            this.currentCursorPosition = e.GetPosition(this.Paint);
        }

        /// <summary>
        ///     Starts a new task which is receiving data.
        /// </summary>
        private void ReceiveData()
        {
            Task.Run(
                () =>
                    {
                        var errorCount = 0; // Counts consecutive deserialization errors.
                        while (true)
                        {
                            if (!this.connected)
                            {
                                Thread.Sleep(TimeSpan.FromSeconds(1));
                                continue;
                            }

                            var sender = new IPEndPoint(IPAddress.Any, 0);
                            var data = this.udpClient.Receive(ref sender);

                            // Accept data from server only
                            if (!sender.Address.Equals(this.serverEndPoint.Address))
                                continue;

                            try
                            {
                                DrawingPacket packet;
                                IFormatter formatter = new BinaryFormatter();
                                using (var stream = new MemoryStream(data))
                                {
                                    packet = (DrawingPacket)formatter.Deserialize(stream);
                                }

                                // To invoke outside of STA thread
                                this.Dispatcher.BeginInvoke(
                                    (Action)(() => this.DrawLine(packet.Color, packet.Points)));
                                errorCount = 0;
                            }
                            catch (Exception)
                            {
                                if (errorCount++ > 50)
                                {
                                    MessageBox.Show("Too many errors in received data!");
                                    this.Close();
                                }
                            }
                        }
                    });
        }

        /// <summary>
        ///     Sends drawing data to the server.
        /// </summary>
        /// <param name="color"> Color of the drawn line. </param>
        /// <param name="points"> Points of the drawn line. </param>
        private void SendDrawingData(byte[] color, double[] points)
        {
            byte[] data;
            var drawingPacket = new DrawingPacket(points, color);
            IFormatter formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, drawingPacket);
                data = stream.ToArray();
            }

            // Add drawing code and send
            var packet = new byte[data.Length + 1];
            packet[0] = DrawingCode;
            data.CopyTo(packet, 1);
            this.udpClient.SendAsync(packet, packet.Length);
        }

        /// <summary>
        ///     Update current brush color upon sliders change.
        /// </summary>
        private void UpdateColor(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.currentColor = new byte[]
                {(byte)this.RSlider.Value, (byte)this.GSlider.Value, (byte)this.BSlider.Value};
        }

        /// <summary>
        ///     Validates whether given connection info is a valid IP Address and port.
        /// </summary>
        /// <returns> Variable determining the success of validation. </returns>
        private (bool, IPAddress, int) ValidateConnectionInfo()
        {
            var ip = IPAddress.Any;
            var result = int.TryParse(this.PortTextBox.Text, out var port) &&
                         IPAddress.TryParse(this.ServerTextBox.Text, out ip);
            return (result, ip, port);
        }
    }

    [Serializable]
    internal struct DrawingPacket
    {
        public double[] Points;

        public byte[] Color;

        public DrawingPacket(double[] points, byte[] color)
        {
            this.Points = new double[4];
            points.CopyTo(this.Points, 0);
            this.Color = new byte[3];
            color.CopyTo(this.Color, 0);
        }
    }
}