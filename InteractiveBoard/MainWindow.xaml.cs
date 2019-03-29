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

namespace InteractiveBoard
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private const int MessageCode = 0;

        private const int DrawingCode = 1;

        private readonly UdpClient udpClient = new UdpClient();

        private Point currentPosition;

        private Color currentColor;

        private bool connected;

        public MainWindow()
        {
            this.InitializeComponent();
            this.currentColor = Colors.Black;
        }

        private void Connect()
        {
            (bool result, string ip, int port) = this.ValidateConnectionInfo();
            if (!result)
            {
                MessageBox.Show("Invalid Server address!");
                return;
            }

            this.udpClient.Connect(ip, port);
            const string msg = "Connect";
            var stringData = Encoding.ASCII.GetBytes(msg);
            var data = new byte[stringData.Length + 1];
            stringData.CopyTo(data, 1);
            data[0] = MessageCode;
            this.udpClient.SendAsync(data, data.Length); // TODO: do some response from server to make sure
            this.connected = true;
            this.ConnectButton.IsEnabled = false;
            this.ServerTextBox.IsEnabled = false;
            this.PortTextBox.IsEnabled = false;
            this.DisconnectButton.IsEnabled = true;
            this.ReceiveData();
        }

        private void Disconnect()
        {
            const string msg = "Disconnect";
            var stringData = Encoding.ASCII.GetBytes(msg);
            byte[] data = new byte[stringData.Length + 1];
            stringData.CopyTo(data, 1);
            data[0] = MessageCode;
            this.udpClient.SendAsync(data, data.Length);
            this.connected = false;
            this.ConnectButton.IsEnabled = true;
            this.ServerTextBox.IsEnabled = true;
            this.PortTextBox.IsEnabled = true;
            this.DisconnectButton.IsEnabled = false;
        }

        private void ConnectionButtonClick(object sender, RoutedEventArgs e)
        {
            this.Connect();
        }

        private (bool, string, int ) ValidateConnectionInfo()
        {
            bool result = int.TryParse(this.PortTextBox.Text, out int port) &&
                          IPAddress.TryParse(this.ServerTextBox.Text, out _);
            return (result, this.ServerTextBox.Text, port);
        }

        private void ReceiveData()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    if (!this.connected)
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                    var serverEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    var data = this.udpClient.Receive(ref serverEndPoint);

                    DrawingPacket packet;
                    IFormatter formatter = new BinaryFormatter();
                    using (MemoryStream stream = new MemoryStream(data))
                    {
                        packet = (DrawingPacket)formatter.Deserialize(stream);
                    }

                    // To invoke outside of STA thread
                    this.Dispatcher.BeginInvoke((Action)(() => this.DrawLine((Color)ColorConverter.ConvertFromString(packet.Color), packet.Points)));
                }
            });
        }

        private void DisconnectionButtonClick(object sender, RoutedEventArgs e)
        {
            this.Disconnect();
        }

        private void DrawLine(Color color, double[] points)
        {
            var line = new Line
            {
                Stroke = new SolidColorBrush(color),
                X1 = points[0],
                Y1 = points[1],
                X2 = points[2],
                Y2 = points[3]
            };

            this.Paint.Children.Add(line);
        }

        private void Paint_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

           double[] points =
           {
               this.currentPosition.X,
               this.currentPosition.Y,
               e.GetPosition(this.Paint).X,
               e.GetPosition(this.Paint).Y
           };

            this.DrawLine(this.currentColor, points);

            if (this.connected)
                this.SendDrawingData(this.currentColor, points);

            this.currentPosition = e.GetPosition(this.Paint);
        }

        private void Paint_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.currentPosition = e.GetPosition(this.Paint);
        }

        private void SendDrawingData(Color color, double[] points)
        {
            byte[] data;
            DrawingPacket drawingPacket = new DrawingPacket(points, color);
            IFormatter formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, drawingPacket);
                data = stream.ToArray();
            }

            // Add drawing code and send
            byte[] packet = new byte[data.Length+1];
            packet[0] = DrawingCode;
            data.CopyTo(packet, 1);
            this.udpClient.SendAsync(packet, packet.Length);   
        }

        private void UpdateColor(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.currentColor = Color.FromRgb((byte)this.RSlider.Value, (byte)this.GSlider.Value, (byte)this.BSlider.Value);
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            if (this.connected)
                this.Disconnect();
        }
    }

    [Serializable]
    internal struct DrawingPacket
    {
        public double[] Points;
        public string Color;

        public DrawingPacket(double[] points, Color color)
        {
            this.Points = new double[4];
            points.CopyTo(this.Points, 0);
            this.Color = new ColorConverter().ConvertToString(color);
        }

    }
}