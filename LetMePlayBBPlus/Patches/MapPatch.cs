using HarmonyLib;

[HarmonyPatch(typeof(Map), "Update")]
class MapUpdateBlockerPatch
{
    public static bool BlockUpdate = false;

    static bool Prefix()
    {
        return !BlockUpdate;
    }
}