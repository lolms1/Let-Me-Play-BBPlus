using HarmonyLib;
using UnityEngine;
using System.Collections;
namespace LetMePlayBBPlus;

[HarmonyPatch(typeof(Cumulo), "VirtualUpdate")]
class CumuloRedirectPatch
{
    private static bool alreadyRedirected = false;

    static void Postfix(Cumulo __instance)
    {
        if (!CharacterSpawnSystem.activeCumulo) return;
        if (alreadyRedirected) return;

        EnvironmentController ec = Singleton<BaseGameManager>.Instance.Ec;

        PlayerManager pm = ec.Players[0];

        Cell playerCell = ec.CellFromPosition(pm.transform.position);

        var startCellField = AccessTools.Field(typeof(Cumulo), "_startCell");
        startCellField.SetValue(__instance, playerCell);

        if (__instance.behaviorStateMachine.CurrentState is Cumulo_Blowing)
        {
            __instance.StopBlowing();
        }

        __instance.Navigator.FindPath(playerCell.FloorWorldPosition);

        __instance.transform.position = playerCell.FloorWorldPosition;

        float blowTime = __instance.RandomBlowTime;
        var blowState = new Cumulo_Blowing(__instance, blowTime);

        var startPosField = AccessTools.Field(typeof(Cumulo_Blowing), "startPosition");
        startPosField.SetValue(blowState, playerCell.FloorWorldPosition);

        __instance.behaviorStateMachine.ChangeState(blowState);

        alreadyRedirected = true;
        __instance.StartCoroutine(ResetRedirectAfterDelay());
    }

    private static IEnumerator ResetRedirectAfterDelay()
    {
        yield return new WaitForSeconds(10f);
        CharacterSpawnSystem.activeCumulo = false;
        alreadyRedirected = false;
    }
}

[HarmonyPatch(typeof(Cumulo_Blowing), "Update")]
class CumuloBlowingStayPatch
{
    static bool Prefix(Cumulo_Blowing __instance)
    {
        if (!CharacterSpawnSystem.activeCumulo) return true;

        Cumulo cumulo = (Cumulo)AccessTools.Field(typeof(Cumulo_StateBase), "cumulo").GetValue(__instance);
        if (cumulo == null) return true;

        var startPosField = AccessTools.Field(typeof(Cumulo_Blowing), "startPosition");
        startPosField.SetValue(__instance, cumulo.transform.position);

        return true;
    }
}