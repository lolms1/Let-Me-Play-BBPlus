using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LetMePlayBBPlus
{
    /// <summary>
    /// Custom Baldi state where he stops moving, aims at the player,
    /// fires 3 lasers and 3 bullets in sequence, then returns to chasing.
    /// 
    /// Architecture: Inherits from Baldi_SubState (same as Baldi_Praise).
    /// While this state is active, the original Baldi_Chase.Update() logic
    /// (slapping, pathfinding) is completely paused.
    /// </summary>
    public class Baldi_ShootState : Baldi_SubState
    {
        // The player Baldi is aiming at (grabbed from ec.Players[0] on Enter)
        private PlayerManager target;

        // Phase 1: Laser counters
        private int lasersFired = 0;
        private bool phase1Complete = false;

        // Phase 2: Bullet counters
        private int bulletsFired = 0;
        // Phase 3: Placing bananas
        private bool phase2Complete = false;

        private bool phase3Complete = false;

        // Stores the direction of each laser so its corresponding bullet follows the same path

        private Vector3[] bulletDirections;

        // Visual state: we disable the Animator for the entire state and manually control the sprite
        private SpriteRenderer aimRenderer;
        private Sprite aimSprite;
        private Sprite shootSprite;

        private float anger;
        private float laserTimer;
        private float bulletTimer;
        private float bulletInterval;
        private float cleanupTimer;
        private float bulletSpeed;
        private int bulletAmount;
        private bool piercingBullet;

        Animator animator;
        AudioManager audMan;

        private struct Timers
        {
            public float laserTimer;
            public float bulletTimer;
            public float bulletInterval;
            public float cleanupTimer;
        }

        private Timers timers;

        private List<GameObject> activeBullets = new List<GameObject>();

        private List<GameObject> activeLasers = new List<GameObject>();

        public Baldi_ShootState(NPC npc, Baldi baldi, NpcState previousState)
            : base(npc, baldi, previousState)
        {
        }

        public override void Enter()
        {
            base.Enter();

            anger = GetAnger();
            audMan = GetaudMan();
            CalculateAndGetValues();
            PlayAimSound();
            DisableAnimator();

            bulletDirections = new Vector3[bulletAmount];

            // Grab the primary player as the target
            target = baldi.ec.Players[0];
        }

        public override void Update()
        {
            base.Update();

            // DeltaTime scaled by the NPC's speed (so fast-forward/slowdown effects apply)
            float deltaTime = Time.deltaTime * npc.TimeScale;

            if (npc.behaviorStateMachine.CurrentState != this)
            {
                return;
            }

            if (!phase1Complete)
            {
                // PHASE 1: Fire 3 lasers with 0.2s interval
                laserTimer -= deltaTime;

                if (laserTimer <= 0f && lasersFired < bulletAmount)
                {
                    FireLaser();
                    lasersFired++;
                    laserTimer = timers.laserTimer; // Reset interval timer
                }

                if (lasersFired >= bulletAmount)
                {
                    phase1Complete = true;
                    bulletTimer = timers.bulletTimer; // Small pause between last laser and first bullet
                }
            }
            else if (!phase2Complete)
            {
                bulletTimer -= deltaTime;

                if (bulletTimer <= 0f && bulletsFired < bulletAmount)
                {
                    var bullet = FireBullet();
                    activeBullets.Add(bullet);
                    bulletsFired++;
                    bulletTimer = timers.bulletTimer;
                }

                if (bulletsFired >= bulletAmount)
                {
                    phase2Complete = true;
                    cleanupTimer = timers.cleanupTimer;
                }
            }
            else
            {
                cleanupTimer -= deltaTime;

                activeBullets.RemoveAll(x => x == null);

                if ((cleanupTimer <= 0f || activeBullets.Count == 0) && (!phase3Complete))
                {
                    phase3Complete = true;
                    ProcessAllHits();
                    baldi.StartCoroutine(WaitSomeTimeBeforeExitState());
                }
            }
        }
        private IEnumerator WaitSomeTimeBeforeExitState()
        {
            yield return new WaitForSeconds(2f);
            npc.behaviorStateMachine.ChangeState(previousState);
            this.Exit();
        }

        private void ProcessAllHits()
        {
            ITM_NanaPeel bananaPrefab = BasePlugin.assetMan.Get<ITM_NanaPeel>("ITM_NanaPeel");

            Vector3 shootDirection = (target.transform.position - baldi.transform.position).normalized;

            foreach (var kvp in BulletComponent.hitCounts)
            {
                Entity targetEntity = kvp.Key;
                int hits = kvp.Value;

                if (targetEntity == null) continue;

                var freezeMod = new MovementModifier(Vector3.zero, 0f);
                targetEntity.ExternalActivity.moveMods.Add(freezeMod);

                RemoveModFromBullet(targetEntity);
                float slideSpeed = GetSlideSpeed(hits, anger);

                float randomAngle = UnityEngine.Random.Range(-45f, 45);
                Vector3 pushDirection = Quaternion.Euler(0f, randomAngle, 0f) * shootDirection;

                if (bananaPrefab != null)
                {
                    ITM_NanaPeel banana = GameObject.Instantiate(bananaPrefab);

                    var speedField = AccessTools.Field(typeof(ITM_NanaPeel), "speed");
                    speedField.SetValue(banana, slideSpeed);

                    var startHeightField = AccessTools.Field(typeof(ITM_NanaPeel), "startHeight");
                    startHeightField.SetValue(banana, 0f);

                    Vector3 spawnPos = targetEntity.transform.position;
                    banana.Spawn(baldi.ec, spawnPos, pushDirection, 0f);
                }

                Force pushForce = new Force(pushDirection, 1f, 1f);
                targetEntity.AddForce(pushForce);

                baldi.StartCoroutine(RemoveFreezeAfterDelay(targetEntity, freezeMod, 0.3f, pushForce));
            }
        }

        public override void Exit()
        {
            base.Exit();

            EnableAnimator();
            CleanArea();
            RemoveAppliedMods();
            ResetCycle();
        }

        /// <summary>
        /// Fires a single laser beam with random spread.
        /// Saves the direction so the corresponding bullet uses the same path.
        /// Plays a one-frame "shoot" animation overlay.
        /// </summary>
        private void FireLaser()
        {
            // Start position slightly above Baldi (eye level)
            Vector3 baldiPos = baldi.transform.position;
            Vector3 playerPos = target.transform.position;

            // Base direction toward player
            Vector3 baseDirection = (playerPos - baldiPos).normalized;

            // Add random spread: ±0.5 degrees on both axes
            float randomAngleX = UnityEngine.Random.Range(-0.5f, 0.5f);
            float randomAngleY = UnityEngine.Random.Range(-0.5f, 0.5f);
            Vector3 laserDirection = Quaternion.Euler(randomAngleX, randomAngleY, 0f) * baseDirection;

            // Save this laser's direction for its bullet
            bulletDirections[lasersFired] = laserDirection;

            // Create the visual laser beam (pure visual, no collision)
            var laser = CreateGameobject.CreateLaserBeam(baldiPos, laserDirection, 1000f);
            activeLasers.Add(laser);
        }

        /// <summary>
        /// Fires a single bullet along the direction of the corresponding laser.
        /// </summary>
        private GameObject FireBullet()
        {
            Vector3 baldiPos = baldi.transform.position;
            var bullet = CreateGameobject.CreateBullet(baldi.ec, baldiPos, bulletDirections[bulletsFired], bulletSpeed, baldi, piercingBullet, cleanupTimer);
            baldi.StartCoroutine(PlayShootFrame());
            return bullet;
        }

        /// <summary>
        /// Coroutine that swaps to the shoot sprite for 0.1 seconds, then restores the aim sprite.
        /// This gives a visual "kick" effect when Baldi fires a bullet.
        /// </summary>
        private IEnumerator PlayShootFrame()
        {
            if (aimRenderer == null || shootSprite == null) yield break;

            aimRenderer.sprite = shootSprite;
            PlayShootSound();
            yield return new WaitForSeconds(0.1f);
            aimRenderer.sprite = aimSprite;
        }

        private IEnumerator RemoveFreezeAfterDelay(Entity targetEntity, MovementModifier freezeMod, float delay, Force pushForce)
        {
            yield return new WaitForSeconds(delay);
            if (targetEntity != null && targetEntity.ExternalActivity != null)
            {
                targetEntity.ExternalActivity.moveMods.Remove(freezeMod);
                targetEntity.RemoveForce(pushForce);
            }
        }
        private void CalculateAndGetValues()
        {
            laserTimer = 0.03f;
            bulletTimer = 0.03f;
            bulletInterval = 0.5f;
            cleanupTimer = 0f;
            // coefficient = BaldiShootCfg.Coefficient;
            bulletAmount = 3;
            piercingBullet = false;
            bulletSpeed = 300;

            float multiplier = 1;

            timers.laserTimer = laserTimer / multiplier;
            timers.bulletTimer = bulletTimer / multiplier;
            timers.bulletInterval = bulletInterval / multiplier;
            timers.cleanupTimer = Math.Max(cleanupTimer / multiplier, 1f);

        }
        private float GetSlideSpeed(int hits, float anger)
        {
            float baseSpeed = hits switch
            {
                1 => 15f,
                2 => 25f,
                3 => 35f,
                > 3 => 60f,
                _ => 99f
            };

            float multiplier = 1f + Mathf.Log(1f + anger, 2f) * 0.8f;
            return baseSpeed * multiplier;
        }
        private AudioManager GetaudMan()
        {
            var audmanField = AccessTools.Field(typeof(Baldi), "audMan");
            return (AudioManager)audmanField.GetValue(baldi);
        }
        private void PlayAimSound()
        {
            audMan.PlaySingle(BasePlugin.assetMan.Get<SoundObject>("BaldiAimingSound"));
        }
        private void PlayShootSound()
        {
            audMan.PlaySingle(BasePlugin.assetMan.Get<SoundObject>("BaldiShootSound"));
        }
        private void DisableAnimator()
        {
            // Load custom sprites from the AssetManager
            aimSprite = BasePlugin.assetMan.Get<Sprite>("BaldiAim");
            shootSprite = BasePlugin.assetMan.Get<Sprite>("BaldiShoot");

            // Find the VISIBLE SpriteRenderer (accounting for SpriteOverlay if active)
            var spriteOverlay = baldi.GetComponentInChildren<SpriteOverlay>();
            if (spriteOverlay != null)
            {
                // SpriteOverlay hides the original renderer and creates a child "FakeRenderer"
                var fakeRendererTransform = spriteOverlay.transform.Find("FakeRenderer");
                aimRenderer = fakeRendererTransform.GetComponent<SpriteRenderer>();
            }
            else
            {
                // No SpriteOverlay — use the original renderer directly
                aimRenderer = baldi.GetComponentInChildren<SpriteRenderer>();
            }

            // Disable the Animator so it doesn't overwrite our custom sprite,
            // then set the aiming sprite for the entire state duration
            if (aimRenderer != null)
            {
                // Access the private "animator" field on Baldi via Harmony's AccessTools
                Animator animator = GetAnimator();

                animator.enabled = false;
                aimRenderer.sprite = aimSprite;
            }
        }
        private void EnableAnimator()
        {
            // Re-enable the Animator so Baldi resumes normal animations
            animator = GetAnimator();
            animator.enabled = true;
        }

        private Animator GetAnimator()
        {
            // Access the private "animator" field on Baldi via Harmony's AccessTools
            var animatorField = AccessTools.Field(typeof(Baldi), "animator");
            Animator animator = (Animator)animatorField.GetValue(baldi);
            return animator;
        }
        private float GetAnger()
        {
            var angerField = AccessTools.Field(typeof(Baldi), "anger");
            float anger = (float)angerField.GetValue(baldi);
            return anger;
        }
        private void CleanArea()
        {
            foreach (var bullet in activeBullets)
            {
                if (bullet != null)
                {
                    GameObject.Destroy(bullet);
                }
            }

            foreach (var laser in activeLasers)
            {
                if (laser != null)
                {
                    GameObject.Destroy(laser);
                }
            }
            activeBullets.Clear();
            activeLasers.Clear();
        }
        private void RemoveAppliedMods()
        {
            foreach (var kvp in BulletComponent.appliedModifiers)
            {
                if (kvp.Key != null && kvp.Key.ExternalActivity != null)
                {
                    kvp.Key.ExternalActivity.moveMods.Remove(kvp.Value);
                }
            }

            BulletComponent.hitCounts.Clear();
            BulletComponent.appliedModifiers.Clear();
        }
        private void ResetCycle()
        {
            lasersFired = 0;
            phase1Complete = false;
            bulletsFired = 0;
            phase2Complete = false;
            phase3Complete = false;
        }
        private void RemoveModFromBullet(Entity targetEntity)
        {
            if (BulletComponent.appliedModifiers.ContainsKey(targetEntity))
            {
                targetEntity.ExternalActivity.moveMods.Remove(BulletComponent.appliedModifiers[targetEntity]);
            }
        }
    }
}
