using HarmonyLib;
using UnityEngine;
namespace LetMePlayBBPlus
{
    [HarmonyPatch(typeof(CoreGameManager), "Update")]
    class PauseMusicPatch
    {
        private static bool wasPaused;

        static void Postfix()
        {
            bool isPaused = Time.timeScale == 0f;

            if (!wasPaused && isPaused)
            {
                SilhouettesSystem.Instance.PauseMusic();
            }
            else if (wasPaused && !isPaused)
            {
                SilhouettesSystem.Instance.ResumeMusic();
            }

            wasPaused = isPaused;
        }
    }
}