using HarmonyLib;
using LetMePlayBBPlus;
using UnityEngine;

/*
[HarmonyPatch(typeof(FogEvent), "Begin")]
class GameStartPatch
{
    static void Postfix(FogEvent __instance)
    {
        var audManField = AccessTools.Field(typeof(FogEvent), "audMan");
        AudioManager audMan = (AudioManager)audManField.GetValue(__instance);
    }
}*/