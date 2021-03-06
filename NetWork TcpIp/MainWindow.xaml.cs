﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ComLib;

namespace NetWork_TcpIp {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private static bool _running;

        public MainWindow() {
            InitializeComponent();
            TbPort.Text = "23000";
            TbIpAddress.Text = @"127.0.0.1";
            Lstatus.Content = "Closed";
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e) {
            var port = TbPort.Text;
            _running = true;
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
            Lstatus.Content = "Closed";
        }

        private void GetDataAndUpdateConsoleOutputBox() {
            while (_running) {
                SocketServerEx.GetAutoResetEvent.WaitOne();
                var str = SocketServerEx.GetData;
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(
                        () => TbConsoleOutPut.AppendText(
                            $"{DateTime.Now.ToShortDateString()} Client: {str} {Environment.NewLine}")
                    )
                );
            }
        }

        private void SendMessageTb_OnKeyUp(object sender, KeyEventArgs e) {
            if (e.Key != Key.Enter) return;
            SocketServerEx.MessageClient($"{SendMessageTb.Text}");
            SendMessageTb.Text = string.Empty;
        }
    }
}