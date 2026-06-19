using LetMePlayBBPlus;
using MTM101BaldAPI.AssetTools;
using Newtonsoft.Json;
using System.IO;

public static class LMPCfgLoader
{
    public static void LoadAndApply()
    {
        string configPath = Path.Combine(AssetLoader.GetModPath(BasePlugin.Instance), "Config.json");

        LMPCfgData data;

        if (!File.Exists(configPath))
        {
            data = new LMPCfgData();
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(configPath, json);
        }
        else
        {
            string json = File.ReadAllText(configPath);
            data = JsonConvert.DeserializeObject<LMPCfgData>(json);
        }

        LMPCfg.SetDefaults(data);
        LMPCfg.ResetToDefaults();
    }
}