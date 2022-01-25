using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WebServerLib
{
    public class WebServer
    {
        public event ServerInfoEvent? ServerInfo;
        private readonly Socket serverSocket;
        private readonly Thread webServerThread;
        private readonly ManualResetEvent allDone;
        private bool isRunning;

        public WebServer(IPAddress ipAddress, int port)
        {
            IpAddress = ipAddress;
            Port = port;
            allDone = new ManualResetEvent(false);
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            webServerThread = new(WebServerEntry);
        }

        public IPAddress IpAddress { get; }
        public int Port { get; }
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Used to start the server
        /// </summary>
        public void Start()
        {
            try
            {
                // Bind ip and port to server socket
                serverSocket.Bind(new IPEndPoint(IpAddress, Port));

                // Listen up to 10 clients
                serverSocket.Listen(10);

                // Start web server thread
                isRunning = true;
                webServerThread.Start();
            }
            catch (Exception ex)
            {
                ServerInfo?.Invoke("Server failed to start: " + ex.Message);
            }
        }

        /// <summary>
        /// Used to stop the server
        /// </summary>
        public void Stop()
        {
            if (isRunning)
            {
                ServerInfo?.Invoke("Server is closing...");

                // Joins thread and closes socket
                isRunning = false;
                webServerThread.Join();
                serverSocket.Close();
            }
        }

        /// <summary>
        /// The entry point for the server
        /// </summary>
        private void WebServerEntry()
        {
            ServerInfo?.Invoke($"Server started on {IpAddress.MapToIPv4()}:{Port}");

            while (isRunning)
            {
                // Begin accepting a new client
                serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), serverSocket);

                // Waits until a client tries to connect
                allDone.WaitOne();
                allDone.Reset();
            }
        }

        /// <summary>
        /// The entry point for a client request
        /// </summary>
        private void AcceptCallback(IAsyncResult ar)
        {
            // The clients has tried to connect continue in server thread 
            allDone.Set();

            Socket clientSocket = ((Socket)ar.AsyncState).EndAccept(ar);
            HandleRequest(clientSocket);
            SendResponse(clientSocket, Content);
        }

        /// <summary>
        /// Used to handle request from a client
        /// </summary>
        private void HandleRequest(Socket clientSocket)
        {
            byte[] buffer = new byte[10240];
            int receivedByteAmount = clientSocket.Receive(buffer);
            ServerInfo?.Invoke($"Data amount recived from client: {receivedByteAmount} ");
        }

        /// <summary>
        /// Used to send response to a client
        /// </summary>
        private void SendResponse(Socket clientSocket, string content)
        {
            // Http header
            byte[] header = Encoding.UTF8.GetBytes(
                  "HTTP/1.1 200 OK\r\n"
                + "Server: Atasoy Simple Web Server\r\n"
                + "Content-Length: " + content.Length.ToString() + "\r\n"
                + "Connection: close\r\n"
                + "Content-Type: text/html\r\n\r\n");

            // Send data and close the client connection
            clientSocket.Send(header);
            clientSocket.Send(Encoding.UTF8.GetBytes(content));
            clientSocket.Close();

            ServerInfo?.Invoke("Server responded to client client successfully");
        }
    }
}
