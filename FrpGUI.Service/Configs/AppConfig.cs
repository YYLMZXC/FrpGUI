using FrpGUI.Models;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace FrpGUI.Configs
{
    public class AppConfig : AppConfigBase
    {
        private static readonly string ConfigPathS = Path.Combine(AppContext.BaseDirectory, "config.json");
        public override string ConfigPath { get; } = ConfigPathS;
        public List<FrpConfigBase> FrpConfigs { get; set; } = new List<FrpConfigBase>();
        public string Token { get; set; }
        private JsonTypeInfo<AppConfig> JsonTypeInfo { get; } = AppConfigSourceGenerationContext.Get().AppConfig;

        public static AppConfig Get()
        {
            //MigrateConfig20250407();
            return Get(AppConfigSourceGenerationContext.Get().AppConfig);
        }

        public void Save()
        {
            Save(JsonTypeInfo);
        }

        protected override void OnLoaded()
        {
            if (FrpConfigs.Count == 0)
            {
                FrpConfigs.Add(new ServerConfig());
                FrpConfigs.Add(new ClientConfig());
            }
        }

        //private static void MigrateConfig20250407()
        //{
        //    //将2025年4月6日之前版本的配置（Servers和Clients合并）迁移至后面的版本
        //    if (!File.Exists(ConfigPathS))
        //    {
        //        return;
        //    }
        //    try
        //    {
        //        var json = JsonNode.Parse(File.ReadAllText(ConfigPathS))?.AsObject();
        //        if (json == null)
        //        {
        //            return;
        //        }
        //        if (!json.ContainsKey("FrpConfigs"))//不是旧版
        //        {
        //            return;
        //        }
        //        var oldConfigs = json["FrpConfigs"].AsArray();
        //        var servers = new JsonArray();
        //        json.Add(nameof(Servers), servers);
        //        var clients = new JsonArray();
        //        json.Add(nameof(Clients), clients);
        //        foreach (JsonObject c in oldConfigs)
        //        {
        //            if (c[nameof(FrpConfigBase.Type)]?.GetValue<string>() == "c" || c.ContainsKey(nameof(ClientConfig.Rules)))
        //            {
        //                clients.Add(c.DeepClone());
        //            }
        //            else
        //            {
        //                servers.Add(c.DeepClone());
        //            }
        //        }
        //        File.WriteAllText(ConfigPathS, json.ToJsonString(new JsonSerializerOptions
        //        {
        //            WriteIndented = true,
        //            PropertyNameCaseInsensitive = true
        //        }));
        //    }
        //    catch(Exception ex)
        //    {
        //        Debug.Assert(false);
        //    }
        //}
    }
}