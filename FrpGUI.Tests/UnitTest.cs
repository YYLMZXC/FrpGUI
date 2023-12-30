using FrpGUI.Config;
using System.Diagnostics;

namespace FrpGUI.Tests
{
    [TestClass]
    public class UnitTest
    {
        public const string TOKEN = "frptoken";
        public const int PORT = 7000;
        public const string LOCALHOST = "localhost";

        ServerConfig server;
        ClientConfig client;
        ClientConfig visitor;

        [TestMethod]
        public async Task TestAll()
        {
            Logger.NewLog += Logger_NewLog;
            server = new ServerConfig()
            {
                Name = "�����",
                Port = 7000,
                Token = TOKEN,
                TlsOnly = true
            };
            server.Start();

            client = new ClientConfig()
            {
                Name = "�ͻ���",
                ServerPort = PORT,
                Token = TOKEN,
                ServerAddress = LOCALHOST,
                EnableTls = true,
                Rules = new List<Rule>()
                {
                    new Rule()
                    {
                       Type=NetType.TCP,
                       Name="TcpTest",
                       LocalAddress=LOCALHOST,
                       LocalPort="11000",
                       RemotePort="10000",
                       Encryption=true,
                       Compression=true,
                    },
                    new Rule()
                    {
                       Type=NetType.UDP,
                       Name="UdpTest",
                       LocalAddress=LOCALHOST,
                       LocalPort="11001",
                       RemotePort="10001",
                       Encryption=true,
                       Compression=true,
                    },
                    new Rule()
                    {
                       Type=NetType.STCP,
                       Name="StcpTest",
                       LocalAddress=LOCALHOST,
                       LocalPort="11002",
                       STCPKey="1234",
                       Encryption=true,
                       Compression=true,
                    },
                }
            };
            client.Start();

            visitor = new ClientConfig()
            {
                Name = "������",
                ServerPort = PORT,
                Token = TOKEN,
                ServerAddress = LOCALHOST,
                EnableTls = true,
                Rules = new List<Rule>()
                {
                    new Rule()
                    {
                       Type=NetType.STCP_Visitor,
                       Name="StcpVTest",
                       LocalAddress=LOCALHOST,
                       LocalPort="11003",
                       STCPKey="1234",
                       STCPServerName="StcpTest",
                       Encryption=true,
                       Compression=true,
                    },
                }
            };
            visitor.Start();

            await Task.Delay(2000);
            Trace.WriteLine("==========2���ѹ�����ʼ�˳�����==========");
            await client?.StopAsync();
            await visitor?.StopAsync();
            await server?.StopAsync();
        }

        private void Logger_NewLog(object? sender, LogEventArgs e)
        {
            string message = e.Message;
            if (e.FromFrp)
            {
                message = $"(frp-{e.InstanceName}) {message}";
            }
            else
            {
                if (e.InstanceName != null)
                {
                    message = $"({e.InstanceName}) {message}";
                }
            }

            Trace.WriteLine($"{e.Time}  [{e.Type}]  {message}");
        }
    }
}