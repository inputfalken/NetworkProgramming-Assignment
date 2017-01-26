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
            var dataRecieved = state.WorkSocket.EndReceive(ar);
            if (dataRecieved > 0) {
                var data = Encoding.ASCII.GetString(state.Buffer, 0, dataRecieved);
                state.StringBuilder.Append(data);
                var content = state.StringBuilder.ToString();
                if (data == Environment.NewLine) {
                    content = content.Replace(data, string.Empty);
                    state.StringBuilder.Clear();
                    if (content == "Time") SendToClient(DateTime.Now.ToShortDateString(), state.WorkSocket);
                    else if (content == "Course") SendToClient("Network Programming", state.WorkSocket);
                    else if (content == "Name") SendToClient("Robert", state.WorkSocket);
                    GetData = content + GetData;
                    GetAutoResetEvent.Set();
                }
                if (Messages.Count != state.MessageCount) {
                    SendToClient(Messages.Last(), state.WorkSocket);
                    state.MessageCount++;
                }
                state.WorkSocket.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, ReadCallBack, state);
            }
        }

        private static readonly List<string> Messages = new List<string>();

        public static void SendMessageToClient(string message) {
            Messages.Add(message);
        }

        private static void SendToClient(string message, Socket socket) {
            socket.BeginSend(Encoding.ASCII.GetBytes(message), 0, message.Length, SocketFlags.None, SendCallback, socket);
        }

        private static void SendCallback(IAsyncResult ar) {
            var stateObject = (Socket) ar.AsyncState;
            stateObject.EndSend(ar);
        }
    }

    public class StateObject {
        public int MessageCount { get; set; }
        public Socket WorkSocket;
        public const int BufferSize = 1024;
        public readonly byte[] Buffer = new byte[BufferSize];
        public readonly StringBuilder StringBuilder = new StringBuilder();
    }
}