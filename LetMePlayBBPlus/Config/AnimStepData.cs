using System.Collections.Generic;

namespace LetMePlayBBPlus
{
    public enum AnimStepType
    {
        Phase1,
        FogFlash,
        Replay,
        Phase2,
        SpawnCharacter,
        SaveAndDisableLights,
        RestoreLights,
        Cooldown
    }

    [System.Serializable]
    public class AnimStep
    {
        public AnimStepType type;
        public float speedMultiplier = 1f;
        public bool enabled = true;
    }
    [System.Serializable]
    public class AnimSequenceParams
    {
        public float? phase1Duration;
        public float? spawnInterval;
        public float? silhouetteSpeed;
        public float? pauseAtEdgeTime;
    }

    [System.Serializable]
    public class AnimSequence
    {
        public AnimSequenceParams parameters = new AnimSequenceParams();
        public List<AnimStep> steps = new List<AnimStep>();
    }

    [System.Serializable]
    public class AnimEditorData
    {
        public Dictionary<string, AnimSequence> sequences = new Dictionary<string, AnimSequence>();
    }
}