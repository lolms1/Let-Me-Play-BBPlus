using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LetMePlayBBPlus
{
    public class ReplayManager
    {
        private EnvironmentController ec;
        private TimeScaleModifier timeScaleModifier;
        private MonoBehaviour coroutineOwner;
        private Coroutine smoothCoroutine;

        private float currentTimeScale = 1f;
        private float targetTimeScale = 1f;
        private float timeScaleChangeSpeed = 0.2f;
        private bool isScaling;

        private Dictionary<Entity, Vector3> savedPositions = new Dictionary<Entity, Vector3>();
        private Dictionary<Entity, MovementModifier> playerSlowMods = new Dictionary<Entity, MovementModifier>();
        private Dictionary<PlayerManager, Quaternion> savedRotations = new Dictionary<PlayerManager, Quaternion>();

        public bool IsScaling => isScaling;

        public ReplayManager(EnvironmentController ec, MonoBehaviour owner)
        {
            this.ec = ec;
            this.coroutineOwner = owner;
            this.timeScaleModifier = new TimeScaleModifier(1f, 1f, 1f);
        }

        public void SetTimeScaleInstant(float timeScale, bool playerApply)
        {
            currentTimeScale = timeScale;
            targetTimeScale = timeScale;
            ApplyTimeScale(timeScale);
            if (playerApply)
                UpdatePlayerSlow();
        }

        public void SetTimeScaleSmooth(float target, float changeSpeed = -1f)
        {
            if (changeSpeed > 0f)
                timeScaleChangeSpeed = changeSpeed;

            targetTimeScale = target;

            if (smoothCoroutine != null)
                coroutineOwner.StopCoroutine(smoothCoroutine);

            smoothCoroutine = coroutineOwner.StartCoroutine(SmoothTimeScaleCoroutine());
        }

        private IEnumerator SmoothTimeScaleCoroutine()
        {
            isScaling = true;

            while (Mathf.Abs(currentTimeScale - targetTimeScale) > 0.01f)
            {
                float step = timeScaleChangeSpeed * Time.deltaTime;
                currentTimeScale = Mathf.MoveTowards(currentTimeScale, targetTimeScale, step);
                ApplyTimeScale(currentTimeScale);
                UpdatePlayerSlow();
                yield return null;
            }

            currentTimeScale = targetTimeScale;
            ApplyTimeScale(currentTimeScale);
            UpdatePlayerSlow();
            isScaling = false;
            smoothCoroutine = null;
        }

        private void ApplyTimeScale(float scale)
        {
            timeScaleModifier.environmentTimeScale = scale;
            timeScaleModifier.npcTimeScale = scale;
        }

        public void EnableTimeScale()
        {
            if (ec == null) return;
            ec.AddTimeScale(timeScaleModifier);
        }

        public void DisableTimeScale()
        {
            if (ec == null) return;
            ec.RemoveTimeScale(timeScaleModifier);
        }
        private void UpdatePlayerSlow()
        {
            if (Singleton<CoreGameManager>.Instance == null) return;

            float slowFactor = currentTimeScale;

            for (int i = 0; i < Singleton<CoreGameManager>.Instance.setPlayers; i++)
            {
                PlayerManager player = Singleton<CoreGameManager>.Instance.GetPlayer(i);
                if (player == null) continue;

                Entity entity = player.plm?.Entity;
                if (entity == null) continue;

                if (playerSlowMods.ContainsKey(entity))
                {
                    if (entity.ExternalActivity != null)
                        entity.ExternalActivity.moveMods.Remove(playerSlowMods[entity]);
                }

                MovementModifier mod = new MovementModifier(Vector3.zero, slowFactor);
                if (entity.ExternalActivity != null)
                {
                    entity.ExternalActivity.moveMods.Add(mod);
                    playerSlowMods[entity] = mod;
                }
            }
        }

        public void ClearPlayerSlow()
        {
            foreach (var kvp in playerSlowMods)
            {
                if (kvp.Key == null) continue;
                if (kvp.Key.ExternalActivity == null) continue;
                kvp.Key.ExternalActivity.moveMods.Remove(kvp.Value);
            }
            playerSlowMods.Clear();
        }
        public void SaveAllPositions()
        {
            savedPositions.Clear();
            savedRotations.Clear();
            SavePlayerRotations();
            if (ec == null) return;

            foreach (NPC npc in ec.Npcs)
            {
                if (npc == null) continue;
                Entity entity = npc.GetComponent<Entity>();
                if (entity != null)
                    savedPositions[entity] = entity.transform.position;
            }

            if (Singleton<CoreGameManager>.Instance == null) return;
            for (int i = 0; i < Singleton<CoreGameManager>.Instance.setPlayers; i++)
            {
                PlayerManager player = Singleton<CoreGameManager>.Instance.GetPlayer(i);
                if (player == null) continue;
                Entity entity = player.plm?.Entity;
                if (entity != null)
                    savedPositions[entity] = entity.transform.position;
            }
        }

        public void RestoreAllPositions()
        {
            foreach (var kvp in savedPositions)
            {
                if (kvp.Key == null) continue;
                kvp.Key.Teleport(kvp.Value);
            }
        }

        public void SavePlayerRotations()
        {
            savedRotations.Clear();
            if (Singleton<CoreGameManager>.Instance == null) return;

            for (int i = 0; i < Singleton<CoreGameManager>.Instance.setPlayers; i++)
            {
                PlayerManager player = Singleton<CoreGameManager>.Instance.GetPlayer(i);
                if (player == null) continue;
                savedRotations[player] = player.transform.rotation;
            }
        }

        public void RestorePlayerRotations()
        {
            foreach (var kvp in savedRotations)
            {
                if (kvp.Key == null) continue;
                kvp.Key.transform.rotation = kvp.Value;
            }
        }

        public void Reset()
        {
            if (smoothCoroutine != null)
            {
                coroutineOwner.StopCoroutine(smoothCoroutine);
                smoothCoroutine = null;
            }
            isScaling = false;

            ClearPlayerSlow();

            ApplyTimeScale(1f);
            if (ec != null)
            {
                ec.RemoveTimeScale(timeScaleModifier);
                ec = null;
            }

            currentTimeScale = 1f;
            targetTimeScale = 1f;

            savedPositions.Clear();
            savedRotations.Clear();
        }
    }
}