using System;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using MiniSocket.Server;
using MiniSocket.Transmitting;

namespace MiniComm.Server
{
    /// <summary>
    /// 服务器管理器
    /// </summary>
    public class ServerManager
    {
        /// <summary>
        /// 迷你服务器
        /// </summary>
        private MiniServer miniServer;
        /// <summary>
        /// 服务器指令集
        /// </summary>
        private Dictionary<string, Action> commandSet;

        /// <summary>
        /// 创建一个服务器管理器
        /// </summary>
        /// <param name="serverIPEndPoint">服务器网络终结点</param>
        public ServerManager(IPEndPoint serverIPEndPoint)
        {
            miniServer = new MiniServer(serverIPEndPoint, ProtocolType.Tcp, 100);
            miniServer.ServerMessage += new EventHandler<ServerNotifyEventArgs>(MiniServer_ServerMessage);
            miniServer.ClientMessage += new EventHandler<ServerNotifyEventArgs>(MiniServer_ClientMessage);
            miniServer.ClientOffline += new EventHandler<ServerNotifyEventArgs>(MiniServer_ClientOffline);
            miniServer.ServerError += new EventHandler<ServerErrorEventArgs>(MiniServer_ServerError);
            miniServer.DatabaseRequest += new RequestHandler<DatabaseEventArgs>(MiniServer_DatabaseRequest);

            commandSet = new Dictionary<string, Action>()
            {
                { "help", ()=>{ ShowCommandHelp(); } },
                { "open", ()=>{ miniServer.OpenServer(); } },
                { "close", ()=>{ miniServer.CloseServer(); } },
                { "show server", ()=>{ ShowServerInfo(); } },
                { "show clients", ()=>{ ShowClientsInfo(); } },
            };
        }

        /// <summary>
        /// 服务器执行指定的命令
        /// </summary>
        /// <param name="cmd">命令</param>
        public void ExecuteCommand(string cmd)
        {
            if (commandSet.ContainsKey(cmd))
            {
                commandSet[cmd].Invoke();
            }
        }

        /// <summary>
        /// 显示命令帮助
        /// </summary>
        private void ShowCommandHelp()
        {
            StringBuilder command = new StringBuilder();
            command.AppendLine("open -启动服务器");
            command.AppendLine("close -关闭服务器");
            command.AppendLine("show server -查看服务器信息");
            command.AppendLine("show clients -查看所有客户端的信息");
            OutputMessage("\r\n" + command);
        }

        /// <summary>
        /// 显示服务器信息
        /// </summary>
        private void ShowServerInfo()
        {
            Console.WriteLine();
            OutputMessage($"服务器IP地址：{miniServer.ServerIPEndPoint.Address}");
            OutputMessage($"服务器端口：{miniServer.ServerIPEndPoint.Port}");
            OutputMessage($"服务器网络协议：{miniServer.ServerAgreement}");
            OutputMessage($"服务器状态：{(miniServer.ServerState == true ? "开启" : "关闭")}");
            Console.WriteLine();
        }

        /// <summary>
        /// 显示所有客户端的信息
        /// </summary>
        private void ShowClientsInfo()
        {
            var users = miniServer.GetClientIds();
            if (users.Count < 1)
            {
                OutputMessage("当前没有客户端连接到服务器");
                return;
            }
            Console.WriteLine();
            foreach (var user in users)
            {
                OutputMessage(user);
                Console.WriteLine("----------");
                foreach (var ipEndPoint in miniServer.GetClientIPEndPoints(user))
                {
                    OutputMessage(ipEndPoint);
                }
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 输出消息到控制台
        /// </summary>
        /// <param name="message">消息</param>
        private void OutputMessage(object message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private void MiniServer_ServerMessage(object sender, ServerNotifyEventArgs e)
        {
            OutputMessage(e.Message);
            ServerHelper.OutputRuntimeLog(e.Message);
        }

        private void MiniServer_ClientMessage(object sender, ServerNotifyEventArgs e)
        {
            OutputMessage(e.Message);
        }

        private void MiniServer_ServerError(object sender, ServerErrorEventArgs e)
        {
            ServerHelper.OutputErrorLog(e.Exception, e.ErrorMessage);
        }

        private async void MiniServer_ClientOffline(object sender, ServerNotifyEventArgs e)
        {
            await DatabaseActions.UserOfflineAsync(e.Message);
        }

        private async Task<RequestResult> MiniServer_DatabaseRequest(object sender, DatabaseEventArgs e)
        {
            if (e.Operation != null && e.Model != null)
            {
                string[] operation = e.Operation.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                User user = e.Model as User;
                if (operation.Length > 0 && user != null)
                {
                    return await ServerHelper.ExecuteDBAction(user, operation[0], operation.Length == 2 ? operation[1] : string.Empty);
                }
            }
            return null;
        }
    }
}