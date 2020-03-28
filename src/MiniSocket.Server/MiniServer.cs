﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MiniSocket.Transmitting;

namespace MiniSocket.Server
{
    /// <summary>
    /// 迷你服务器
    /// </summary>
    public class MiniServer
    {
        /// <summary>
        /// 原子锁，防止多个线程同时访问同一个资源
        /// </summary>
        private readonly object lockObject = new object();

        /// <summary>
        /// 服务器Socket
        /// </summary>
        private Socket serverSocket;
        /// <summary>
        /// 客户端对象集合
        /// </summary>
        private ClientCollection clientCollection;

        /// <summary>
        /// 获取服务器网络终结点
        /// </summary>
        public IPEndPoint ServerIPEndPoint { get; }
        /// <summary>
        /// 获取服务器协议
        /// </summary>
        public ProtocolType ServerAgreement { get; }
        /// <summary>
        /// 获取服务器队列的最大值
        /// </summary>
        public int ListenMaximum { get; }
        /// <summary>
        /// 获取服务器的状态
        /// </summary>
        public bool ServerState { get; private set; }

        /// <summary>
        /// 服务器的消息通知
        /// </summary>
        public event EventHandler<ServerNotifyEventArgs> ServerMessage;
        /// <summary>
        /// 客户端的消息通知
        /// </summary>
        public event EventHandler<ServerNotifyEventArgs> ClientMessage;
        /// <summary>
        /// 客户端离线时触发
        /// </summary>
        public event EventHandler<ServerNotifyEventArgs> ClientOffline;
        /// <summary>
        /// 服务器内部发生错误时触发
        /// </summary>
        public event EventHandler<ServerErrorEventArgs> ServerError;
        /// <summary>
        /// 数据库请求
        /// </summary>
        public event RequestHandler<DatabaseEventArgs> DatabaseRequest;

        /// <summary>
        /// 创建一个迷你服务器
        /// </summary>
        /// <param name="ipEndPoint">服务器网络终结点</param>
        /// <param name="agreement">服务器协议</param>
        /// <param name="listenMaximum">服务器等待队列最大值</param>
        public MiniServer(IPEndPoint ipEndPoint, ProtocolType agreement, int listenMaximum)
        {
            if (ipEndPoint.AddressFamily != AddressFamily.InterNetwork && ipEndPoint.AddressFamily != AddressFamily.InterNetworkV6)
            {
                throw new ArgumentException("只支持IPv4或IPv6地址");
            }
            if (agreement != ProtocolType.Tcp && agreement != ProtocolType.Udp)
            {
                throw new ArgumentException("只支持TCP或UDP协议");
            }
            clientCollection = new ClientCollection();
            ServerIPEndPoint = ipEndPoint;
            ServerAgreement = agreement;
            ListenMaximum = listenMaximum;
            ServerState = false;
        }

        /// <summary>
        /// 初始化服务器
        /// </summary>
        private void InitializationServer()
        {
            if (ServerAgreement == ProtocolType.Tcp)
            {
                serverSocket = new Socket(ServerIPEndPoint.AddressFamily, SocketType.Stream, ServerAgreement);
                serverSocket.Bind(ServerIPEndPoint);
                serverSocket.Listen(ListenMaximum);
            }
            else if (ServerAgreement == ProtocolType.Udp)
            {
                throw new ArgumentException("UDP协议暂时不受支持");
            }
        }

        /// <summary>
        /// 开启服务器
        /// </summary>
        public void OpenServer()
        {
            if (!ServerState)
            {
                ServerState = true;
                InitializationServer();
                WaitForClientConnection();
                ServerMessage?.Invoke(this, new ServerNotifyEventArgs("服务器已启动"));
            }
        }

        /// <summary>
        /// 关闭服务器
        /// </summary>
        public void CloseServer()
        {
            if (ServerState)
            {
                ServerState = false;
                CloseClients();
                serverSocket.Close();
                serverSocket.Dispose();
                ServerMessage?.Invoke(this, new ServerNotifyEventArgs("服务器已关闭"));
            }
        }

