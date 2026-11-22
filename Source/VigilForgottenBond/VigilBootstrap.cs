using System;
using System.Linq;
using HarmonyLib;
using Verse;

namespace VigilForgottenBond
{
    [StaticConstructorOnStartup]
    internal static class VigilBootstrap
    {
        static VigilBootstrap()
        {
            // Harmony.PatchAll() aborts at the first patch whose target cannot be found,
            // silently leaving every later patch unapplied. Patch class by class instead,
            // so a single broken target is reported by name and the rest still load.
            var harmony = new Harmony("vigil.forgottenbond.core");
            var patchTypes = AccessTools.GetTypesFromAssembly(typeof(VigilBootstrap).Assembly)
                .Where(t => t.GetCustomAttributes(typeof(HarmonyPatch), false).Any());
            int ok = 0, failed = 0;
            foreach (Type type in patchTypes)
            {
                try { harmony.CreateClassProcessor(type).Patch(); ok++; }
                catch (Exception e)
                {
                    failed++;
                    Log.Error("[VIGIL] Patch " + type.Name + " failed: " + e);
                }
            }
            string msg = "[VIGIL] Forgotten Bond alpha v0.9.1 loaded: " + ok + " patches applied";
            if (failed > 0) Log.Error(msg + ", " + failed + " FAILED (see errors above).");
            else Log.Message(msg + ".");
        }
    }
}
