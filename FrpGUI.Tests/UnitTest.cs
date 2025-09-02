using FrpGUI.Enums;
using System.Diagnostics;
using FrpGUI.Configs;
using FrpGUI.Models;
using FrpGUI.Services;

namespace FrpGUI.Tests
{   
    [TestClass]
    public class UnitTest
    {

        public const string LOCALHOST = "localhost";
        public const int PORT = 7000;
        public const string TOKEN = "frptoken";

        [TestMethod]
        public async Task TestAll()
        {
            (ServerConfig server, ClientConfig client, ClientConfig visitor) configs
                = (GetServerConfig(), GetClientConfig(), GetVisitorConfig());
            AppConfig config = new AppConfig()
            {
                FrpConfigs =
                {
                    configs.server,
                    configs.client,
                    configs.visitor
                }
            };
            var logger = new TestLogger();
            FrpProcessCollection processes = new FrpProcessCollection(config, logger);
            var server = processes.GetOrCreateProcess(configs.server.ID);
            var client = processes.GetOrCreateProcess(configs.client.ID);
            var visitor = processes.GetOrCreateProcess(configs.visitor.ID);
            Trace.WriteLine("��ʼ���������");
            await server.StartAsync();
            Trace.WriteLine("������������");
            await Task.Delay(1000);

            Trace.WriteLine("��ʼ�����ͻ���");
            await client.StartAsync();
            Trace.WriteLine("��������ͻ���");
            await Task.Delay(1000);

            Trace.WriteLine("��ʼ����������");
            await visitor.StartAsync();
            Trace.WriteLine("�������������");

            Trace.WriteLine("��ʼ�ȴ�2��");
            await Task.Delay(2000);
            Trace.WriteLine("==========2���ѹ�����ʼ�˳�����==========");
            await client.StopAsync();
            await visitor.StopAsync();
            await server.StopAsync();
        }

        private static void WriteLog(LogEntity log)
        {
            string message = log.Message;
            if (log.FromFrp)
            {
                message = $"(frp-{log.InstanceName}) {message}";
            }
            else
            {
                if (log.InstanceName != null)
                {
                    message = $"({log.InstanceName}) {message}";
                }
            }

            Trace.WriteLine($"{log.Time}  [{log.Type}]  {message}");
        }

        private ClientConfig GetClientConfig()
        {
            return new ClientConfig()
            {
                Name = "�ͻ���",
                ServerPort = PORT,
                Token = TOKEN,
                ServerAddress = LOCALHOST,
                EnableTls = true,
                DashBoardPort=7501,
                Rules = new List<Rule>()
                {
                    new Rule()
                    {
                        Type = NetType.TCP,
                        Name = "TcpTest",
                        LocalAddress = LOCALHOST,
                        LocalPort = "11000",
                        RemotePort = "10000",
                        Encryption = true,
                        Compression = true,
                    },
                    new Rule()
                    {
                        Type = NetType.UDP,
                        Name = "UdpTest",
                        LocalAddress = LOCALHOST,
                        LocalPort = "11001",
                        RemotePort = "10001",
                        Encryption = true,
                        Compression = true,
                    },
                    new Rule()
                    {
                        Type = NetType.STCP,
                        Name = "StcpTest",
                        LocalAddress = LOCALHOST,
                        LocalPort = "11002",
                        StcpKey = "1234",
                        Encryption = true,
                        Compression = true,
                    },
                }
            };
        }

        private ServerConfig GetServerConfig()
        {
            return new ServerConfig()
            {
                Name = "�����",
                Port = 7000,
                Token = TOKEN,
                TlsOnly = true,
                DashBoardPort = 7500,
            };
        }

        private ClientConfig GetVisitorConfig()
        {
            return new ClientConfig()
            {
                Name = "������",
                ServerPort = PORT,
                Token = TOKEN,
                ServerAddress = LOCALHOST,
                EnableTls = true,
                DashBoardPort = 7502,
                Rules = new List<Rule>()
                {
                    new Rule()
                    {
                        Type = NetType.STCP_Visitor,
                        Name = "StcpVTest",
                        LocalAddress = LOCALHOST,
                        LocalPort = "11003",
                        StcpKey = "1234",
                        StcpServerName = "StcpTest",
                        Encryption = true,
                        Compression = true,
                    },
                }
            };
        }

        class TestLogger : LoggerBase
        {
            protected override void AddLog(LogEntity logEntity)
            {
                WriteLog(logEntity);
            }
        }
    }
}