using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using EchWorkersManager.Models;
using EchWorkersManager.Routing;

namespace EchWorkersManager.Services
{
    public class HttpProxyService
    {
        private Thread httpProxyThread;
        private TcpListener httpProxyListener;
        private bool httpProxyRunning;
        private ProxyConfig config;
        private RoutingManager routingManager;

        public bool IsRunning => httpProxyRunning;

        public HttpProxyService(RoutingManager routingManager)
        {
            this.routingManager = routingManager;
        }

        public void Start(ProxyConfig config)
        {
            this.config = config;
            httpProxyRunning = true;
            httpProxyListener = new TcpListener(IPAddress.Loopback, config.HttpProxyPort);
            httpProxyListener.Start();

            httpProxyThread = new Thread(ListenForClients);
            httpProxyThread.IsBackground = true;
            httpProxyThread.Start();
        }

        public void Stop()
        {
            httpProxyRunning = false;
            if (httpProxyListener != null)
            {
                httpProxyListener.Stop();
            }
        }

        private void ListenForClients()
        {
            while (httpProxyRunning)
            {
                try
                {
                    if (httpProxyListener.Pending())
                    {
                        TcpClient client = httpProxyListener.AcceptTcpClient();
                        Thread clientThread = new Thread(() => HandleHttpProxyClient(client));
                        clientThread.IsBackground = true;
                        clientThread.Start();
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
                catch { }
            }
        }

        private void HandleHttpProxyClient(TcpClient client)
        {
            try
            {
                NetworkStream clientStream = client.GetStream();
                byte[] buffer = new byte[4096];
                int bytesRead = clientStream.Read(buffer, 0, buffer.Length);
                string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                string[] lines = request.Split(new[] { "\r\n" }, StringSplitOptions.None);
                if (lines.Length == 0) return;

                string[] requestLine = lines[0].Split(' ');
                if (requestLine.Length < 3) return;

                string method = requestLine[0];
                string url = requestLine[1];

                string targetHost = ExtractTargetHost(method, url);

                if (!routingManager.ShouldProxy(targetHost))
                {
                    HandleDirectConnection(client, clientStream, buffer, bytesRead, method, url, targetHost);
                    return;
                }

                if (method == "CONNECT")
                {
                    HandleConnectMethod(client, clientStream, url);
                }
                else
                {
                    HandleHttpMethod(client, clientStream, buffer, bytesRead, url);
                }
            }
            catch { }
        }

        private string ExtractTargetHost(string method, string url)
        {
            string targetHost = "";
            if (method == "CONNECT")
            {
                targetHost = url.Split(':')[0];
            }
            else
            {
                try
                {
                    Uri uri = new Uri(url.StartsWith("http") ? url : "http://" + url);
                    targetHost = uri.Host;
                }
                catch { }
            }
            return targetHost;
        }

        private void HandleDirectConnection(TcpClient client, NetworkStream clientStream, byte[] buffer, int bytesRead, string method, string url, string targetHost)
        {
            try
            {
                if (method == "CONNECT")
                {
                    string[] hostPort = url.Split(':');
                    int targetPort = hostPort.Length > 1 ? int.Parse(hostPort[1]) : 443;

                    TcpClient targetClient = new TcpClient(targetHost, targetPort);
                    NetworkStream targetStream = targetClient.GetStream();

                    string successResponse = "HTTP/1.1 200 Connection Established\r\n\r\n";
                    byte[] successBytes = Encoding.UTF8.GetBytes(successResponse);
                    clientStream.Write(successBytes, 0, successBytes.Length);

                    Thread forwardThread = new Thread(() => ForwardData(clientStream, targetStream));
                    forwardThread.IsBackground = true;
                    forwardThread.Start();
                    ForwardData(targetStream, clientStream);

                    targetClient.Close();
                }
                else
                {
                    Uri uri = new Uri(url.StartsWith("http") ? url : "http://" + url);
                    int targetPort = uri.Port;

                    TcpClient targetClient = new TcpClient(targetHost, targetPort);
                    NetworkStream targetStream = targetClient.GetStream();

                    targetStream.Write(buffer, 0, bytesRead);

                    Thread forwardThread = new Thread(() => ForwardData(targetStream, clientStream));
                    forwardThread.IsBackground = true;
                    forwardThread.Start();
                    ForwardData(clientStream, targetStream);

                    targetClient.Close();
                }

                client.Close();
            }
            catch { }
        }

        private void HandleConnectMethod(TcpClient client, NetworkStream clientStream, string url)
        {
            try
            {
                string[] hostPort = url.Split(':');
                string targetHost = hostPort[0];
                int targetPort = hostPort.Length > 1 ? int.Parse(hostPort[1]) : 443;

                TcpClient socksClient = new TcpClient(config.SocksHost, config.SocksPort);
                NetworkStream socksStream = socksClient.GetStream();

                socksStream.Write(new byte[] { 0x05, 0x01, 0x00 }, 0, 3);
                byte[] response = new byte[2];
                socksStream.Read(response, 0, 2);

                byte[] hostBytes = Encoding.ASCII.GetBytes(targetHost);
                byte[] connectRequest = new byte[7 + hostBytes.Length];
                connectRequest[0] = 0x05;
                connectRequest[1] = 0x01;
                connectRequest[2] = 0x00;
                connectRequest[3] = 0x03;
                connectRequest[4] = (byte)hostBytes.Length;
                Array.Copy(hostBytes, 0, connectRequest, 5, hostBytes.Length);
                connectRequest[5 + hostBytes.Length] = (byte)(targetPort >> 8);
                connectRequest[6 + hostBytes.Length] = (byte)(targetPort & 0xFF);

                socksStream.Write(connectRequest, 0, connectRequest.Length);
                byte[] connectResponse = new byte[10];
                socksStream.Read(connectResponse, 0, 10);

                if (connectResponse[1] == 0x00)
                {
                    string successResponse = "HTTP/1.1 200 Connection Established\r\n\r\n";
                    byte[] successBytes = Encoding.UTF8.GetBytes(successResponse);
                    clientStream.Write(successBytes, 0, successBytes.Length);

                    Thread forwardThread = new Thread(() => ForwardData(clientStream, socksStream));
                    forwardThread.IsBackground = true;
                    forwardThread.Start();
                    ForwardData(socksStream, clientStream);
                }

                socksClient.Close();
                client.Close();
            }
            catch { }
        }

        private void HandleHttpMethod(TcpClient client, NetworkStream clientStream, byte[] buffer, int bytesRead, string url)
        {
            try
            {
                Uri uri = new Uri(url.StartsWith("http") ? url : "http://" + url);
                string targetHost = uri.Host;
                int targetPort = uri.Port;

                TcpClient socksClient = new TcpClient(config.SocksHost, config.SocksPort);
                NetworkStream socksStream = socksClient.GetStream();

                socksStream.Write(new byte[] { 0x05, 0x01, 0x00 }, 0, 3);
                byte[] response = new byte[2];
                socksStream.Read(response, 0, 2);

                byte[] hostBytes = Encoding.ASCII.GetBytes(targetHost);
                byte[] connectRequest = new byte[7 + hostBytes.Length];
                connectRequest[0] = 0x05;
                connectRequest[1] = 0x01;
                connectRequest[2] = 0x00;
                connectRequest[3] = 0x03;
                connectRequest[4] = (byte)hostBytes.Length;
                Array.Copy(hostBytes, 0, connectRequest, 5, hostBytes.Length);
                connectRequest[5 + hostBytes.Length] = (byte)(targetPort >> 8);
                connectRequest[6 + hostBytes.Length] = (byte)(targetPort & 0xFF);

                socksStream.Write(connectRequest, 0, connectRequest.Length);
                byte[] connectResponse = new byte[10];
                socksStream.Read(connectResponse, 0, 10);

                if (connectResponse[1] == 0x00)
                {
                    socksStream.Write(buffer, 0, bytesRead);

                    Thread forwardThread = new Thread(() => ForwardData(socksStream, clientStream));
                    forwardThread.IsBackground = true;
                    forwardThread.Start();
                    ForwardData(clientStream, socksStream);
                }

                socksClient.Close();
                client.Close();
            }
            catch { }
        }

        private void ForwardData(NetworkStream from, NetworkStream to)
        {
            try
            {
                byte[] buffer = new byte[4096];
                int bytesRead;
                while ((bytesRead = from.Read(buffer, 0, buffer.Length)) > 0)
                {
                    to.Write(buffer, 0, bytesRead);
                }
            }
            catch { }
        }
    }
}