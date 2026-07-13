using Newtonsoft.Json;

[System.Serializable]
public class LMPCfgData
{
    public float cooldown = 35f;
    public float phase1Duration = 15f;
    public float spawnInterval = 0.50f;
    public float mainStopTime = 2f;
    public float pauseAtEdgeTime = 1f;
    public float silhouetteSpeed = 800f;
    public float coefficient = 1f;
    public float cooldownCoefficient = 1.5f;
    public float multiplierLogBase = 6f;
    public float cooldownMultiplierLogBase = 6f;
    public float starterAnger = 1.5f;

    public int maxRecentSilhouettes = 8;
    public float CycleType2AngerRequirement = 5.0f;
    public int CycleType2Chance = 3;

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public HashSet<string> pausingSilhouettes = new HashSet<string>
    {
        "silhouette0",
        "silhouette1"
    };
    public bool RandomAudioSelecting = true;
    public int AudioIndex = 0;
}