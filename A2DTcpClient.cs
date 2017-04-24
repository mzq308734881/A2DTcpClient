using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;

namespace EventBase
{
    public delegate void MessageReceived(string msg);

    public class A2DTcpClient
    {
        public const string terminateString = "\r\n";
        public const int receiveBufferSize = 1024;

        private string RemoteServer{get;set;}
        private int RemotePort { get; set; }
        private TcpClient tcpClient;

        public event MessageReceived NewMessageReceived;

        public A2DTcpClient(string remoteServer, int remotePort)
        {
            this.RemotePort = remotePort;
            this.RemoteServer = remoteServer;
            tcpClient = new TcpClient();
        }

        public void Connect()
        {
            if (tcpClient.Connected)
                throw new Exception("Connected, cannot re-connect.");

            tcpClient.Connect(this.RemoteServer, this.RemotePort);
            ThreadPool.QueueUserWorkItem(new WaitCallback(ReceiveMessage), tcpClient.Client);
            Thread.Sleep(2000);//强制暂停，为了上面的线程运行
        }

        public void Close()
        {
            if (!tcpClient.Connected)
                throw new Exception("Closed, cannot re-close.");

            tcpClient.Close();
        }

        StringBuilder sb = new StringBuilder();
        public void ReceiveMessage(object state)
        {
            Socket socket = (Socket)state;
            while(true)
            {
                byte[] buffer = new byte[receiveBufferSize];
                int receivedSize=socket.Receive(buffer);

                string rawMsg=System.Text.Encoding.Default.GetString(buffer, 0, receivedSize);
                int rnFixLength = terminateString.Length;
                for(int i=0;i<rawMsg.Length;)
                {
                    if (i <= rawMsg.Length - rnFixLength)
                    {
                        if (rawMsg.Substring(i, rnFixLength) != terminateString)
                        {
                            sb.Append(rawMsg[i]);
                            i++;
                        }
                        else
                        {
                            this.OnNewMessageReceived(sb.ToString());
                            sb.Clear();
                            i += rnFixLength;
                        }   
                    }
                    else
                    {
                        sb.Append(rawMsg[i]);
                        i++;
                    }
                }
            }
        }
        private void OnNewMessageReceived(string msg)
        {
            if (this.NewMessageReceived != null)
                this.NewMessageReceived.Invoke(msg);
        }

        public void Send(string str)
        {
            if(!this.tcpClient.Connected)
                throw new Exception("Closed, cannot send data.");

            str += terminateString;
            byte[] byteArray = System.Text.Encoding.Default.GetBytes(str);
            this.tcpClient.Client.Send(byteArray);
        }
    }
}
