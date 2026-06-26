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
                case AnimStepType.StartShakingWall:
                    if (obj["intensity"] != null)
                        step.speedMultiplier = obj["intensity"].Value<float>();
                    if (obj["interval"] != null)
                        step.speedMultiplier = obj["interval"].Value<float>();
                    if (obj["decaySpeedl"] != null)
                        step.speedMultiplier = obj["decaySpeed"].Value<float>();
                    if (obj["duration"] != null)
                        step.speedMultiplier = obj["duration"].Value<float>();
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
            }

            writer.WriteEndObject();
        }
    }
}