using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Registers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LetMePlayBBPlus
{
    [BepInPlugin("lolms.bbplusmod.letmeplaybbplus", "Let me play BBPlus", "1.0.0.0")]

    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]

    public class BasePlugin : BaseUnityPlugin
    {

        public static AssetManager assetMan = new AssetManager();

        public void Awake()
        {
            Harmony harmony = new Harmony("lolms.bbplusmod.letmeplaybbplus");

            harmony.PatchAllConditionals();

            LoadingEvents.RegisterOnAssetsLoaded(Info, LoadMyAssets(), LoadingEventOrder.Start);
        }
        IEnumerator LoadMyAssets()
        {
            yield return 4;
            yield return "Loading Silhouettes...";

            string modPath = AssetLoader.GetModPath(this);
            string silhouettesPath = Path.Combine(modPath, "Silhouettes");

            string[] silhouettesFiles = Directory.GetFiles(silhouettesPath, "*.png");

            for (int i = 0; i < silhouettesFiles.Length; i++)
            {
                Texture2D texture = AssetLoader.TextureFromFile(silhouettesFiles[i]);

                Sprite sprite = AssetLoader.SpriteFromTexture2D(texture, 100f);
                sprite.name = $"silhouette{i}";

                assetMan.Add<Sprite>($"silhouette{i}", sprite);
            }

            assetMan.Add<int>("silhouetteCount", silhouettesFiles.Length);

            yield return "Loading Nich Silhouettes...";

            string nicheSilhouettesPath = Path.Combine(AssetLoader.GetModPath(this), "NicheSilhouettes");
            string[] nicheSilhouettesFiles = Directory.GetFiles(nicheSilhouettesPath, "*.png");

            for (int i = 0; i < nicheSilhouettesFiles.Length; i++)
            {
                Texture2D texture = AssetLoader.TextureFromFile(nicheSilhouettesFiles[i]);
                Sprite sprite = AssetLoader.SpriteFromTexture2D(texture, 100f);
                assetMan.Add<Sprite>($"nicheSilhouette{i}", sprite);
            }
            assetMan.Add<int>("nicheSilhouetteCount", nicheSilhouettesFiles.Length);

            yield return "Loading Main Silhouettes...";

            string mainSilhouettesPath = Path.Combine(modPath, "MainSilhouettes");

            string[] mainSilhouettesFiles = Directory.GetFiles(mainSilhouettesPath, "*.png");

            for (int i = 0; i < mainSilhouettesFiles.Length; i++)
            {
                Texture2D texture = AssetLoader.TextureFromFile(Path.Combine(mainSilhouettesPath, $"MainSil{i}.png"));
                Sprite sprite = AssetLoader.SpriteFromTexture2D(texture, 100f);
                assetMan.Add<Sprite>($"mainSilhouette{i}", sprite);
            }
            assetMan.Add<int>("mainSilhouetteCount", mainSilhouettesFiles.Length);

            GameObject systemObj = new GameObject("SilhouettesSystem");
            Object.DontDestroyOnLoad(systemObj);
            systemObj.AddComponent<SilhouettesSystem>();
            systemObj.AddComponent<AudioSourceManagerMain>();

            yield return "Loading audio...";

            string audioPathType1 = Path.Combine(modPath, "Audio", "AnimationCycleType1");
            if (Directory.Exists(audioPathType1))
            {
                string[] audioFilesType1 = Directory.GetFiles(audioPathType1, "*.*");
                for (int i = 0; i < audioFilesType1.Length; i++)
                {
                    AudioClip clip = AssetLoader.AudioClipFromFile(audioFilesType1[i]);
                    SoundObject soundObj = ObjectCreators.CreateSoundObject(
                        clip,
                        $"Mus_Sil_Type1_{i}",
                        SoundType.Effect,
                        Color.white
                    );
                    soundObj.subtitle = false; 
                    assetMan.Add<SoundObject>($"animAudioType1_{i}", soundObj);
                }
                assetMan.Add<int>("audioCountType1", audioFilesType1.Length);
            }

            string audioPathType2 = Path.Combine(modPath, "Audio", "AnimationCycleType2");
            if (Directory.Exists(audioPathType2))
            {
                string[] audioFilesType2 = Directory.GetFiles(audioPathType2, "*.*");
                for (int i = 0; i < audioFilesType2.Length; i++)
                {
                    AudioClip clip = AssetLoader.AudioClipFromFile(audioFilesType2[i]);
                    SoundObject soundObj = ObjectCreators.CreateSoundObject(
                        clip,
                        $"Mus_Sil_Type2_{i}",
                        SoundType.Effect,
                        Color.white
                    );
                    soundObj.subtitle = false;
                    assetMan.Add<SoundObject>($"animAudioType2_{i}", soundObj);
                }
                assetMan.Add<int>("audioCountType2", audioFilesType2.Length);
            }
            yield break;
        }
    }
}
     