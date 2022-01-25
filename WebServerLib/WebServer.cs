using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WebServerLib
{
    // State object for reading client data asynchronously  
    public class StateObject
    {
        // Size of receive buffer.  
        public const int BufferSize = 1024;

        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];

        // Received data string.
        public StringBuilder sb = new StringBuilder();

        // Client socket.
        public Socket workSocket = null;
    }

    public class WebServer
    {
        public event ServerInfoEvent ServerInfo;

        private readonly Socket serverSocket;
        private readonly Thread webServerThread;
        private readonly ManualResetEvent allDone;

        public WebServer(IPAddress ipAddress, int port)
        {
            allDone = new ManualResetEvent(false);
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            webServerThread = new(WebServerEntry);
            IpAddress = ipAddress;
            Port = port;
        }

        public IPAddress IpAddress { get; }
        public int Port { get; }
        public bool IsRunning { get; private set; }

        public void Start()
        {
            try
            {
                serverSocket.Bind(new IPEndPoint(IpAddress, Port));
                serverSocket.Listen(10);
                serverSocket.ReceiveTimeout = 1000;
                serverSocket.SendTimeout = 1000;
                IsRunning = true;
                webServerThread.Start();
            }
            catch (Exception ex)
            {
                ServerInfo?.Invoke("Server failed to start: " + ex.Message);
            }
        }

        public void Stop()
        {
            try
            {
                if (IsRunning)
                {
                    ServerInfo?.Invoke("Server is closing...");
                    IsRunning = false;
                    webServerThread.Join();
                    serverSocket.Close();
                    ServerInfo?.Invoke("Server stopped successfully");
                }
            }
            catch (Exception ex)
            {
                ServerInfo?.Invoke("Server failed to stop: " + ex.Message);
            }
        }

        private void WebServerEntry()
        {
            ServerInfo?.Invoke("Server started successfully");
           
            while (IsRunning)
            {
                allDone.Reset();
                serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), serverSocket);
                allDone.WaitOne();
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            allDone.Set();
            Socket? listener = (Socket?)ar.AsyncState;
            if (listener == null)
            {
                return;
            }
            Socket clientSocket = listener.EndAccept(ar);
            ServerInfo?.Invoke($"A request was made from a client");

            byte[] buffer = new byte[10240]; // 10 kb, just in case
            int receivedBCount = clientSocket.Receive(buffer); // Receive the request
            string strReceived = Encoding.UTF8.GetString(buffer, 0, receivedBCount);
            ServerInfo?.Invoke($"Data amount recived from client: {strReceived.Length} ");
            SendResponse(clientSocket, File.ReadAllText("index.html"), "200 OK", "text/html");
        }

        private void SendResponse(Socket clientSocket, string bContent, string responseCode, string contentType)
        {
            try
            {
                byte[] bHeader = Encoding.UTF8.GetBytes(
                                    "HTTP/1.1 " + responseCode + "\r\n"
                                  + "Server: Atasoy Simple Web Server\r\n"
                                  + "Content-Length: " + bContent.Length.ToString() + "\r\n"
                                  + "Connection: close\r\n"
                                  + "Content-Type: " + contentType + "\r\n\r\n");
                clientSocket.Send(bHeader);
                clientSocket.Send(Encoding.UTF8.GetBytes(bContent));
                clientSocket.Close();
                ServerInfo?.Invoke("Server responded to client client successfully");
            }
            catch (Exception ex)
            {
                ServerInfo?.Invoke($"Failed to respond to client: {ex.Message}");
            }
        }
    }
}
