using System.Collections.Generic;
using RimWorld;
using StkArcheanGlow.Gizmo;
using UnityEngine;
using Verse;

namespace StkArcheanGlow.CompProperties;

[StaticConstructorOnStartup]
public class CompPlantGlowerPruning : CompGlower
{
	private readonly Pawn connectedPawn;
	public Pawn ConnectedPawn => connectedPawn;
	private int spawnTick = -1;
	private float connectionStrength;
	private int lastPrunedTick;
	private float desiredConnectionStrength = 0.5f;
	private Effecter leafEffecter;
	private PruningConfig pruningGizmo;
	private const int ConnectionTornDurationTicks = 1800000;
	private const int CheckPodSpawnInterval = 300;
	private readonly float TimeBetweenAutoPruning = 10000f;
	private const float PruningConnectionStrengthDithering = 0.03f;
	private const float PruningSpeedFactor_DisabledSkill = 0.75f;
	public new PlantGlowerPruning Props => (PlantGlowerPruning)props;
	public float ConnectionStrength
	{
		get
		{
			return connectionStrength;
		}
		set
		{
			connectionStrength = Mathf.Clamp01(value);
		}
	}
	public float DesiredConnectionStrength
	{
		get
		{
			return desiredConnectionStrength;
		}
		set
		{
			desiredConnectionStrength = Mathf.Clamp01(value);
		}
	}
	public float ConnectionStrengthLossPerDay
	{
		get
		{
			float num = Props.connectionLossPerLevelCurve.Evaluate(ConnectionStrength);
			return num;
		}
	}

	public float ConnectionStrengthGainPerHourOfPruning
	{
		get
		{
			float connectionStrengthGainPerHourPruningBase = Props.connectionStrengthGainPerHourPruningBase;
			connectionStrengthGainPerHourPruningBase = ((!StatDefOf.PruningSpeed.Worker.IsDisabledFor(ConnectedPawn)) ? (connectionStrengthGainPerHourPruningBase * ConnectedPawn.GetStatValue(StatDefOf.PruningSpeed)) : (connectionStrengthGainPerHourPruningBase * PruningSpeedFactor_DisabledSkill));
			if (Props.connectionStrengthGainPerPlantSkill != null)
			{
				connectionStrengthGainPerHourPruningBase *= Props.connectionStrengthGainPerPlantSkill.Evaluate(ConnectedPawn.skills.GetSkill(SkillDefOf.Plants).Level);
			}
			return connectionStrengthGainPerHourPruningBase;
		}
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		if (!respawningAfterLoad)
		{
			lastPrunedTick = Find.TickManager.TicksGame;
		}
	}

	public override void CompTick()
	{
		if (!ModsConfig.IdeologyActive)
		{
			return;
		}
		if (leafEffecter == null)
		{
			leafEffecter = EffecterDefOf.GauranlenLeavesBatch.Spawn();
			leafEffecter.Trigger(parent, parent);
		}
		leafEffecter?.EffectTick(parent, parent);
		if (!parent.IsHashIntervalTick(CheckPodSpawnInterval))
		{
			return;
		}
	}


	public void Prune(int delta)
	{
		lastPrunedTick = Find.TickManager.TicksGame;
		ConnectionStrength += ConnectionStrengthGainPerHourOfPruning * delta / 2500f;
	}

	public bool ShouldBePrunedNow(bool forced)
	{
		if (ConnectionStrength >= desiredConnectionStrength)
		{
			return false;
		}
		if (!forced)
		{
			if (ConnectionStrength >= desiredConnectionStrength - PruningConnectionStrengthDithering)
			{
				return false;
			}
			if (Find.TickManager.TicksGame < lastPrunedTick + TimeBetweenAutoPruning)
			{
				return false;
			}
		}
		return true;
	}

	public override IEnumerable<Verse.Gizmo> CompGetGizmosExtra()
	{
		{
			pruningGizmo ??= new PruningConfig(this);
			yield return pruningGizmo;
		}
		if (DebugSettings.ShowDevGizmos)
		{
			Command_Action command_Action3 = new()
			{
				defaultLabel = "DEV: Connection strength -10%",
				action = delegate
					{
						ConnectionStrength -= 0.1f;
					}
			};
			yield return command_Action3;
			Command_Action command_Action4 = new()
			{
				defaultLabel = "DEV: Connection strength +10%",
				action = delegate
					{
						ConnectionStrength += 0.1f;
					}
			};
			yield return command_Action4;
		}
	}

	public override string CompInspectStringExtra()
	{
		string text = base.CompInspectStringExtra();
		if (!text.NullOrEmpty())
		{
			text += "\n";
		}
		string text2 = string.Empty;
		{
			if (lastPrunedTick >= 0 && Find.TickManager.TicksGame - lastPrunedTick <= 60)
			{
				text = string.Concat(text, "\n", "PruningConnectionStrength".Translate(), ": ", "PerHour".Translate(ConnectionStrengthGainPerHourOfPruning.ToStringPercent()).Resolve());
			}
			if (!text2.NullOrEmpty())
			{
				text = text + "\n" + text2;
			}
		}
		if (!text2.NullOrEmpty())
		{
			text = text + "\n" + text2;
		}
		return text;
	}

	public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
	{
		yield return new StatDrawEntry(StatCategoryDefOf.BasicsNonPawn, "ConnectedPawn".Translate(), (ConnectedPawn != null) ? ConnectedPawn.NameFullColored : "Nobody".Translate(), "ConnectedPawnDesc".Translate(ConnectionTornDurationTicks.ToStringTicksToPeriod().Named("DURATION"), parent.Named("TREE")), 6010, null, Gen.YieldSingle(new Dialog_InfoCard.Hyperlink(ConnectedPawn)));
	}

	public float PruningHoursToMaintain(float desired)
	{
		float num = Props.connectionLossPerLevelCurve.Evaluate(desired);
		return num / ConnectionStrengthGainPerHourOfPruning;
	}

	public override void PostExposeData()
	{
		Scribe_Values.Look(ref spawnTick, "spawnTick", -1);
		Scribe_Values.Look(ref lastPrunedTick, "lastPrunedTick", 0);
		Scribe_Values.Look(ref desiredConnectionStrength, "desiredConnectionStrength", 0.5f);
		Scribe_Values.Look(ref connectionStrength, "connectionStrength", 0f);
	}

}

public class PlantGlowerPruning : CompProperties_Glower
{
	public FloatRange initialConnectionStrengthRange;
	public SimpleCurve connectionLossPerLevelCurve;
	public SimpleCurve connectionStrengthGainPerPlantSkill;
	public float connectionStrengthGainPerHourPruningBase = 0.01f;
	public PlantGlowerPruning()
	{
		compClass = typeof(CompPlantGlowerPruning);
	}

}