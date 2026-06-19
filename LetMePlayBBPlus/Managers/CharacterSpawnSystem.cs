using HarmonyLib;
using UnityEngine;

namespace LetMePlayBBPlus
{
    public static class CharacterSpawnSystem
    {
        private const float SpawnDistance = 10f;
        private const float NpcHeight = 5f;
        public static void SpawnForSilhouette(int silhouetteIndex)
        {
            Character character = GetCharacterForSilhouette(silhouetteIndex);
            if (character == Character.Null) return;

            EnvironmentController ec = GetCurrentEC();
            if (ec == null) return;

            NPC existingNpc = ec.Npcs.Find(x => x.Character == character);

            if (existingNpc != null)
                TeleportNpcToPlayer(existingNpc);
            else
                SpawnNpc(character, ec);
        }

        private static Character GetCharacterForSilhouette(int index)
        {
            switch (index)
            {
                case 2: return Character.LookAt;
                default: return Character.Null;
            }
        }

        private static void SpawnNpc(Character character, EnvironmentController ec)
        {
            switch (character)
            {
                case Character.LookAt:
                    SpawnNpcOfType<LookAtGuy>(ec);
                    break;
            }
        }

        private static void SpawnNpcOfType<T>(EnvironmentController ec) where T : NPC
        {
            T prefab = FindPrefab<T>();
            if (prefab == null) return;

            Cell safeCell = ec.RandomCell(false, false, true);
            if (safeCell == null) return;

            prefab.gameObject.SetActive(false);
            T instance = Object.Instantiate(prefab, ec.transform);
            prefab.gameObject.SetActive(true);

            instance.transform.position = safeCell.FloorWorldPosition;

            var navigator = instance.GetComponent<Navigator>();
            if (navigator != null) navigator.ec = ec;

            var entity = instance.GetComponent<Entity>();
            if (entity != null)
                AccessTools.Field(typeof(Entity), "environmentController").SetValue(entity, ec);

            AccessTools.Field(typeof(NPC), "ec")?.SetValue(instance, ec);

            instance.gameObject.SetActive(true);
            instance.Initialize();
            ec.Npcs.Add(instance);

            TeleportNpcToPlayer(instance);
        }

        private static T FindPrefab<T>() where T : NPC
        {
            foreach (var npc in Resources.FindObjectsOfTypeAll<T>())
            {
                if (npc.gameObject.scene.name == null)
                    return npc;
            }
            return null;
        }

        private static void TeleportNpcToPlayer(NPC npc)
        {
            PlayerManager player = Singleton<CoreGameManager>.Instance.GetPlayer(0);
            if (player == null) return;

            Vector3 forward = player.transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 cardinalDir = GetClosestCardinalDirection(forward);
            Vector3 playerPos = player.transform.position;

            Vector3 teleportPos = FindTeleportPosition(playerPos, cardinalDir, npc);

            npc.transform.position = teleportPos;

            Entity entity = npc.GetComponent<Entity>();
            if (entity != null)
                entity.Teleport(teleportPos);
        }
        private static Vector3 FindTeleportPosition(Vector3 playerPos, Vector3 direction, NPC npc)
        {
            Vector3 ahead = playerPos + direction * SpawnDistance;
            ahead.y = NpcHeight;
            if (!IsLineBlocked(playerPos, ahead, npc)) return ahead;

            Vector3 behind = playerPos + (-direction) * SpawnDistance;
            behind.y = NpcHeight;
            if (!IsLineBlocked(playerPos, behind, npc)) return behind;

            return new Vector3(playerPos.x, NpcHeight, playerPos.z);
        }

        private static bool IsLineBlocked(Vector3 from, Vector3 to, NPC npcToIgnore)
        {
            Vector3 dir = (to - from).normalized;
            float dist = Vector3.Distance(from, to);

            foreach (var hit in Physics.RaycastAll(from, dir, dist))
            {
                if (hit.collider.isTrigger) continue;
                if (npcToIgnore != null && hit.collider.transform.IsChildOf(npcToIgnore.transform)) continue;
                if (hit.collider.GetComponent<Entity>() != null) continue;
                return true;
            }
            return false;
        }

        public static EnvironmentController GetCurrentEC()
        {
            if (Singleton<BaseGameManager>.Instance != null)
                return Singleton<BaseGameManager>.Instance.Ec;
            return null;
        }

        private static Vector3 GetClosestCardinalDirection(Vector3 direction)
        {
            Vector3[] cardinals =
            {
                new Vector3( 0f, 0f,  1f),  // North
                new Vector3( 1f, 0f,  0f),  // East
                new Vector3( 0f, 0f, -1f),  // South
                new Vector3(-1f, 0f,  0f),  // West
            };

            float maxDot = -2f;
            int best = 0;
            for (int i = 0; i < cardinals.Length; i++)
            {
                float dot = Vector3.Dot(direction, cardinals[i]);
                if (dot > maxDot) { maxDot = dot; best = i; }
            }
            return cardinals[best];
        }
    }
}