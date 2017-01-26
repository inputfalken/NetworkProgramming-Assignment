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
            state.WorkSocket.EndReceive(ar)
                .ToMaybe()
                .Where(i => i > 0) // Checks if theres any bytes.
                .Select(i => Encoding.ASCII.GetString(state.Buffer, 0, i)) // Map bytes to string
                .Select(data => new {builder = state.StringBuilder.Append(data), txt = data}) // Build the message
                .Where(arg => arg.txt == Environment.NewLine) // If we get here, we have a message.
                .Select(arg => arg.builder.Replace(arg.txt, string.Empty).ToString()) // Map the message.
                .Do(message => {
                    state.StringBuilder.Clear();
                    GetData = message;
                    GetAutoResetEvent.Set();
                }) //Print message sent by client
                .SelectMany(Services) // If this returns a value we need to respond.
                .Do(s => SendToClient(s, state.WorkSocket));

            if (Messages.Count != state.MessageCount) {
                SendToClient(Messages.Last(), state.WorkSocket);
                state.MessageCount++;
            }
            state.WorkSocket.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, ReadCallBack, state);
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