using System.Collections.Generic;

[System.Serializable]
public class LMPCfgData
{
    public float cooldown = 5f;
    public float phase1Duration = 3.3f;
    public float spawnInterval = 0.55f;
    public float mainStopTime = 2f;
    public float pauseAtEdgeTime = 0.7f;
    public float silhouetteSpeed = 800f;
    public int maxRecentSilhouettes = 4;

    public List<string> pausingSilhouettes = new List<string>
    {
        "silhouette0"
    };
}