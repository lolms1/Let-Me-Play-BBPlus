using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace LetMePlayBBPlus
{
    public class BulletComponent : MonoBehaviour
    {
        private float speed;
        private Vector3 direction;
        private bool hasHit = false;
        private Baldi baldi;
        private bool piercingBullet;

        private HashSet<Entity> hitEntities = new HashSet<Entity>();

        public static Dictionary<Entity, int> hitCounts = new Dictionary<Entity, int>();
        public static Dictionary<Entity, MovementModifier> appliedModifiers = new Dictionary<Entity, MovementModifier>();

        public void Initialize(Vector3 dir, float spd, Baldi owner, bool piercing)
        {
            this.direction = dir.normalized;
            this.speed = spd;
            this.piercingBullet = piercing;
            this.baldi = owner;
        }

        void Update()
        {
            if (!hasHit || piercingBullet)
            {
                transform.position += direction * speed * Time.deltaTime;
            }
        }

        public void OnChildTriggerEnter(Collider other)
        {
            Entity target = other.GetComponent<Entity>();
            if (target == null) target = other.GetComponentInParent<Entity>();
            if (target == null) return;

            if (other.transform == baldi.transform) return;

            if (other.CompareTag("Player") || other.CompareTag("NPC"))
            {
                if (hitEntities.Contains(target)) return;

                hitEntities.Add(target);

                RegisterHit(target);

                if (!piercingBullet)
                {
                    hasHit = true;
                    Destroy(gameObject, 0.05f);
                }
            }
        }

        public void RegisterHit(Entity target)
        {
            if (!hitCounts.ContainsKey(target))
            {
                hitCounts[target] = 0;
            }
            hitCounts[target]++;
            int currentHits = hitCounts[target];

            ActivityModifier actMod = target.ExternalActivity;

            if (appliedModifiers.ContainsKey(target))
            {
                actMod.moveMods.Remove(appliedModifiers[target]);
            }

            float slowFactor = currentHits switch
            {
                1 => 0.75f,
                2 => 0.4f,
                >= 3 => 0f,
                _ => 1f
            };

            MovementModifier newMod = new MovementModifier(Vector3.zero, slowFactor);
            actMod.moveMods.Add(newMod);
            appliedModifiers[target] = newMod;
            /*
            SoundObject[] hitsounds = BasePlugin.assetMan.Get<SoundObject[]>("BulletHitsSounds"); // no idea yet how to make own audMan or play this sound
            audMan.PlaySingle(hitsounds[UnityEngine.Random.Range(0, hitsounds.Length)]);
            */
        }
    }

    public class BulletCollisionProxy : MonoBehaviour
    {
        public BulletComponent owner;

        void OnTriggerEnter(Collider other)
        {
            if (owner != null)
            {
                owner.OnChildTriggerEnter(other);
            }
        }
    }
}