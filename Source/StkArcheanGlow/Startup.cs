using HarmonyLib;
using Verse;

namespace StkArcheanGlow;

[StaticConstructorOnStartup]
public static class Startup
{
	static Startup()
	{
		var harmony = new Harmony("stk.archeanglow");
		harmony.PatchAll();
	}

}