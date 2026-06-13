using HarmonyLib;
using LetMePlayBBPlus;

/*[HarmonyPatch(typeof(HudManager), "Awake")]
class AddSilhouettesSystemPatch
{
    static void Postfix(HudManager __instance)
    {
        if (__instance.GetComponent<SilhouettesSystem>() == null) 
        {
            __instance.gameObject.AddComponent<SilhouettesSystem>();
        }
    }
}*/