using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Functional.Maybe;

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

        private static Maybe<string> Services(string str) {
            if (str == "Time") return DateTime.Now.ToShortDateString().ToMaybe();
            if (str == "Course") return "Network Programming".ToMaybe();
            if (str == "Name") return "Robert".ToMaybe();
            return Maybe<string>.Nothing;
        }


        private static void ReadCallBack(IAsyncResult ar) {
            var state = (StateObject) ar.AsyncState;
            _messageClient = ParameterizeMessageClient(state.WorkSocket);
            state.WorkSocket.EndReceive(ar).ToMaybe()
                .Where(i => i > 0) // Checks if theres any bytes.
                .Select(i => Encoding.ASCII.GetString(state.Buffer, 0, i)) // Map bytes to string
                .Do(s => state.StringBuilder.Append(s))
                .Where(s => s == Environment.NewLine) // If we get here, we have a message.
                .Select(s => state.StringBuilder.ToString()) // Map the message.
                .Select(message => message.Replace(Environment.NewLine, string.Empty)) // Remove Newline
                .Do(message => {
                    state.StringBuilder.Clear();
                    GetData = message;
                    GetAutoResetEvent.Set();
                }) //Print message sent by client
                .SelectMany(Services) // If this returns a value we need to respond.
                .Do(MessageClient);
            ParameterizeMessageClient(state.WorkSocket);
            state.WorkSocket.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, ReadCallBack, state);
        }

        private static Action<string> ParameterizeMessageClient(Socket socket)
            => s => socket.BeginSend(Encoding.ASCII.GetBytes(s), 0, s.Length, SocketFlags.None, SendCallback, socket);

        private static Action<string> _messageClient;

        public static void MessageClient(string message) => _messageClient?.Invoke(message);

        private static void SendCallback(IAsyncResult ar) {
            var stateObject = (Socket) ar.AsyncState;
            stateObject.EndSend(ar);
        }
    }

    public class StateObject {
        public Socket WorkSocket;
        public const int BufferSize = 1024;
        public readonly byte[] Buffer = new byte[BufferSize];
        public readonly StringBuilder StringBuilder = new StringBuilder();
    }
}