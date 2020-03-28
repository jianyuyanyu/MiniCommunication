using System;

namespace MiniComm.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "迷你服务终端";
            ServerHelper.CreateConfigFile();
            SqlHelper.SQLServerString = ServerHelper.GetDatabaseConnectionString();
            ServerManager serverManager = new ServerManager(ServerHelper.GetServerIPEndPoint());
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Server>");
                Console.ResetColor();
                serverManager.ExecuteCommand(Console.ReadLine().Trim().ToLower());
            }
        }
    }
}