using HarmonyLib;
using MTM101BaldAPI.AssetTools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LetMePlayBBPlus
{
    public class SilhouettesSystem : MonoBehaviour
    {

        public static SilhouettesSystem Instance { get; private set; }

        private float cooldown = LMPCfg.Cooldown;
        private float phase1Duration = LMPCfg.Phase1Duration;
        private float spawnInterval = LMPCfg.SpawnInterval;
        private float mainStopTime = LMPCfg.MainStopTime;
        private float pauseAtEdgeTime = LMPCfg.PauseAtEdgeTime;

        private float silhouetteSpeed = LMPCfg.SilhouetteSpeed;

        private static readonly HashSet<string> pausingSilhouettes = LMPCfg.PausingSilhouettes;

        private float timer;
        private bool isRunning;
        private bool isInitialized;
        private int lastMainSilhouetteIndex = -1;

        private Canvas canvas;
        private FogManager fogMan;
        private AudioSourceManagerMain audSourceManMain;
        private ReplayManager repMan;

        private bool isInGame;
        private int currentLevel = -1;

        private readonly Queue<string> recentSilhouettes = new Queue<string>();
        private int maxRecentSilhouettes = LMPCfg.MaxRecentSilhouettes;

        private readonly List<string> savedSilhouetteOrder = new List<string>();

        private enum MoveMode
        {
            PassThrough,
            PauseAtEdge,
            StopInCenter,
        }

        void Start()
        {
            DontDestroyOnLoad(gameObject);
            Instance = this;

            canvas = GetComponent<Canvas>() ?? FindObjectOfType<Canvas>();
            audSourceManMain = GetComponent<AudioSourceManagerMain>();

            timer = cooldown;
            isInitialized = true;
        }

        void Update()
        {
            if (!isInitialized) return;

            int level = GetCurrentLevel();
            bool inGame = IsInGame();

            if (level != currentLevel)
            {
                currentLevel = level;
                isInGame = false;
                isRunning = false;
            }

            if (inGame && !isInGame)
            {
                isInGame = true;
                timer = cooldown;
            }
            else if (!inGame && isInGame)
            {
                isInGame = false;
                CancelCycle();
            }

            if (!isInGame || isRunning) return;

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                timer = cooldown;
                StartCoroutine(RunAnimationCycleType2());
            }
        }

        private bool IsInGame()
        {
            if (Singleton<CoreGameManager>.Instance == null) return false;
            if (Singleton<CoreGameManager>.Instance.sceneObject == null) return false;
            if (Singleton<CoreGameManager>.Instance.sceneObject.levelTitle == "PIT") return false;
            return GameObject.FindObjectOfType<DigitalClock>() != null;
        }

        private int GetCurrentLevel()
        {
            if (Singleton<CoreGameManager>.Instance == null) return -1;
            if (Singleton<CoreGameManager>.Instance.sceneObject == null) return -1;
            return Singleton<CoreGameManager>.Instance.sceneObject.levelNo;
        }

        private FogManager GetFogManager()
        {
            if (fogMan == null)
                fogMan = new FogManager(Singleton<BaseGameManager>.Instance.Ec);
            return fogMan;
        }
        private ReplayManager GetRepManager()
        {
            if (repMan == null)
                repMan = new ReplayManager(Singleton<BaseGameManager>.Instance.Ec, this);
            return repMan;
        }

        private AudioSourceManagerMain GetAudSourceManMain()
        {
            if (audSourceManMain == null)
                audSourceManMain = new AudioSourceManagerMain();
            return audSourceManMain;
        }

        private void CancelCycle()
        {
            StopAllCoroutines();
            isRunning = false;

            if (canvas != null)
            {
                foreach (Transform child in canvas.transform)
                {
                    string name = child.name;
                    if (name == "FastSilhouette" || name == "MainSilhouette" || name == "ReplaySilhouette")
                        Destroy(child.gameObject);
                }
            }

            if (fogMan != null)
            {
                fogMan.DisableFog(this);
                fogMan = null;
            }

            if (repMan != null)
            {
                repMan.Reset();
                repMan = null;
            }
        }

        private IEnumerator RunAnimationCycleType2()
        {
            isRunning = true;
            fogMan = GetFogManager();
            repMan = GetRepManager();
            repMan.EnableTimeScale();
            audSourceManMain = GetAudSourceManMain();

            repMan.SaveAllPositions();

            audSourceManMain.PlayMusic(BasePlugin.assetMan.Get<SoundObject>("animAudioType2_0")); // TODO : add random selecting

            yield return StartCoroutine(Phase1_FastSilhouettes(recordTo: savedSilhouetteOrder));

            yield return StartCoroutine(FogFlash(speedMultiplier: 2f));

            yield return StartCoroutine(Phase1_ReplaySilhouettes(savedSilhouetteOrder, speedMultiplier: 2f));

            yield return StartCoroutine(FogFlash(speedMultiplier: 0.7f));

            yield return StartCoroutine(Phase1_ReplaySilhouettes(savedSilhouetteOrder, speedMultiplier: 0.7f));

            yield return StartCoroutine(Phase2_MainSilhouette());

            CharacterSpawnSystem.SpawnForSilhouette(lastMainSilhouetteIndex);

            repMan.SetTimeScaleSmooth(1f, 0.03f);

            isRunning = false;
        }

        private IEnumerator RunAnimationCycle()
        {
            isRunning = true;

            fogMan = GetFogManager();

            Debug.Log($"isingame {IsInGame()}, scene level {Singleton<CoreGameManager>.Instance.sceneObject.levelNo}");
            fogMan.EnableFog(this);

            yield return StartCoroutine(Phase1_FastSilhouettes());
            yield return StartCoroutine(Phase2_MainSilhouette());

            CharacterSpawnSystem.SpawnForSilhouette(lastMainSilhouetteIndex);

            fogMan.DisableFog(this);

            isRunning = false;
        }
        private IEnumerator FogFlash(float speedMultiplier)
        {
            fogMan.EnableFog(this);
            yield return new WaitForSeconds(0.5f);
            repMan.SetTimeScaleInstant(speedMultiplier);
            repMan.RestorePlayerRotations();
            repMan.RestoreAllPositions();
            fogMan.DisableFog(this);
            yield return new WaitForSeconds(0.35f);
        }

        private IEnumerator Phase1_FastSilhouettes(List<string> recordTo = null)
        {
            if (recordTo != null) recordTo.Clear();

            float elapsed = 0f;
            float nextSpawn = 0f;
            bool lastWasPausing = false;

            while (elapsed < phase1Duration)
            {
                if (nextSpawn <= 0f)
                {
                    lastWasPausing = false;
                    float extraDelay = 0f;

                    yield return StartCoroutine(SpawnFastSilhouette(recordTo, result =>
                    {
                        extraDelay = result;
                        lastWasPausing = result > 0f;
                    }));

                    nextSpawn = spawnInterval + extraDelay;
                }

                nextSpawn -= Time.deltaTime;
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (lastWasPausing)
                yield return new WaitForSeconds(pauseAtEdgeTime);

            yield return new WaitForSeconds(0.7f);
        }

        private IEnumerator Phase1_ReplaySilhouettes(List<string> spriteKeys, float speedMultiplier)
        {
            float effectiveSpawnInterval = spawnInterval / speedMultiplier;

            int index = 0;
            float nextSpawn = 0f;
            bool lastWasPausing = false;

            while (index < spriteKeys.Count)
            {
                if (nextSpawn <= 0f)
                {
                    string key = spriteKeys[index];
                    bool isPausing = pausingSilhouettes.Contains(key);
                    lastWasPausing = isPausing;
                    float extraDelay = isPausing ? pauseAtEdgeTime / speedMultiplier : 0f;

                    MoveMode mode = isPausing ? MoveMode.PauseAtEdge : MoveMode.PassThrough;
                    SpawnSilhouetteByKey(key, mode, speedMultiplier);

                    index++;
                    nextSpawn = effectiveSpawnInterval + extraDelay;
                }

                nextSpawn -= Time.deltaTime;
                yield return null;
            }

            if (lastWasPausing)
                yield return new WaitForSeconds(pauseAtEdgeTime);

            yield return new WaitForSeconds(0.6f);
        }

        private IEnumerator SpawnFastSilhouette(List<string> recordTo, System.Action<float> onComplete)
        {
            string key;
            Sprite sprite = PickRandomSprite(isMain: false, out key);

            if (recordTo != null) recordTo.Add(key);

            bool isPausing = pausingSilhouettes.Contains(sprite.name);
            MoveMode mode = isPausing ? MoveMode.PauseAtEdge : MoveMode.PassThrough;

            LaunchSilhouette(sprite, "FastSilhouette", mode, speedMultiplier: 1f);
            onComplete(isPausing ? pauseAtEdgeTime : 0f);
            yield return null;
        }

        private IEnumerator Phase2_MainSilhouette()
        {
            string key;
            Sprite sprite = PickRandomSprite(isMain: true, out key);
            if (sprite == null) yield break;

            yield return LaunchSilhouette(sprite, "MainSilhouette", MoveMode.StopInCenter, speedMultiplier: 1f);
        }

        private void SpawnSilhouetteByKey(string spriteKey, MoveMode mode, float speedMultiplier)
        {
            Sprite sprite = BasePlugin.assetMan.Get<Sprite>(spriteKey);
            if (sprite == null) return;
            LaunchSilhouette(sprite, "ReplaySilhouette", mode, speedMultiplier);
        }
        private Coroutine LaunchSilhouette(Sprite sprite, string objName, MoveMode mode, float speedMultiplier)
        {
            (RawImage rawImage, RectTransform rect) = BuildSilhouetteVisual(sprite, objName);

            float startX = Screen.width / 4.5f;

            rect.anchoredPosition = new Vector2(startX, 0f);

            return StartCoroutine(MoveSilhouette(rawImage, rect, mode, speedMultiplier));
        }

        private (RawImage rawImage, RectTransform rect) BuildSilhouetteVisual(Sprite sprite, string objName)
        {
            GameObject obj = new GameObject(objName);
            obj.transform.SetParent(canvas.transform, false);
            obj.transform.SetAsFirstSibling();

            RawImage rawImage = obj.AddComponent<RawImage>();
            RectTransform rect = obj.GetComponent<RectTransform>();
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();

            rawImage.texture = sprite.texture;

            float canvasHeight = canvasRect.rect.height;
            float canvasWidth = canvasRect.rect.width;
            float spriteRatio = (float)sprite.texture.width / sprite.texture.height;

            float targetHeight = canvasHeight;
            float targetWidth = targetHeight * spriteRatio;

            float maxWidth = canvasWidth * 0.4f;
            if (targetWidth > maxWidth)
            {
                targetWidth = maxWidth;
            }

            if (targetWidth > canvasWidth)
            {
                targetWidth = canvasWidth;
            }

            rect.sizeDelta = new Vector2(targetWidth, targetHeight);

            return (rawImage, rect);
        }

        private IEnumerator MoveSilhouette(RawImage rawImage, RectTransform rect, MoveMode mode, float speedMultiplier)
        {
            float speed = silhouetteSpeed * speedMultiplier;
            float endX = -Screen.height * 0.5f;

            switch (mode)
            {
                case MoveMode.StopInCenter:
                    yield return MoveUntilX(rect, speed, targetX: 0f, clamp: true);
                    yield return new WaitForSeconds(mainStopTime);
                    break;

                case MoveMode.PauseAtEdge:
                    float rightEdgeX = canvas.GetComponent<RectTransform>().rect.width / 4f;
                    yield return MoveUntilX(rect, speed, targetX: rightEdgeX, clamp: true);
                    yield return new WaitForSeconds(pauseAtEdgeTime / speedMultiplier);
                    yield return MoveUntilX(rect, speed, targetX: endX, clamp: false);
                    break;

                case MoveMode.PassThrough:
                default:
                    yield return MoveUntilX(rect, speed, targetX: endX, clamp: false);
                    break;
            }

            Destroy(rawImage.gameObject);
        }

        private IEnumerator MoveUntilX(RectTransform rect, float speed, float targetX, bool clamp)
        {
            while (rect.anchoredPosition.x > targetX)
            {
                Vector2 pos = rect.anchoredPosition;
                pos.x -= speed * Time.deltaTime;
                if (clamp && pos.x < targetX) pos.x = targetX;
                rect.anchoredPosition = pos;
                yield return null;
            }
        }


        private Sprite PickRandomSprite(bool isMain, out string key)
        {
            key = "";

            if (isMain)
            {
                int count = BasePlugin.assetMan.Get<int>("mainSilhouetteCount");
                if (count <= 0) return null;

                int index = 2; // TODO: make random selecting
                lastMainSilhouetteIndex = index;
                key = $"mainSilhouette{index}";
                return BasePlugin.assetMan.Get<Sprite>(key);
            }

            int nicheCount = BasePlugin.assetMan.Get<int>("nicheSilhouetteCount");
            if (nicheCount > 0 && Random.Range(0, 200) == 43)
                return PickUniqueSprite("nicheSilhouette", nicheCount, out key);

            int regularCount = BasePlugin.assetMan.Get<int>("silhouetteCount");
            if (regularCount <= 0) return null;

            return PickUniqueSprite("silhouette", regularCount, out key);
        }

        private Sprite PickUniqueSprite(string prefix, int count, out string chosenKey)
        {
            var available = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                string k = $"{prefix}{i}";
                if (!recentSilhouettes.Contains(k))
                    available.Add(k);
            }

            chosenKey = available.Count > 0
                ? available[Random.Range(0, available.Count)]
                : $"{prefix}{Random.Range(0, count)}";

            AddToRecentQueue(chosenKey);
            return BasePlugin.assetMan.Get<Sprite>(chosenKey);
        }

        private void AddToRecentQueue(string key)
        {
            recentSilhouettes.Enqueue(key);
            if (recentSilhouettes.Count > maxRecentSilhouettes)
                recentSilhouettes.Dequeue();
        }

        public void PauseMusic()
        {
            audSourceManMain.PauseMusic();
        }

        public void ResumeMusic()
        {
            audSourceManMain.ResumeMusic();
        }
    }
}