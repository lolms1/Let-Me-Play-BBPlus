using HarmonyLib;
using System.Collections;
using TMPro;
using UnityEngine;

namespace LetMePlayBBPlus
{
    public class MapManager
    {
        private Map map;
        private EnvironmentController ec;
        private MonoBehaviour coroutineOwner;

        private IntVector2 mapSize;

        private bool isCounting;
        private int requiredCells = 20;
        private int cellsFoundSinceReset;

        private bool[,] hiddenTiles;

        private TMP_Text counterText;
        private GameObject textObject;

        public MapManager(EnvironmentController ec, MonoBehaviour owner)
        {
            this.ec = ec;
            this.coroutineOwner = owner;
            this.map = ec.map;
            this.mapSize = map.size;
        }

        public void SaveAndHideMap(int requiredAmount = 20)
        {
            hiddenTiles = new bool[mapSize.x, mapSize.z];

            for (int x = 0; x < mapSize.x; x++)
            {
                for (int z = 0; z < mapSize.z; z++)
                {
                    if (map.foundTiles[x, z])
                    {
                        hiddenTiles[x, z] = true;

                        map.tiles[x, z].gameObject.SetActive(false);
                        var foundField = AccessTools.Field(typeof(MapTile), "found");
                        foundField.SetValue(map.tiles[x, z], false);

                        map.foundTiles[x, z] = false;
                    }
                }
            }

            StartCounting(requiredAmount);
        }

        private void StartCounting(int requiredAmount)
        {
            requiredCells = requiredAmount;
            cellsFoundSinceReset = 0;
            isCounting = true;

            CreateCounterUI();
            coroutineOwner.StartCoroutine(CountNewCellsCoroutine());
        }

        private IEnumerator CountNewCellsCoroutine()
        {
            while (isCounting)
            {
                int currentFound = 0;
                for (int x = 0; x < mapSize.x; x++)
                    for (int z = 0; z < mapSize.z; z++)
                        if (map.foundTiles[x, z])
                            currentFound++;

                if (currentFound > cellsFoundSinceReset)
                {
                    cellsFoundSinceReset = currentFound;
                    UpdateCounterText();

                    if (cellsFoundSinceReset >= requiredCells)
                    {
                        RestoreMap();
                        yield break;
                    }
                }

                yield return new WaitForSeconds(0.5f);
            }
        }
        public void RestoreMap()
        {
            isCounting = false;
            DestroyCounterUI();

            MapUpdateBlockerPatch.BlockUpdate = false;

            for (int x = 0; x < mapSize.x; x++)
            {
                for (int z = 0; z < mapSize.z; z++)
                {
                    if (hiddenTiles[x, z])
                    {
                        map.foundTiles[x, z] = true;
                        map.tiles[x, z].gameObject.SetActive(true);
                    }
                }
            }

            if (cellsFoundSinceReset < requiredCells)
            {
                SpawnBaldiOnPlayer();
            }
        }

        public void ForceStop()
        {
            if (!isCounting) return;

            isCounting = false;
            DestroyCounterUI();
            RestoreMap();
        }

        private void SpawnBaldiOnPlayer()
        {
            Baldi baldi = GameObject.FindObjectOfType<Baldi>();
            if (baldi == null) return;

            PlayerManager player = Singleton<CoreGameManager>.Instance.GetPlayer(0);
            if (player == null) return;

            baldi.transform.position = player.transform.position + player.transform.forward * 5f;
        }

        private void CreateCounterUI()
        {
            var hudManager = GameObject.FindObjectOfType<HudManager>();
            if (hudManager == null) return;

            textObject = new GameObject("MapCounterText");
            textObject.transform.SetParent(hudManager.transform, false);

            counterText = textObject.AddComponent<TextMeshProUGUI>();
            counterText.fontSize = 36;
            counterText.color = Color.white;
            counterText.alignment = TextAlignmentOptions.TopLeft;

            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(20, -20);
            rect.sizeDelta = new Vector2(400, 50);

            UpdateCounterText();
        }

        private void UpdateCounterText()
        {
            if (counterText == null) return;
            int remaining = Mathf.Max(0, requiredCells - cellsFoundSinceReset);
            counterText.text = $"Explore cells: {remaining}";
        }

        private void DestroyCounterUI()
        {
            if (textObject != null)
                GameObject.Destroy(textObject);
        }
    }
}