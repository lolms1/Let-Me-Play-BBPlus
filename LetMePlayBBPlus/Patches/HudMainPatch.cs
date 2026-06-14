using HarmonyLib;
using LetMePlayBBPlus;
using UnityEngine;

/*[HarmonyPatch(typeof(BaseGameManager), "BeginPlay")]
class GameStartPatch
{
    static void Postfix()
    {
        var system = GameObject.FindObjectOfType<SilhouettesSystem>();
        if (system != null)
        {
            system.OnGameStarted();
        }
    }
}*/