using System.Collections;
using UnityEngine;

public class WallShakeManager
{
    private MonoBehaviour coroutineOwner;
    private Coroutine shakeCoroutine;
    private Coroutine flashCoroutine;

    public bool IsShaking { get; private set; }
    public bool IsFlashing { get; private set; }

    public WallShakeManager(MonoBehaviour owner)
    {
        this.coroutineOwner = owner;
    }
    public void StartShake(float intensity = 0.05f, float interval = 0.5f, float decaySpeed = 4f, float duration = 0f)
    {
        if (IsShaking) ForceStopShake();

        shakeCoroutine = coroutineOwner.StartCoroutine(BeatShakeCoroutine(intensity, interval, decaySpeed, duration));
        IsShaking = true;
    }

    public void StopShake(float fadeDuration = 0.3f)
    {
        if (!IsShaking) return;

        if (shakeCoroutine != null)
        {
            coroutineOwner.StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }

        shakeCoroutine = coroutineOwner.StartCoroutine(FadeOutShake(fadeDuration));
        IsShaking = false;
    }

    public void ForceStopShake()
    {
        if (shakeCoroutine != null)
        {
            coroutineOwner.StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }
        Shader.SetGlobalFloat("_TileVertexGlitchIntensity", 0f);
        IsShaking = false;
    }

    private IEnumerator BeatShakeCoroutine(float intensity, float interval, float decaySpeed, float duration)
    {
        float elapsed = 0f;
        float nextBeat = 0f;
        float currentGlitch = 0f;

        while (duration <= 0f || elapsed < duration)
        {
            if (nextBeat <= 0f)
            {
                currentGlitch = intensity;
                Shader.SetGlobalFloat("_TileVertexGlitchSeed", UnityEngine.Random.Range(0f, 1000f));
                nextBeat = interval;
            }
            currentGlitch = Mathf.Max(0f, currentGlitch - decaySpeed * Time.deltaTime);
            Shader.SetGlobalFloat("_TileVertexGlitchIntensity", currentGlitch);

            nextBeat -= Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }

        shakeCoroutine = null;
        IsShaking = false;
        yield return FadeOutShake(0.3f);
    }

    private IEnumerator FadeOutShake(float fadeDuration)
    {
        if (fadeDuration <= 0f)
        {
            Shader.SetGlobalFloat("_TileVertexGlitchIntensity", 0f);
            yield break;
        }

        float elapsed = 0f;
        float startIntensity = Shader.GetGlobalFloat("_TileVertexGlitchIntensity");

        while (elapsed < fadeDuration)
        {
            Shader.SetGlobalFloat("_TileVertexGlitchIntensity",
                Mathf.Lerp(startIntensity, 0f, elapsed / fadeDuration));
            Shader.SetGlobalFloat("_TileVertexGlitchSeed", UnityEngine.Random.Range(0f, 1000f));
            elapsed += Time.deltaTime;
            yield return null;
        }

        Shader.SetGlobalFloat("_TileVertexGlitchIntensity", 0f);
    }
    public void StartFlash(float duration, float intensity = 0.9f)
    {
        if (IsFlashing) ForceStopFlash();

        flashCoroutine = coroutineOwner.StartCoroutine(FlashCoroutine(duration, intensity));
        IsFlashing = true;
    }

    public void StopFlash()
    {
        if (!IsFlashing) return;

        if (flashCoroutine != null)
        {
            coroutineOwner.StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        Shader.SetGlobalInt("_SpriteColorGlitching", 0);
        Shader.SetGlobalInt("_ColorGlitching", 0);
        IsFlashing = false;
    }

    public void ForceStopFlash()
    {
        if (flashCoroutine != null)
        {
            coroutineOwner.StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }
        Shader.SetGlobalInt("_SpriteColorGlitching", 0);
        Shader.SetGlobalInt("_ColorGlitching", 0);
        IsFlashing = false;
    }

    private IEnumerator FlashCoroutine(float duration, float intensity)
    {
        Shader.SetGlobalInt("_SpriteColorGlitching", 1);
        Shader.SetGlobalFloat("_SpriteColorGlitchPercent", intensity);
        Shader.SetGlobalInt("_ColorGlitching", 1);
        Shader.SetGlobalFloat("_ColorGlitchPercent", intensity);
        Shader.SetGlobalFloat("_SpriteColorGlitchVal", UnityEngine.Random.Range(0f, 4096f));

        while (duration > 0f)
        {
            duration -= Time.unscaledDeltaTime;
            float val = UnityEngine.Random.Range(0f, 4096f);
            Shader.SetGlobalFloat("_SpriteColorGlitchVal", val);
            Shader.SetGlobalFloat("_ColorGlitchVal", val);
            yield return null;
        }

        Shader.SetGlobalInt("_SpriteColorGlitching", 0);
        Shader.SetGlobalInt("_ColorGlitching", 0);
        IsFlashing = false;
    }
    public void ForceStopAll()
    {
        ForceStopShake();
        ForceStopFlash();
    }
}