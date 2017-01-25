using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ComLib;

namespace NetWork_TcpIp {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private static bool _running = true;

        public MainWindow() {
            InitializeComponent();
            TbPort.Text = "23000";
            TbIpAddress.Text = @"127.0.0.1";
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e) {
            var port = TbPort.Text;
            var ipaddress = TbIpAddress.Text;
            Task.Run(() => { SocketServerEx.StartListening(port, ipaddress); });
            Lstatus.Content = "Listening";
            BtnStart.IsEnabled = false;
            new Task(GetDataAndUpdateConsoleOutputBox).Start();
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e) {
            SocketServerEx.CloseSocket();
            _running = false;
            BtnStart.IsEnabled = true;
            Close();
        }

        private void GetDataAndUpdateConsoleOutputBox() {
            while (_running) {
                SocketServerEx.GetAutoResetEvent.WaitOne();
                var str = SocketServerEx.GetData;
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(
                        delegate {
                            TbConsoleOutPut.Text =
                                $"{DateTime.Now.ToShortDateString()} Client: {str} {TbConsoleOutPut.Text}";
                        }
                    )
                );
            }
        }

        private void SendMessageTb_OnKeyUp(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                // Send message to server.
            }
        }
    }
}