        /// <summary>
        /// 关闭指定客户端
        /// </summary>
        /// <param name="userID">客户端唯一的ID</param>
        /// <param name="normal">是否为正常断开</param>
        /// <param name="exception">异常类型</param>
        public void CloseClient(string userID, bool normal = true, Exception exception = null)
        {
            lock (lockObject)
            {
                if (clientCollection.ContainClient(userID))
                {
                    string ipEndPoint = clientCollection.GetClientIPEndPoint(userID).ToString();
                    if (clientCollection.RemoveClient(userID))
                    {
                        ClientOffline?.Invoke(this, new ServerNotifyEventArgs(userID));
                        if (normal)
                        {
                            ServerMessage?.Invoke(this, new ServerNotifyEventArgs($"客户端 {userID}({ipEndPoint}) 断开连接"));
                            return;
                        }
                        ServerMessage?.Invoke(this, new ServerNotifyEventArgs($"客户端 {userID}({ipEndPoint}) 意外的断开连接"));
                        ServerError?.Invoke(this, new ServerErrorEventArgs(exception, $"客户端 {userID}({ipEndPoint}) 意外的断开连接"));
                    }
                }
            }
        }

        /// <summary>
        /// 关闭指定客户端的端口
        /// </summary>
        /// <param name="userID">客户端的ID</param>
        /// <param name="port">客户端的端口</param>
        /// <param name="normal">是否为正常断开</param>
        /// <param name="exception">异常类型</param>
        public void CloseClientPort(string userID, int port, bool normal = true, Exception exception = null)
        {
            lock (lockObject)
            {
                if (clientCollection.RemovePort(userID, port))
                {
                    if (normal)
                    {
                        ServerMessage?.Invoke(this, new ServerNotifyEventArgs($"客户端 {userID} 的 {port} 端口断开连接"));
                        return;
                    }
                    ServerMessage?.Invoke(this, new ServerNotifyEventArgs($"客户端 {userID} 的 {port} 端口意外的断开连接"));
                    ServerError?.Invoke(this, new ServerErrorEventArgs(exception, $"客户端 {userID} 的 {port} 端口意外的断开连接"));
                }
            }
        }

        /// <summary>
        /// 关闭所有客户端
        /// </summary>
        public void CloseClients()
        {
            clientCollection.RemoveClients();
            ServerMessage?.Invoke(this, new ServerNotifyEventArgs("所有客户端已被强制断开连接"));
        }

        /// <summary>
        /// 获取所有客户端的ID
        /// </summary>
        public List<string> GetClientIds() => clientCollection.GetClientIds();

        /// <summary>
        /// 获取指定ID的所有网络终结点
        /// </summary>
        /// <param name="userID">客户端唯一ID</param>
        public List<IPEndPoint> GetClientIPEndPoints(string userID) => clientCollection.GetClientIPEndPoints(userID);

        /// <summary>
        /// 等待客户端连接
        /// </summary>
        private void WaitForClientConnection()
        {
            Task serverAcceptTask = Task.Factory.StartNew(() =>
            {
                while (ServerState)
                {
                    Socket clientSocket = null;
                    try
                    {
                        clientSocket = serverSocket.Accept();
                    }
                    catch (Exception exception)
                    {
                        ServerError?.Invoke(this, new ServerErrorEventArgs(exception, "服务器意外的被关闭"));
                        break;
                    }
                    switch (ServerAgreement)
                    {
                        case ProtocolType.Tcp:
                            Task task = Task.Run(() => ReceiveMessage(clientSocket));
                            break;
                        case ProtocolType.Udp:
                            break;
                    }
                }
            });
        }

