using HarmonyLib;
using Verse;

namespace VigilForgottenBond
{
    [StaticConstructorOnStartup]
    internal static class VigilBootstrap
    {
        static VigilBootstrap()
        {
            new Harmony("vigil.forgottenbond.core").PatchAll();
            Log.Message("[VIGIL] Forgotten Bond alpha loaded.");
        }
    }
}
