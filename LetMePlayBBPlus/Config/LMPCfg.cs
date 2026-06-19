using System.Collections.Generic;

public static class LMPCfg
{
    private static LMPCfgData _defaults;
    public static float Cooldown { get; set; } = 5f;
    public static float Phase1Duration { get; set; } = 3.3f;
    public static float SpawnInterval { get; set; } = 0.55f;
    public static float MainStopTime { get; set; } = 2f;
    public static float PauseAtEdgeTime { get; set; } = 0.7f;
    public static float SilhouetteSpeed { get; set; } = 800f;
    public static int MaxRecentSilhouettes { get; set; } = 4;
    public static HashSet<string> PausingSilhouettes { get; set; } = new HashSet<string>
    {
        "silhouette0"
    };

    public static void SetDefaults(LMPCfgData defaults)
    {
        _defaults = defaults;
    }

    public static void ResetToDefaults()
    {
        if (_defaults == null) return;

        Cooldown = _defaults.cooldown;
        Phase1Duration = _defaults.phase1Duration;
        SpawnInterval = _defaults.spawnInterval;
        MainStopTime = _defaults.mainStopTime;
        PauseAtEdgeTime = _defaults.pauseAtEdgeTime;
        SilhouetteSpeed = _defaults.silhouetteSpeed;
        MaxRecentSilhouettes = _defaults.maxRecentSilhouettes;
        PausingSilhouettes = new HashSet<string>(_defaults.pausingSilhouettes);
    }
}