using UnityEngine;
using System.Collections;

namespace LetMePlayBBPlus
{
    public class FogManager
    {
        private EnvironmentController ec;
        private Fog currentFog;
        private Coroutine fadeCoroutine;

        public float FogDensity { get; set; } = 0.03f;
        public float FogSpeed { get; set; } = 1f;
        public Color FogColor { get; set; } = Color.gray;
        public float FogStartDist { get; set; } = 5f;
        public float FogMaxDist { get; set; } = 15f;

        public FogManager(EnvironmentController ec)
        {
            this.ec = ec;
        }
        public void EnableFog(MonoBehaviour coroutineOwner)
        {
            if (currentFog != null) return;

            currentFog = new Fog
            {
                color = FogColor,
                startDist = FogStartDist,
                maxDist = FogMaxDist,
                strength = 0f
            };

            ec.AddFog(currentFog);
            fadeCoroutine = coroutineOwner.StartCoroutine(FadeIn());
        }
        public void DisableFog(MonoBehaviour coroutineOwner)
        {
            if (currentFog == null) return;

            if (fadeCoroutine != null)
            {
                coroutineOwner.StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = coroutineOwner.StartCoroutine(FadeOut());
        }
        public void RemoveFogInstantly()
        {
            if (currentFog == null) return;

            ec.RemoveFog(currentFog);
            currentFog = null;
        }

        public void UpdateFogParameters(Color? color = null, float? startDist = null, float? maxDist = null)
        {
            if (currentFog == null) return;

            if (color.HasValue) currentFog.color = color.Value;
            if (startDist.HasValue) currentFog.startDist = startDist.Value;
            if (maxDist.HasValue) currentFog.maxDist = maxDist.Value;

            ec.UpdateFog();
        }

        private IEnumerator FadeIn()
        {
            float strength = 0f;
            while (strength < 1f)
            {
                strength += FogSpeed * Time.deltaTime;
                currentFog.strength = Mathf.Min(strength, 1f);
                ec.UpdateFog();
                yield return null;
            }
            currentFog.strength = 1f;
            ec.UpdateFog();
        }

        private IEnumerator FadeOut()
        {
            float strength = currentFog.strength;
            while (strength > 0f)
            {
                strength -= FogSpeed * Time.deltaTime;
                currentFog.strength = Mathf.Max(strength, 0f);
                ec.UpdateFog();
                yield return null;
            }
            currentFog.strength = 0f;
            ec.UpdateFog();

            ec.RemoveFog(currentFog);
            currentFog = null;
        }
    }
}