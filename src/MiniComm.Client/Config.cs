using System;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using MiniSocket.Client;

namespace MiniComm.Client
{
    /// <summary>
    /// 应用程序配置
    /// </summary>
    public static class Config
    {
        /// <summary>
        /// 获取客户端名称
        /// </summary>
        public const string Name = "迷你通讯";
        /// <summary>
        /// 获取或设置客户端对象
        /// </summary>
        public static MiniClient MiniClient;

        /// <summary>
        /// 获取日志目录名称
        /// </summary>
        public const string LogDirectoryName = "Log";
        /// <summary>
        /// 获取文件目录名称
        /// </summary>
        public const string FileDirectoryName = "File";
        /// <summary>
        /// 获取缓存目录名称
        /// </summary>
        public const string CacheDirectoryName = "Cache";

        /// <summary>
        /// 获取客户端地址族
        /// </summary>
        public const AddressFamily ClientAddressFamily = AddressFamily.InterNetwork;
        /// <summary>
        /// 获取客户端网络协议
        /// </summary>
        public const ProtocolType ClientAgreement = ProtocolType.Tcp;

        /// <summary>
        /// 获取服务器网络终结点
        /// </summary>
        /// <returns></returns>
        public static IPEndPoint GetServerIPEndPoint()
        {
            if (!File.Exists("Config.ini"))
            {
                File.WriteAllLines("Config.ini", new string[] { "[Client]", "Server=0.0.0.0:0" }, Encoding.UTF8);
            }
            string[] config = File.ReadAllLines("Config.ini", Encoding.UTF8);
            if (config.Length == 2)
            {
                string[] ipAddress = config[1].Split(new string[] { "Server=" }, StringSplitOptions.RemoveEmptyEntries);
                if (ipAddress.Length == 1)
                {
                    try
                    {
                        return IPEndPoint.Parse(ipAddress[0]);
                    }
                    catch { }
                }
            }
            return IPEndPoint.Parse("0.0.0.0:0");
        }

        /// <summary>
        /// 获取用户目录的绝对路径，并创建相应目录
        /// </summary>
        /// <param name="userID">用户ID</param>
        /// <param name="directoryName">目录名称</param>
        public static string GetUserDirectory(string userID, string directoryName)
        {
            StringBuilder directoryString = new StringBuilder(Directory.GetCurrentDirectory());
            directoryString.Append("\\");
            directoryString.Append("User");
            directoryString.Append("\\");
            directoryString.Append(userID);
            directoryString.Append("\\");
            directoryString.Append(directoryName);
            string path = directoryString.ToString();
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }
    }
}