using HarmonyLib;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LetMePlayBBPlus
{
    public class SilhouettesSystem : MonoBehaviour
    {
        [Header("Timing")]
        private float cooldown = 1f;
        private float phase1Duration = 10f;
        private float spawnInterval = 0.8f;
        private float mainStopTime = 2f;

        [Header("Movement")]
        private float silhouetteSpeed = 300f;

        private float timer;
        private bool isRunning;
        private bool isInitialized;
        private int lastMainSilhouetteIndex = -1;

        private Canvas canvas;
        private FogManager fogMan;

        private bool isInGame;
        private int currentLevel = -1;

        void Start()
        {
            DontDestroyOnLoad(gameObject);

            canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = FindObjectOfType<Canvas>();

            timer = cooldown;
            isInitialized = true;
        }

        void Update()
        {
            if (!isInitialized) return;

            if (!isInGame && IsInGame())
            {
                isInGame = true;
                timer = cooldown;
                Debug.Log("[SilhouettesSystem] Game detected via DigitalClock! System activated.");
            }

            if (!isInGame || isRunning) return;

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                timer = cooldown;
                StartCoroutine(RunAnimationCycle());
            }
        }

        private bool IsInGame()
        {
            int level = GetCurrentLevel();

            if (level != currentLevel)
            {
                currentLevel = level;
                isInGame = false;
            }

            if (isInGame) return true;

            DigitalClock clock = GameObject.FindObjectOfType<DigitalClock>(); // yeahhhhhhhhhhhhhhhh...
            return clock != null;
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
            {
                fogMan = new FogManager(Singleton<BaseGameManager>.Instance.Ec);
            }
            return fogMan;
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

            lastMainSilhouetteIndex = -1;

            isRunning = false;
        }

        private IEnumerator Phase1_FastSilhouettes()
        {
            float elapsed = 0f;
            float nextSpawn = 0f;

            while (elapsed < phase1Duration)
            {
                if (nextSpawn <= 0f)
                {
                    CreateAndMoveSilhouette(isMain: false, stopInCenter: false);
                    nextSpawn = spawnInterval;
                }

                nextSpawn -= Time.deltaTime;
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator Phase2_MainSilhouette()
        {
            yield return CreateAndMoveSilhouette(isMain: true, stopInCenter: true);
        }

        private Coroutine CreateAndMoveSilhouette(bool isMain, bool stopInCenter)
        {
            GameObject obj = new GameObject(isMain ? "MainSilhouette" : "FastSilhouette");
            obj.transform.SetParent(canvas.transform, false);
            obj.transform.SetAsFirstSibling();

            RawImage rawImage = obj.AddComponent<RawImage>();
            RectTransform rect = obj.GetComponent<RectTransform>();

            Sprite sprite = GetRandomSprite(isMain);
            if (sprite == null)
            {
                Destroy(obj);
                return null;
            }
            rawImage.texture = sprite.texture;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            float targetHeight = canvasRect.rect.height;
            float targetWidth = canvasRect.rect.width * 0.3f;

            rect.sizeDelta = new Vector2(targetWidth, targetHeight);
            rect.anchoredPosition = new Vector2(Screen.width + targetWidth / 2f, 0f);

            return StartCoroutine(MoveAndDestroy(rawImage, rect, stopInCenter));
        }

        private IEnumerator MoveAndDestroy(RawImage rawImage, RectTransform rect, bool stopInCenter)
        {
            float speed = silhouetteSpeed;

            if (stopInCenter)
            {
                while (rect.anchoredPosition.x > 0f)
                {
                    Vector2 pos = rect.anchoredPosition;
                    pos.x -= speed * Time.deltaTime;
                    if (pos.x < 0f) pos.x = 0f;
                    rect.anchoredPosition = pos;
                    yield return null;
                }
                yield return new WaitForSeconds(mainStopTime);
            }
            else
            {
                float endX = -Screen.height * 0.5f;
                while (rect.anchoredPosition.x > endX)
                {
                    Vector2 pos = rect.anchoredPosition;
                    pos.x -= speed * Time.deltaTime;
                    rect.anchoredPosition = pos;
                    yield return null;
                }
            }

            Destroy(rawImage.gameObject);
        }

        private Sprite GetRandomSprite(bool isMain)
        {
            if (isMain)
            {
                int count = BasePlugin.assetMan.Get<int>("mainSilhouetteCount");
                if (count <= 0) return null;

                int index = 2;
                lastMainSilhouetteIndex = index;
                return BasePlugin.assetMan.Get<Sprite>($"mainSilhouette{index}");
            }

            int nicheCount = BasePlugin.assetMan.Get<int>("nicheSilhouetteCount");
            if (nicheCount > 0 && Random.Range(0, 200) == 43)
            {
                int nicheIndex = Random.Range(0, nicheCount);
                return BasePlugin.assetMan.Get<Sprite>($"nicheSilhouette{nicheIndex}");
            }

            int regularCount = BasePlugin.assetMan.Get<int>("silhouetteCount");
            if (regularCount <= 0) return null;
            return BasePlugin.assetMan.Get<Sprite>($"silhouette{Random.Range(0, regularCount)}");
        }
    }
}