        /// <summary>
        /// 接收数据
        /// </summary>
        /// <param name="socket">客户端Socket对象</param>
        private void ReceiveMessage(Socket socket)
        {
            bool isMain = true; //是否为主端口

            string clientID = string.Empty; //客户端ID
            IPEndPoint clientIPEndPoint = (IPEndPoint)socket.RemoteEndPoint; //客户端网络终结点

            bool isFirstConnection = true; //是否为首次连接
            byte[] dataBuffer = new byte[1024 * 1024]; //缓冲区

            bool isFirstReceive = true; //是否为首次接收数据
            Transmit transmit = null; //传输对象
            double dataSize = 0.0; //数据总大小
            double receiveBytes = 0.0; //已接收的字节数
            int targetPort = -1; //发送目标端口

            while (true)
            {
                int readBytes = 0;
                if (socket.Poll(-1, SelectMode.SelectRead) == true)
                {
                    if (!ServerState) { break; }
                    try
                    {
                        readBytes = socket.Receive(dataBuffer, dataBuffer.Length, SocketFlags.None);
                        if (readBytes == 0)
                        {
                            if (isMain)
                            {
                                CloseClient(clientID);
                                break;
                            }
                            CloseClientPort(clientID, clientIPEndPoint.Port);
                            break;
                        }
                    }
                    catch (Exception exception)
                    {
                        if (isMain)
                        {
                            CloseClient(clientID, false, exception);
                            break;
                        }
                        CloseClientPort(clientID, clientIPEndPoint.Port, false, exception);
                        break;
                    }
                }

                //首次连接
                if (isFirstConnection)
                {
                    isFirstConnection = false;
                    clientID = Encoding.UTF8.GetString(dataBuffer, 0, readBytes);
                    if (clientCollection.AddClient(clientID, clientIPEndPoint, socket))
                    {
                        ServerMessage?.Invoke(this, new ServerNotifyEventArgs($"客户端 {clientID}({clientIPEndPoint}) 已连接"));
                    }
                    else
                    {
                        if (!clientCollection.AddPort(clientID, clientIPEndPoint.Port, socket)) { break; }
                        isMain = false;
                        ServerMessage?.Invoke(this, new ServerNotifyEventArgs($"客户端 {clientID} 的 {clientIPEndPoint.Port} 端口已连接"));
                    }
                    socket.Send(new byte[] { 1 });
                    continue;
                }

                //首次接收数据
                if (isFirstReceive)
                {
                    isFirstReceive = false;
                    transmit = BytesConvert.BytesToObject(dataBuffer, readBytes, "MiniSocket.Transmitting") as Transmit;
                    if (transmit?.Parameter == null)
                    {
                        ResetParameter(ref isFirstReceive, ref transmit, ref dataSize, ref receiveBytes);
                        continue;
                    }

                    if (isMain)
                    {
                        string[] parameters = transmit.Parameter.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                        switch (transmit.DataType)
                        {
                            case DataType.Text:
                                if (!Forward(transmit, dataBuffer, readBytes)) { return; }
                                break;
                            case DataType.Image:
                                if (parameters.Length == 2)
                                {
                                    if (double.TryParse(parameters[1], out dataSize))
                                    {
                                        if (!Forward(transmit, dataBuffer, readBytes)) { return; }
                                        continue;
                                    }
                                }
                                break;
                            case DataType.Request:
                                if (transmit.Parameter.StartsWith("Database"))
                                {
                                    if (parameters.Length == 2)
                                    {
                                        var result = DatabaseRequest?.Invoke(this, new DatabaseEventArgs(parameters[1], transmit.Object))?.Result;
                                        if (result != null)
                                        {
                                            transmit.Parameter = "Client;RequestResult";
                                            transmit.Object = result;
                                            byte[] buffer = BytesConvert.ObjectToBytes(transmit);
                                            if (!Forward(transmit, buffer, buffer.Length)) { return; }
                                        }
                                    }
                                }
                                break;
                        }
                        ResetParameter(ref isFirstReceive, ref transmit, ref dataSize, ref receiveBytes);
                        continue;
                    }
                    if (transmit.DataType == DataType.File)
                    {
                        string[] parameters = transmit.Parameter.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parameters.Length == 2)
                        {
                            if (double.TryParse(parameters[1], out dataSize))
                            {
                                Transmit requestTransmit = new Transmit()
                                {
                                    SourceID = transmit.SourceID,
                                    TargetID = transmit.TargetID,
                                    DataType = DataType.Request,
                                    Parameter = "Client;AddSocket"
                                };
                                byte[] buffer = BytesConvert.ObjectToBytes(requestTransmit);
                                if (!Forward(requestTransmit, buffer, buffer.Length)) { return; }
                                Thread.Sleep(3000);
                                List<Socket> sockets = clientCollection.GetClientSockets(transmit.TargetID);
                                targetPort = ((IPEndPoint)sockets[sockets.Count - 1].RemoteEndPoint).Port;
                                if (!Forward(transmit, dataBuffer, readBytes, targetPort)) { return; }
                                continue;
                            }
                        }
                    }
                    CloseClientPort(clientID, clientIPEndPoint.Port);
                    return;
                }

                receiveBytes += readBytes; //已接收到的数据累加

                //判断已接收到的数据是否有效
                if (receiveBytes > 0 && receiveBytes <= dataSize)
                {
                    if (!Forward(transmit, dataBuffer, readBytes, targetPort)) { break; }
                }
                //判断是否接收完成或数据是否有误
                if (receiveBytes == dataSize || (receiveBytes <= 0 || receiveBytes > dataSize))
                {
                    if (isMain)
                    {
                        ResetParameter(ref isFirstReceive, ref transmit, ref dataSize, ref receiveBytes);
                        continue;
                    }
                    CloseClientPort(clientID, clientIPEndPoint.Port);
                    break;
                }
            }
        }

