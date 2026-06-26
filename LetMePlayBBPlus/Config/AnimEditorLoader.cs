using LetMePlayBBPlus;
using MTM101BaldAPI.AssetTools;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class AnimEditorLoader
{
    private static string filePath;

    public static void LoadAndApply()
    {
        filePath = Path.Combine(AssetLoader.GetModPath(BasePlugin.Instance), "AnimEditor.json");

        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };

        AnimEditorData data;

        if (!File.Exists(filePath))
        {
            data = new AnimEditorData();
        }
        else
        {
            string json = File.ReadAllText(filePath);
            data = JsonConvert.DeserializeObject<AnimEditorData>(json, settings) ?? new AnimEditorData();
        }

        bool changed = FillMissingSequences(data);
        changed |= FillMissingParameters(data);

        if (changed || !File.Exists(filePath))
        {
            string json = JsonConvert.SerializeObject(data, settings);
            File.WriteAllText(filePath, json);
        }

        Validate(data);

        AnimEditor.SetSequences(data.sequences);
    }
    private static bool FillMissingSequences(AnimEditorData data)
    {
        bool changed = false;

        string audioPath = Path.Combine(
            AssetLoader.GetModPath(BasePlugin.Instance), "Audio", "AnimationCycleType2");

        if (!Directory.Exists(audioPath))
            return false;

        string[] files = Directory.GetFiles(audioPath, "*.*");

        for (int i = 0; i < files.Length; i++)
        {
            string key = $"animAudioType2_{i}";
            if (!data.sequences.ContainsKey(key))
            {
                data.sequences[key] = CreateDefaultSequence();
                changed = true;
            }
        }

        return changed;
    }

    private static bool FillMissingParameters(AnimEditorData data)
    {
        bool changed = false;

        foreach (var kvp in data.sequences)
        {
            if (kvp.Value.parameters == null)
            {
                kvp.Value.parameters = new AnimSequenceParams();
                changed = true;
            }
        }

        return changed;
    }

    private static AnimSequence CreateDefaultSequence()
    {
        return new AnimSequence
        {
            parameters = new AnimSequenceParams(),
            steps = new List<AnimStep>
            {
                new AnimStep { type = AnimStepType.Phase1                                  },
                new AnimStep { type = AnimStepType.FogFlash, speedMultiplier = 2f         },
                new AnimStep { type = AnimStepType.Replay,   speedMultiplier = 2f         },
                new AnimStep { type = AnimStepType.FogFlash, speedMultiplier = 0.7f       },
                new AnimStep { type = AnimStepType.Replay,   speedMultiplier = 0.7f       },
                new AnimStep { type = AnimStepType.Phase2                                  },
                new AnimStep { type = AnimStepType.SpawnCharacter                          },
            }
        };
    }

    private static void Validate(AnimEditorData data)
    {
        foreach (var kvp in data.sequences)
        {
            string audioKey = kvp.Key;
            List<AnimStep> steps = kvp.Value.steps;

            bool phase1Seen = false;
            bool phase2Seen = false;

            for (int i = 0; i < steps.Count; i++)
            {
                AnimStep step = steps[i];

                switch (step.type)
                {
                    case AnimStepType.Phase1:
                        phase1Seen = true;
                        break;

                    case AnimStepType.Phase2:
                        phase2Seen = true;
                        break;

                    case AnimStepType.Replay:
                        if (!phase1Seen)
                        {
                            Debug.LogError(
                                $"Sequence \"{audioKey}\": " +
                                $"Replay at step {i} comes before Phase1! " +
                                $"Replacing with Phase1 automatically."
                            );
                            steps[i] = new AnimStep { type = AnimStepType.Phase1 };
                            phase1Seen = true;
                        }
                        break;

                    case AnimStepType.SpawnCharacter:
                        if (!phase2Seen)
                        {
                            Debug.LogError(
                                $"Sequence \"{audioKey}\": " +
                                $"SpawnCharacter at step {i} comes before Phase2! " +
                                $"Replacing with Phase2 automatically."
                            );
                            steps[i] = new AnimStep { type = AnimStepType.Phase2 };
                            phase2Seen = true;
                        }
                        break;
                }
            }
        }
    }
}