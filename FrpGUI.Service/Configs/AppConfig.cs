using FrpGUI.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace FrpGUI.Configs
{
    public class AppConfig : AppConfigBase
    {
        public override string ConfigPath => Path.Combine(AppContext.BaseDirectory, "config.json");
        public List<ServerConfig> Servers { get; set; } = new List<ServerConfig>();
        public List<ClientConfig> Clients { get; set; } = new List<ClientConfig>();
        public string Token { get; set; }
        private JsonTypeInfo<AppConfig> JsonTypeInfo => AppConfigSourceGenerationContext.Default.AppConfig;

        public static AppConfig Get()
        {
            return Get(AppConfigSourceGenerationContext.Default.AppConfig);
        }

        public void Save()
        {
            Save(JsonTypeInfo);
        }
        protected override void OnLoaded()
        {
            if (Servers.Count == 0 && Clients.Count == 0)
            {
                Servers.Add(new ServerConfig());
                Clients.Add(new ClientConfig());
            }
        }
    }
}