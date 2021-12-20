using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace zRDPClip
{
    public class Configurator
    {
        public static string ConfigJsonPath { get; } = $@"{AppContext.BaseDirectory}\config.json";

        public static void Generate()
        {
            if (!File.Exists(ConfigJsonPath))
            {
                var jsonString = JsonSerializer.Serialize(new ConfigJson());
                File.WriteAllText(ConfigJsonPath, jsonString);
            }
        }

        public static void SetUserName(string userName)
        {
            var jsonString = File.ReadAllText(ConfigJsonPath);
            var configJson =  JsonSerializer.Deserialize<ConfigJson>(jsonString);
            configJson.UserName = userName;
            jsonString = JsonSerializer.Serialize(configJson);
            File.WriteAllText(ConfigJsonPath, jsonString);
        }

        public static string GetUserName()
        {
            var jsonString = File.ReadAllText(ConfigJsonPath);
            var configJson = JsonSerializer.Deserialize<ConfigJson>(jsonString);
            return configJson.UserName;
        }

        public static string GetUrl()
        {
            var jsonString = File.ReadAllText(ConfigJsonPath);
            var configJson = JsonSerializer.Deserialize<ConfigJson>(jsonString);
            return configJson.Url;
        }
    }

    public class ConfigJson
    {
        public string Url { get; set; } = "https://zcliprdp-server.herokuapp.com/";
        public string UserName { get; set; }
    }
}
