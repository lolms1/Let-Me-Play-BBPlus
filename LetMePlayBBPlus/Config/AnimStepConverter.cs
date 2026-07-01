using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace LetMePlayBBPlus
{
    public class AnimStepConverter : JsonConverter<AnimStep>
    {
        public override AnimStep ReadJson(JsonReader reader, Type objectType, AnimStep existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);

            AnimStep step = new AnimStep();

            JToken typeToken = obj["type"];
            step.type = typeToken.Type == JTokenType.Integer
                ? (AnimStepType)typeToken.Value<int>()
                : (AnimStepType)Enum.Parse(typeof(AnimStepType), typeToken.Value<string>());

            switch (step.type)
            {
                case AnimStepType.Replay:
                case AnimStepType.Cooldown:
                    if (obj["speedMultiplier"] != null)
                        step.speedMultiplier = obj["speedMultiplier"].Value<float>();
                    break;

                case AnimStepType.FogFlash:
                    if (obj["speedMultiplier"] != null)
                        step.speedMultiplier = obj["speedMultiplier"].Value<float>();
                    if (obj["enabled"] != null)
                        step.enabled = obj["enabled"].Value<bool>();
                    break;
                case AnimStepType.StartShakingShaders:
                    if (obj["intensity"] != null)
                        step.intensity = obj["intensity"].Value<float>();
                    if (obj["interval"] != null)
                        step.interval = obj["interval"].Value<float>();
                    if (obj["decaySpeed"] != null)
                        step.decaySpeed = obj["decaySpeed"].Value<float>();
                    if (obj["duration"] != null)
                        step.duration = obj["duration"].Value<float>();
                    break;
                case AnimStepType.StartIncreasingAnger:
                    if (obj["interval"] != null)
                        step.interval = obj["interval"].Value<float>();
                    break;
                case AnimStepType.SaveAndHideMap:
                    if (obj["tilesAmount"] != null)
                        step.tilesAmount = obj["tilesAmount"].Value<int>();
                    break;
                case AnimStepType.StartFlashingShaders:
                    if (obj["duration"] != null)
                        step.duration = obj["duration"].Value<float>();
                    if (obj["intensity"] != null)
                        step.intensity = obj["intensity"].Value<float>();
                    break;
            }

            return step;
        }

        public override void WriteJson(JsonWriter writer, AnimStep step, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("type");
            writer.WriteValue(step.type.ToString());

            switch (step.type)
            {
                case AnimStepType.Replay:
                case AnimStepType.Cooldown:
                    writer.WritePropertyName("speedMultiplier");
                    writer.WriteValue(step.speedMultiplier);
                    break;

                case AnimStepType.FogFlash:
                    writer.WritePropertyName("speedMultiplier");
                    writer.WriteValue(step.speedMultiplier);
                    writer.WritePropertyName("enabled");
                    writer.WriteValue(step.enabled);
                    break;

                case AnimStepType.StartShakingShaders:
                    writer.WritePropertyName("intensity");
                    writer.WriteValue(step.intensity);
                    writer.WritePropertyName("interval");
                    writer.WriteValue(step.interval);
                    writer.WritePropertyName("decaySpeed");
                    writer.WriteValue(step.decaySpeed);
                    writer.WritePropertyName("duration");
                    writer.WriteValue(step.duration);
                    break;
                case AnimStepType.StartIncreasingAnger:
                    writer.WritePropertyName("interval");
                    writer.WriteValue(step.interval);
                    break;
                case AnimStepType.SaveAndHideMap:
                    writer.WritePropertyName("tilesAmount");
                    writer.WriteValue(step.tilesAmount);
                    break;
                case AnimStepType.StartFlashingShaders:
                    writer.WritePropertyName("duration");
                    writer.WriteValue(step.duration);
                    writer.WritePropertyName("intensity");
                    writer.WriteValue(step.intensity);
                    break;
            }

            writer.WriteEndObject();
        }
    }
}