        /// <summary>
        /// 转发数据
        /// </summary>
        /// <param name="transmit">传输对象</param>
        /// <param name="data">数据包</param>
        /// <param name="sendBytes">需要发送的字节数</param>
        /// <param name="targetPort">发送目标端口</param>
        private bool Forward(Transmit transmit, byte[] data, int sendBytes, int targetPort = -1)
        {
            if (clientCollection.ContainClient(transmit.SourceID) && clientCollection.ContainClient(transmit.TargetID))
            {
                List<Socket> sockets = clientCollection.GetClientSockets(transmit.TargetID);
                List<int> ports = clientCollection.GetClientPorts(transmit.TargetID);
                if (targetPort == -1)
                {
                    try
                    {
                        if (sockets.Count < 1)
                        {
                            throw new ArgumentException("找不到客户端主端口");
                        }
                        sockets[0].Send(data, sendBytes, SocketFlags.None);
                    }
                    catch (Exception exception)
                    {
                        CloseClient(transmit.TargetID, false, exception);
                        return false;
                    }
                }
                else
                {
                    try
                    {
                        if (!ports.Contains(targetPort))
                        {
                            throw new ArgumentException("找不到客户端辅助端口");
                        }
                        foreach (Socket socket in sockets)
                        {
                            IPEndPoint ipEndPoint = (IPEndPoint)socket.RemoteEndPoint;
                            if (ipEndPoint.Port == targetPort)
                            {
                                socket.Send(data, sendBytes, SocketFlags.None);
                                break;
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        CloseClientPort(transmit.TargetID, targetPort, false, exception);
                        return false;
                    }
                }
                ClientMessage?.Invoke(this, new ServerNotifyEventArgs($"{transmit.SourceID} Send {sendBytes} Bytes To {transmit.TargetID}"));
            }
            return true;
        }

        private void ResetParameter(ref bool isFirstReceive, ref Transmit transmit, ref double dataSize, ref double receiveBytes)
        {
            isFirstReceive = true;
            transmit = null;
            dataSize = 0.0;
            receiveBytes = 0.0;
        }
    }
}