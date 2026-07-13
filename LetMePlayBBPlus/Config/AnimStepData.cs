using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LetMePlayBBPlus
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AnimStepType
    {
        Phase1,
        FogFlash,
        Replay,
        Phase2,
        SpawnCharacter,
        SaveAndDisableLights,
        RestoreLights,
        StartShakingShaders,
        StopShakingShaders,
        StartFlashingShaders,
        StopFlashingShaders,
        StartIncreasingAnger,
        StopIncreasingAnger,
        SaveAndHideMap,
        RestoreMap,
        SavePositions,
        Cooldown,
    }

    [JsonConverter(typeof(AnimStepConverter))]
    public class AnimStep
    {
        public AnimStepType type;
        public float speedMultiplier = 1f;
        public bool enabled = true;
        public float intensity = 0.10f;
        public float interval = 0.5f;
        public float decaySpeed = 4f;
        public float duration = 0f;
        public int tilesAmount = 20;
    }

    public class AnimSequenceParams
    {
        public float? phase1Duration;
        public float? spawnInterval;
        public float? silhouetteSpeed;
        public float? mainStopTime;
        public float? pauseAtEdgeTime;
    }

    public class AnimSequence
    {
        public AnimSequenceParams parameters = new AnimSequenceParams();
        public List<AnimStep> steps = new List<AnimStep>();
    }

    public class AnimEditorData
    {
        public Dictionary<string, AnimSequence> sequences = new Dictionary<string, AnimSequence>();
    }
}