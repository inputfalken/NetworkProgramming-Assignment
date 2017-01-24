using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ComLib {
    public static class SocketServerEx {
        private static readonly Socket Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
            ProtocolType.Tcp);

        private static readonly AutoResetEvent AllDone = new AutoResetEvent(false);
        public static string GetData { get; private set; }

//      private static readonly IPHostEntry IpHostInfo = Dns.GetHostEntry("Localhost");
//      private static readonly IPAddress IpAddress = IpHostInfo.AddressList[0];
//      public static string GetIp => IpAddress.ToString();
        public static AutoResetEvent GetAutoResetEvent { get; } = new AutoResetEvent(false);

        public static void CloseSocket() => Listener.Close();

        public static void StartListening(string strport, string ipAddr) {
            var bytes = new byte[1024];
            var port = int.Parse(strport);
            var ipAddress = IPAddress.Parse(ipAddr);
            var localEndPoint = new IPEndPoint(ipAddress, port);
            try {
                Listener.Bind(localEndPoint);
                Listener.Listen(100);
                while (true) {
                    // Start an asynch socket to listen for connections.
                    Listener.BeginAccept(Callback, Listener);
                    //Wait until connecion is made before continuing.
                    AllDone.WaitOne();
                }
            }
            catch (Exception e) {
                Debug.WriteLine(e.Message);
            }
        }

        private static void Callback(IAsyncResult ar) {
            AllDone.Set();
            var listenter = (Socket) ar.AsyncState;
            var handler = listenter.EndAccept(ar);
            var state = new StateObject {WorkSocket = handler};
            handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, ReadCallBack, state);
        }

        private static void ReadCallBack(IAsyncResult ar) {
            var state = (StateObject) ar.AsyncState;
            var handler = state.WorkSocket;
            var bytresRead = handler.EndReceive(ar);
            if (bytresRead > 0) {
                state.StringBuilder.Append(Encoding.ASCII.GetString(state.Buffer, 0, bytresRead));
                var content = state.StringBuilder.ToString();
                if (content.IndexOf(Environment.NewLine, StringComparison.Ordinal) > -1) {
                    state.StringBuilder.Clear();
                    if (content == "Time") {
                    }
                    else if (content == "Course") {
                    }
                    else if (content == "Name") {
                    }
                    GetData = content + GetData;
                    GetAutoResetEvent.Set();
                    handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, ReadCallBack, state);
                }
                else {
                    handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, ReadCallBack, state);
                }
            }
        }
    }

    internal class StateObject {
        public Socket WorkSocket;
        public const int BufferSize = 1024;
        public readonly byte[] Buffer = new byte[BufferSize];
        public readonly StringBuilder StringBuilder = new StringBuilder();
    }
}