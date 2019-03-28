using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
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

        private const int MessageCode = 0; // TODO: unify with server codes (copy array instead of changing to ascii)

        private const int DrawingCode = 49;

        private readonly UdpClient udpClient = new UdpClient();

        private const string Hostname = "127.0.0.1";

        private const int Port = 27000;

        private Point currentPosition;

        private Color currentColor;

        private bool connected;

        public MainWindow()
        {
            this.InitializeComponent();
            this.currentColor = Colors.Black;
        }

        private void ConnectionButtonClick(object sender, RoutedEventArgs e)
        {
            this.udpClient.Connect(Hostname , Port);
            string msg = $"{MessageCode}Connect";
            byte[] data = Encoding.ASCII.GetBytes(msg);
            this.udpClient.SendAsync(data, data.Length); // TODO: do some response from server to make sure
            this.connected = true;
            this.Connect.IsEnabled = false;
            this.Disconnect.IsEnabled = true;

            this.ReceiveData(); // TODO: handle case when disconnected and connected back
        }

        private void ReceiveData()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    var serverEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    var data = this.udpClient.Receive(ref serverEndPoint);

                    double[] points;
                    IFormatter formatter = new BinaryFormatter();
                    using (MemoryStream stream = new MemoryStream(data))
                    {
                        points = (double[])formatter.Deserialize(stream);
                    }

                    // To invoke outside of STA thread
                    this.Dispatcher.BeginInvoke((Action)(() => this.DrawLine(this.currentColor, points)));
                }
            });
        }

        private void DisconnectionButtonClick(object sender, RoutedEventArgs e)
        {
            this.udpClient.Connect(Hostname, Port);
            string msg = $"{MessageCode}Disconnect";
            byte[] data = Encoding.ASCII.GetBytes(msg);
            this.udpClient.SendAsync(data, data.Length);
            this.connected = false;
            this.Connect.IsEnabled = true;
            this.Disconnect.IsEnabled = false;
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
                this.SendDrawingData(points);

            this.currentPosition = e.GetPosition(this.Paint);
        }

        private void Paint_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.currentPosition = e.GetPosition(this.Paint);
        }

        private void SendDrawingData(double[] points)
        {
            byte[] data;
            IFormatter formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, points);
                data = stream.ToArray();
                
            }

            // Add drawing code and send
            byte[] packet = new byte[data.Length+1];
            packet[0] = DrawingCode;
            data.CopyTo(packet, 1);
            this.udpClient.SendAsync(packet, packet.Length);   
        }
    }
}