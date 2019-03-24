using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;

namespace InteractiveBoard
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private int drawingServicePort;

        private readonly UdpClient udpClient = new UdpClient();

        private const string Hostname = "127.0.0.1";

        private const int Port = 27000;

        public MainWindow()
        {
            this.InitializeComponent();
        }

        private void ConnectionButtonClick(object sender, RoutedEventArgs e)
        {
            this.udpClient.Connect(Hostname , Port);
            string msg = "Connect";
            byte[] data = Encoding.ASCII.GetBytes(msg);
            this.udpClient.SendAsync(data, data.Length);

            // Get drawing port number
            var serverEndPoint = new IPEndPoint(IPAddress.Any, 0);
            data = this.udpClient.Receive(ref serverEndPoint);
            this.drawingServicePort = data[0] * byte.MaxValue + data[1];
        }

        private void DisconnectionButtonClick(object sender, RoutedEventArgs e)
        {
            this.udpClient.Connect(Hostname, Port);
            string msg = "Disconnect";
            byte[] data = Encoding.ASCII.GetBytes(msg);
            this.udpClient.SendAsync(data, data.Length);
        }
    }
}