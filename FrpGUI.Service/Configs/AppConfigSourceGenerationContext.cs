using FrpGUI.Models;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace FrpGUI.Configs
{
    [JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(AppConfig))]
    internal partial class AppConfigSourceGenerationContext : JsonSerializerContext
    {
        public static AppConfigSourceGenerationContext Get()
        {
            return new AppConfigSourceGenerationContext(new JsonSerializerOptions()
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                Converters = { new FrpConfigJsonConverter() }
            });
        }
    }
}