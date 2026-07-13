using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace LetMePlayBBPlus
{
    public class SilhouettesSystem : MonoBehaviour
    {

        public static SilhouettesSystem Instance { get; private set; }

        private float basePhase1Duration;
        private float baseSpawnInterval;
        private float baseMainStopTime;
        private float baseSilhouetteSpeed;
        private float basePauseAtEdgeTime;
        private float baseCooldown;

        private float currentPhase1Duration;
        private float currentSpawnInterval;
        private float currentMainStopTime;
        private float currentSilhouetteSpeed;
        private float currentPauseAtEdgeTime;
        private float currentCooldown;
        private float coefficient;
        private float cooldownCoefficient;
        private float multiplierLogBase;
        private float cooldownMultiplierLogBase;
        private float starterAnger;

        private HashSet<string> pausingSilhouettes;

        private float timer;
        private float anger;
        private float savedAnger;
        private float cycleType2AngerRequirement;
        private int cycleType2Chance;
        private bool isRunning;
        private bool isInitialized;
        private bool activeAngerIncrease = false;
        private int lastMainSilhouetteIndex = -1;

        private Canvas canvas;
        private FogManager fogMan;
        private AudioSourceManagerMain audSourceManMain;
        private ReplayManager repMan;
        private LightManager lightMan;
        private WallShakeManager wallMan;
        private MapManager mapMan;

        private bool isInGame;
        private int currentLevel = -1;

        private readonly Queue<string> recentSilhouettes = new Queue<string>();
        private int maxRecentSilhouettes;

        private readonly List<string> savedSilhouetteOrder = new List<string>();

        private bool randomAudioSelecting;
        private int audioIndex;

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

            LoadBaseValues();
            currentCooldown = baseCooldown;
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
                CancelCycle();
                return;
            }

            if (inGame && !isInGame)
            {
                isInGame = true;
                timer = currentCooldown;
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
                timer = currentCooldown;
                anger = GetAnger();
                if (anger >= cycleType2AngerRequirement && UnityEngine.Random.Range(0, cycleType2Chance) == 0)
                {
                    StartCoroutine(RunAnimationCycleType2());
                }
                else
                {
                    StartCoroutine(RunAnimationCycleType1());
                }
            }
        }

        private IEnumerator RunAnimationCycleType1()
        {
            isRunning = true;
            CalculateCurrentCooldown();

            string audioKey = PickAudioKey("animAudioType1", count: BasePlugin.assetMan.Get<int>("audioCountType1"));
            PlayAudio(audioKey);

            yield return StartCoroutine(Phase1_FastSilhouettes());
            yield return StartCoroutine(Phase2_MainSilhouette());

            CharacterSpawnSystem.SpawnForSilhouette(lastMainSilhouetteIndex);
            audSourceManMain.StopMusic();

            isRunning = false;
        }

        private IEnumerator RunAnimationCycleType2()
        {
            isRunning = true;

            string audioKey = PickAudioKey("animAudioType2", count: BasePlugin.assetMan.Get<int>("audioCountType2"));
            PlayAudio(audioKey);

            AnimSequence sequence = AnimEditor.GetSequence(audioKey);

            fogMan = GetFogManager();
            repMan = GetRepManager();
            lightMan = GetLightManager();
            wallMan = GetWallShakeManager();
            mapMan = GetMapManager();
            repMan.EnableTimeScale();
            repMan.SaveAllPositions();

            ApplySequenceParams(sequence.parameters);
            yield return StartCoroutine(ExecuteSequence(sequence));

            repMan.SetTimeScaleSmooth(1f, 0.03f);
            wallMan.StopShake();    
            wallMan.StopFlash();
            audSourceManMain.StopMusic();
            mapMan.RestoreMap();
            StopAngerIncrease();

            isRunning = false;
        }
        private IEnumerator ExecuteSequence(AnimSequence sequence)
        {
            foreach (AnimStep step in sequence.steps)
            {
                switch (step.type)
                {
                    case AnimStepType.Phase1: // 0
                        yield return StartCoroutine(Phase1_FastSilhouettes(recordTo: savedSilhouetteOrder));
                        break;

                    case AnimStepType.FogFlash: // 1
                        yield return StartCoroutine(FogFlash(step.speedMultiplier, step.enabled));
                        break;

                    case AnimStepType.Replay: // 2
                        yield return StartCoroutine(Phase1_ReplaySilhouettes(savedSilhouetteOrder, step.speedMultiplier));
                        break;

                    case AnimStepType.Phase2: // 3
                        yield return StartCoroutine(Phase2_MainSilhouette());
                        break;

                    case AnimStepType.SpawnCharacter: // 4
                        CharacterSpawnSystem.SpawnForSilhouette(lastMainSilhouetteIndex);
                        break;

                    case AnimStepType.SaveAndDisableLights: // 5
                        lightMan.DisableAllLights();
                        break;

                    case AnimStepType.RestoreLights: // 6
                        lightMan.RestoreLights();
                        break;

                    case AnimStepType.StartShakingShaders: // 7
                        wallMan.StartShake(step.intensity, step.interval, step.decaySpeed, step.duration);
                        break;

                    case AnimStepType.StopShakingShaders: // 8
                        wallMan.StopShake();
                        break;

                    case AnimStepType.StartFlashingShaders: // 9
                        wallMan.StartFlash(step.duration, step.intensity);
                        break;

                    case AnimStepType.StopFlashingShaders: // 10
                        wallMan.StopFlash();
                        break;

                    case AnimStepType.StartIncreasingAnger: // 11
                        StartAngerIncrease(step.intensity);
                        break;

                    case AnimStepType.StopIncreasingAnger: // 12
                        StopAngerIncrease();
                        break;

                    case AnimStepType.SaveAndHideMap: // 13
                        mapMan.SaveAndHideMap(step.tilesAmount);
                        break; 

                    case AnimStepType.RestoreMap: // 14
                        mapMan.RestoreMap();
                        break;

                    case AnimStepType.SavePositions: // 15
                        repMan.SaveAllPositions();
                        break;

                    case AnimStepType.Cooldown: // 16
                        yield return new WaitForSeconds(step.speedMultiplier);
                        break;

                }
            }
        }

        public void PlayAudio(string key)
        {
            audSourceManMain = GetAudSourceManMain();
            audSourceManMain.PlayMusic(BasePlugin.assetMan.Get<SoundObject>(key));
        }

        private string PickAudioKey(string prefix, int count)
        {
            if (count <= 0) return $"{prefix}_0";
            int index;
            if (!randomAudioSelecting)
            {
                index = audioIndex;
            }
            else
            {
                index = UnityEngine.Random.Range(0, count);
            }
            return $"{prefix}_{index}";
        }
        private void LoadBaseValues()
        {
            baseCooldown = LMPCfg.Cooldown;
            basePhase1Duration = LMPCfg.Phase1Duration;
            baseSpawnInterval = LMPCfg.SpawnInterval;
            baseMainStopTime = LMPCfg.MainStopTime;
            baseSilhouetteSpeed = LMPCfg.SilhouetteSpeed;
            basePauseAtEdgeTime = LMPCfg.PauseAtEdgeTime;
            coefficient = LMPCfg.Coefficient;
            cooldownCoefficient = LMPCfg.CooldownCoefficient;
            multiplierLogBase = LMPCfg.MultiplierLogBase;
            cooldownMultiplierLogBase = LMPCfg.CooldownMultiplierLogBase;
            starterAnger = LMPCfg.StarterAnger;
            maxRecentSilhouettes = LMPCfg.MaxRecentSilhouettes;
            cycleType2AngerRequirement = LMPCfg.CycleType2AngerRequirement;
            cycleType2Chance = LMPCfg.CycleType2Chance;
            randomAudioSelecting = LMPCfg.RandomAudioSelecting;
            audioIndex = LMPCfg.AudioIndex;

            pausingSilhouettes = LMPCfg.PausingSilhouettes;

        }

        private void ApplySequenceParams(AnimSequenceParams p)
        {
            currentPhase1Duration = p.phase1Duration ?? basePhase1Duration;
            currentSpawnInterval = p.spawnInterval ?? baseSpawnInterval;
            currentSilhouetteSpeed = p.silhouetteSpeed ?? baseSilhouetteSpeed;
            currentPauseAtEdgeTime = p.pauseAtEdgeTime ?? basePauseAtEdgeTime;
        }
        private void CalculateCurrentCooldown()
        {
            // we got anger already in Update() if you wonder

            float multiplier = Mathf.Log(starterAnger + anger, multiplierLogBase) * coefficient;
            float cooldownMultiplier = Mathf.Log(starterAnger + anger, cooldownMultiplierLogBase) * cooldownCoefficient;

            currentCooldown = baseCooldown / Mathf.Max(cooldownMultiplier, 1f);
            currentPhase1Duration = basePhase1Duration / Mathf.Max(multiplier, 1f);
            currentSpawnInterval = baseSpawnInterval / multiplier;
            currentMainStopTime = baseMainStopTime / Mathf.Max(multiplier, 1f);
            currentSilhouetteSpeed = baseSilhouetteSpeed * multiplier;
            currentPauseAtEdgeTime = basePauseAtEdgeTime / multiplier;
        }
        private IEnumerator FogFlash(float speedMultiplier, bool playerApply)
        {
            fogMan.EnableFog(this);
            yield return new WaitForSeconds(0.5f);
            repMan.SetTimeScaleInstant(speedMultiplier, playerApply);
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

            while (elapsed < currentPhase1Duration)
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

                    nextSpawn = currentSpawnInterval + extraDelay;
                }

                nextSpawn -= Time.deltaTime;
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (lastWasPausing)
                yield return new WaitForSeconds(currentPauseAtEdgeTime);

            yield return new WaitForSeconds(currentSpawnInterval);
        }

        private IEnumerator Phase1_ReplaySilhouettes(List<string> spriteKeys, float speedMultiplier)
        {
            float effectiveSpawnInterval = currentSpawnInterval / speedMultiplier;

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
                    float extraDelay = isPausing ? currentPauseAtEdgeTime / speedMultiplier : 0f;

                    MoveMode mode = isPausing ? MoveMode.PauseAtEdge : MoveMode.PassThrough;
                    SpawnSilhouetteByKey(key, mode, speedMultiplier);

                    index++;
                    nextSpawn = effectiveSpawnInterval + extraDelay;
                }

                nextSpawn -= Time.deltaTime;
                yield return null;
            }

            if (lastWasPausing)
                yield return new WaitForSeconds(currentPauseAtEdgeTime);

            yield return new WaitForSeconds(currentSpawnInterval / speedMultiplier);
        }

        private IEnumerator SpawnFastSilhouette(List<string> recordTo, System.Action<float> onComplete)
        {
            string key;
            Sprite sprite = PickRandomSprite(isMain: false, out key);

            if (recordTo != null) recordTo.Add(key);

            bool isPausing = pausingSilhouettes.Contains(sprite.name);
            MoveMode mode = isPausing ? MoveMode.PauseAtEdge : MoveMode.PassThrough;

            LaunchSilhouette(sprite, "FastSilhouette", mode, speedMultiplier: 1f);
            onComplete(isPausing ? currentPauseAtEdgeTime : 0f);
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
            float speed = currentSilhouetteSpeed * speedMultiplier;
            float endX = -Screen.height * 0.5f;

            switch (mode)
            {
                case MoveMode.StopInCenter:
                    yield return MoveUntilX(rect, speed, targetX: 0f, clamp: true);
                    yield return new WaitForSeconds(currentMainStopTime);
                    break;

                case MoveMode.PauseAtEdge:
                    float rightEdgeX = canvas.GetComponent<RectTransform>().rect.width / 4f;
                    yield return MoveUntilX(rect, speed, targetX: rightEdgeX, clamp: true);
                    yield return new WaitForSeconds(currentPauseAtEdgeTime / speedMultiplier);
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

                int index = UnityEngine.Random.Range(1, count);
                lastMainSilhouetteIndex = index;
                key = $"mainSilhouette{index}";
                return BasePlugin.assetMan.Get<Sprite>(key);
            }

            int nicheCount = BasePlugin.assetMan.Get<int>("nicheSilhouetteCount");
            if (nicheCount > 0 && UnityEngine.Random.Range(0, 200) == 43)
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
                ? available[UnityEngine.Random.Range(0, available.Count)]
                : $"{prefix}{UnityEngine.Random.Range(0, count)}";

            AddToRecentQueue(chosenKey);
            return BasePlugin.assetMan.Get<Sprite>(chosenKey);
        }

        private void CancelCycle()
        {
            if (wallMan != null)
            {
                wallMan.ForceStopAll();
                wallMan = null;
            }

            if (mapMan != null)
            {
                mapMan.ForceStop();
                mapMan = null;
            }

            StopAllCoroutines();
            isRunning = false;
            activeAngerIncrease = false;

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

        public void StartAngerIncrease(float increasePerSecond = 0.01f)
        {
            if (activeAngerIncrease) return;

            Baldi baldi = UnityEngine.GameObject.FindAnyObjectByType<Baldi>();
            if (baldi == null) return;
            var angerField = AccessTools.Field(typeof(Baldi), "anger");
            savedAnger = (float)angerField.GetValue(baldi);

            activeAngerIncrease = true;
            baldi.StartCoroutine(AngerIncreaseCoroutine(baldi, angerField, increasePerSecond));
        }

        private IEnumerator AngerIncreaseCoroutine(Baldi baldi, FieldInfo angerField, float increasePerSecond)
        {
            while (activeAngerIncrease)
            {
                float currentAnger = (float)angerField.GetValue(baldi);
                currentAnger += increasePerSecond * Time.deltaTime;
                angerField.SetValue(baldi, currentAnger);

                yield return null;
            }
            angerField.SetValue(baldi, savedAnger);
        }

        public void StopAngerIncrease()
        {
            activeAngerIncrease = false;

        }

        private void AddToRecentQueue(string key)
        {
            recentSilhouettes.Enqueue(key);
            if (recentSilhouettes.Count > maxRecentSilhouettes)
                recentSilhouettes.Dequeue();
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
        private LightManager GetLightManager()
        {
            lightMan = new LightManager(Singleton<BaseGameManager>.Instance.Ec);
            return lightMan;
        }
        private WallShakeManager GetWallShakeManager()
        {
            if (wallMan == null)
                wallMan = new WallShakeManager(this);
            return wallMan;
        }
        private MapManager GetMapManager()
        {
            if (mapMan == null)
                mapMan = new MapManager(Singleton<BaseGameManager>.Instance.Ec, this);
            return mapMan;
        }
        private float GetAnger()
        {
            if (GameObject.FindObjectOfType<Baldi>() == null) return 0;
            Baldi baldi = GameObject.FindObjectOfType<Baldi>();

            var angerField = AccessTools.Field(typeof(Baldi), "anger");
            return (float)angerField.GetValue(baldi);
        }

        private AudioSourceManagerMain GetAudSourceManMain()
        {
            if (audSourceManMain == null)
                audSourceManMain = new AudioSourceManagerMain();
            return audSourceManMain;
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