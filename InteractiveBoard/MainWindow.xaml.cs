using System.Net.Sockets;
using System.Text;
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

        private const string Hostname = "127.0.0.1";

        private const int Port = 27000;

        private Point currentPosition;

        private Color currentColor;

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
            this.udpClient.SendAsync(data, data.Length);
        }

        private void DisconnectionButtonClick(object sender, RoutedEventArgs e)
        {
            this.udpClient.Connect(Hostname, Port);
            string msg = $"{MessageCode}Disconnect";
            byte[] data = Encoding.ASCII.GetBytes(msg);
            this.udpClient.SendAsync(data, data.Length);
        }

        private void Paint_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            Line line = new Line
            {
                Stroke = new SolidColorBrush(this.currentColor),
                X1 = this.currentPosition.X,
                Y1 = this.currentPosition.Y,
                X2 = e.GetPosition(this.Paint).X,
                Y2 = e.GetPosition(this.Paint).Y
            };

            this.currentPosition = e.GetPosition(this.Paint);
            this.Paint.Children.Add(line);
        }

        private void Paint_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.currentPosition = e.GetPosition(this.Paint);
        }
    }
}