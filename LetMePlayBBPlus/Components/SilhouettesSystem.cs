using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LetMePlayBBPlus
{
    public class SilhouettesSystem : MonoBehaviour
    {
        private float cooldown = 3f;
        private float phase1Duration = 8f;
        private float spawnInterval = 0.8f;
        private float silhouetteSpeed = 300f;
        private float mainStopTime = 2f;

        private float timer;
        private bool isRunning;
        private bool isInitialized;

        private Canvas canvas;

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
            if (!isInitialized || isRunning) return;

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                timer = cooldown;
                StartCoroutine(RunAnimationCycle());
            }
        }

        private IEnumerator RunAnimationCycle()
        {
            isRunning = true;

            yield return StartCoroutine(Phase1_FastSilhouettes());

            yield return StartCoroutine(Phase2_MainSilhouette());

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


            Sprite randomSprite = GetRandomSprite(isMain);
            rawImage.texture = randomSprite.texture; 

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            float canvasHeight = canvasRect.rect.height;
            float canvasWidth = canvasRect.rect.width;

            float targetHeight = canvasHeight;       
            float targetWidth = canvasWidth * 0.3f;  

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
                int index = 1;
                return BasePlugin.assetMan.Get<Sprite>($"mainSilhouette{index}");
            }
            else
            {
                int nicheCount = BasePlugin.assetMan.Get<int>("nicheSilhouetteCount");
                if (nicheCount > 0 && Random.Range(0, 200) == 43) // 434343434343 https://cdn.discordapp.com/attachments/1307804467625852952/1515408084988858468/2a5d5182-6d93-443a-9d42-f7138e2cf1ab.png?ex=6a2ee542&is=6a2d93c2&hm=e9795c7d4c4bf297bf8ca2759499c2180ccb3e2b7566fbd05f57a405930b11d7&
                {
                    int nicheIndex = Random.Range(0, nicheCount);
                    return BasePlugin.assetMan.Get<Sprite>($"nicheSilhouette{nicheIndex}");
                }

                int count = BasePlugin.assetMan.Get<int>("silhouetteCount");
                if (count <= 0) return null;
                int index = Random.Range(0, count);
                return BasePlugin.assetMan.Get<Sprite>($"silhouette{index}");
            }
        }
    }
}