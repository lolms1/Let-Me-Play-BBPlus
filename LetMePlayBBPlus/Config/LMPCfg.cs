public static class LMPCfg
{
    private static LMPCfgData _defaults;
    public static float Cooldown { get; set; }
    public static float Phase1Duration { get; set; }
    public static float SpawnInterval { get; set; }
    public static float MainStopTime { get; set; }
    public static float PauseAtEdgeTime { get; set; }
    public static float SilhouetteSpeed { get; set; }
    public static float Coefficient { get; set; }
    public static float CooldownCoefficient { get; set; }
    public static float MultiplierLogBase { get; set; }
    public static float CooldownMultiplierLogBase { get; set; }
    public static float StarterAnger { get; set; }
    public static int MaxRecentSilhouettes { get; set; }
    public static float CycleType2AngerRequirement { get; set; }
    public static int CycleType2Chance { get; set; }
    public static HashSet<string> PausingSilhouettes { get; set; } = new HashSet<string>{};

    public static bool RandomAudioSelecting { get; set; }
    public static int AudioIndex { get; set; }

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
        Coefficient = _defaults.coefficient;
        CooldownCoefficient = _defaults.cooldownCoefficient;
        MultiplierLogBase = _defaults.multiplierLogBase;
        CooldownMultiplierLogBase = _defaults.multiplierLogBase;
        StarterAnger = _defaults.starterAnger;
        MaxRecentSilhouettes = _defaults.maxRecentSilhouettes;
        CycleType2AngerRequirement = _defaults.CycleType2AngerRequirement;
        CycleType2Chance = _defaults.CycleType2Chance;
        PausingSilhouettes = _defaults.pausingSilhouettes;
        RandomAudioSelecting = _defaults.RandomAudioSelecting;
        AudioIndex = _defaults.AudioIndex;
    }
}