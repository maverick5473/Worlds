using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

public class DomClanRejectsDemandInfluenceDecision : PolityDecision {

	public const float BaseMinPreferencePercentChange = 0.15f;
	public const float BaseMaxPreferencePercentChange = 0.30f;

	public const float BaseMinRelationshipPercentChange = 0.05f;
	public const float BaseMaxRelationshipPercentChange = 0.15f;

	public const float BaseMinInfluencePercentChange = 0.05f;
	public const float BaseMaxInfluencePercentChange = 0.15f;

	private Tribe _tribe;

	private bool _cantPrevent = false;
	private bool _acceptDemand = true;

	private Clan _dominantClan;
	private Clan _demandClan;

	private static string GenerateDescriptionIntro (Tribe tribe, Clan demandClan, Clan dominantClan) {

		return 
			demandClan.CurrentLeader.Name.BoldText + ", leader of clan " + demandClan.Name.BoldText + ", has demanded greater influence on the " + tribe.Name.BoldText + 
			" tribe at the expense of clan " + dominantClan.Name.BoldText + ".\n\n";
	}

	public DomClanRejectsDemandInfluenceDecision (Tribe tribe, Clan demandClan, Clan dominantClan) : base (tribe) {

		_tribe = tribe;

		_dominantClan = dominantClan;
		_demandClan = demandClan;

		Description = GenerateDescriptionIntro (tribe, demandClan) +
			"Unfortunately, the situation is beyond control for the tribe leader, " + dominantClan.CurrentLeader.Name.BoldText + ", to be able to do anything other than let " +
			"clan " + demandClan.Name.BoldText + " have more influence over the tribe...";

		_cantPrevent = true;
	}

	public DomClanRejectsDemandInfluenceDecision (Tribe tribe, Clan splitClan, Clan dominantClan, bool preferSplit) : base (tribe) {

		_tribe = tribe;

		_dominantClan = dominantClan;
		_demandClan = splitClan;

		Description = GenerateDescriptionIntro (tribe, splitClan) +
			"Should the tribe leader, " + _dominantClan.CurrentLeader.Name.BoldText + ", allow clan " + splitClan.Name.BoldText + " to leave the tribe and form its own?";

		_acceptDemand = preferSplit;
	}

	private void GeneratePreventSplitResultEffectsString_Influence (out string effectSplitClan, out string effectDominantClan) {

		float charismaFactor = _dominantClan.CurrentLeader.Charisma / 10f;
		float wisdomFactor = _dominantClan.CurrentLeader.Wisdom / 15f;

		float attributesFactor = Mathf.Max (charismaFactor, wisdomFactor);
		attributesFactor = Mathf.Clamp (attributesFactor, 0.5f, 2f);

		float minPercentChange = BaseMinInfluencePercentChange / attributesFactor;
		float maxPercentChange = BaseMaxInfluencePercentChange / attributesFactor;

		float oldInfluenceValue = _dominantClan.Influence;

		float minValChange = oldInfluenceValue * (1f - minPercentChange);
		float maxValChange = oldInfluenceValue * (1f - maxPercentChange);

		float oldSplitClanInfluenceValue = _demandClan.Influence;

		float minValChangeSplitClan = oldSplitClanInfluenceValue + oldInfluenceValue - minValChange;
		float maxValChangeSplitClan = oldSplitClanInfluenceValue + oldInfluenceValue - maxValChange;

		effectDominantClan = "Clan " + _dominantClan.Name.BoldText + ": influence within the " + _tribe.Name.BoldText + 
			" tribe (" + oldInfluenceValue.ToString ("P") + ") decreases to: " + minValChange.ToString ("P") + " - " + maxValChange.ToString ("P");

		effectSplitClan = "Clan " + _demandClan.Name.BoldText + ": influence within the " + _tribe.Name.BoldText + 
			" tribe (" + oldSplitClanInfluenceValue.ToString ("P") + ") increases to: " + minValChangeSplitClan.ToString ("P") + " - " + maxValChangeSplitClan.ToString ("P");
	}

	private string GeneratePreventSplitResultEffectsString_Relationship () {

		float charismaFactor = _dominantClan.CurrentLeader.Charisma / 10f;
		float wisdomFactor = _dominantClan.CurrentLeader.Wisdom / 15f;

		float attributesFactor = Mathf.Max (charismaFactor, wisdomFactor);
		attributesFactor = Mathf.Clamp (attributesFactor, 0.5f, 2f);

		float minPercentChange = BaseMinRelationshipPercentChange * attributesFactor;
		float maxPercentChange = BaseMaxRelationshipPercentChange * attributesFactor;

		float originalValue = _dominantClan.GetRelationshipValue (_demandClan);

		float minValChange = MathUtility.IncreaseByPercent (originalValue, minPercentChange);
		float maxValChange = MathUtility.IncreaseByPercent (originalValue, maxPercentChange);

		return "Clan " + _dominantClan.Name.BoldText + ": relationship with clan " + _demandClan.Name.BoldText + " (" + originalValue.ToString ("0.00") + ") increases to: " + 
			minValChange.ToString ("0.00") + " - " + maxValChange.ToString ("0.00");
	}

	private string GeneratePreventSplitResultEffectsString () {

		string splitClanInfluenceChangeEffect;
		string dominantClanInfluenceChangeEffect;

		GeneratePreventSplitResultEffectsString_Influence (out splitClanInfluenceChangeEffect, out dominantClanInfluenceChangeEffect);

		return 
			"\t• " + GeneratePreventSplitResultEffectsString_Relationship () + "\n" + 
			"\t• " + dominantClanInfluenceChangeEffect + "\n" + 
			"\t• " + splitClanInfluenceChangeEffect;
	}

	public static void LeaderPreventsSplit_notifySplitClan (Clan splitClan, Clan dominantClan, Tribe originalTribe) {

		World world = originalTribe.World;

		if (originalTribe.IsUnderPlayerFocus || splitClan.IsUnderPlayerGuidance) {

			Decision decision = new PreventedClanTribeSplitDecision (originalTribe, splitClan, dominantClan); // Notify player that tribe leader prevented split

			if (splitClan.IsUnderPlayerGuidance) {

				world.AddDecisionToResolve (decision);

			} else {

				decision.ExecutePreferredOption ();
			}

		} else {

			PreventedClanTribeSplitDecision.TribeLeaderPreventedSplit (splitClan, dominantClan, originalTribe);
		}
	}

	public static void LeaderPreventsSplit (Clan splitClan, Clan dominantClan, Tribe tribe) {

		float charismaFactor = splitClan.CurrentLeader.Charisma / 10f;
		float wisdomFactor = splitClan.CurrentLeader.Wisdom / 15f;

		float attributesFactor = Mathf.Max (charismaFactor, wisdomFactor);
		attributesFactor = Mathf.Clamp (attributesFactor, 0.5f, 2f);

		int rngOffset = RngOffsets.TRIBE_SPLITTING_EVENT_TRIBE_LEADER_PREVENTS_MODIFY_ATTRIBUTE;

		// Influence

		float randomFactor = dominantClan.GetNextLocalRandomFloat (rngOffset++);
		float influencePercentChange = (BaseMaxInfluencePercentChange - BaseMinInfluencePercentChange) * randomFactor + BaseMinInfluencePercentChange;
		influencePercentChange /= attributesFactor;

		Polity.TransferInfluence (dominantClan, splitClan, influencePercentChange);

		// Relationship

		randomFactor = dominantClan.GetNextLocalRandomFloat (rngOffset++);
		float relationshipPercentChange = (BaseMaxRelationshipPercentChange - BaseMinRelationshipPercentChange) * randomFactor + BaseMinRelationshipPercentChange;
		relationshipPercentChange *= attributesFactor;

		float newValue = MathUtility.IncreaseByPercent (dominantClan.GetRelationshipValue (splitClan), relationshipPercentChange);
		Faction.SetRelationship (dominantClan, splitClan, newValue);

		// Updates

		LeaderPreventsSplit_notifySplitClan (splitClan, dominantClan, tribe);
	}

	private void PreventSplit () {

		LeaderPreventsSplit (_demandClan, _dominantClan, _tribe);
	}

	private string GenerateAllowSplitResultMessage () {

		string message = "\t• Clan " + _demandClan.Name.BoldText + " will leave the " + _tribe.Name.BoldText + " tribe and form a tribe of their own";

		return message;
	}

	public static void LeaderAllowsSplit (Clan splitClan, Clan dominantClan, Tribe originalTribe) {

		Tribe newTribe = new Tribe (splitClan, originalTribe);
		newTribe.Initialize ();

		splitClan.World.AddPolity (newTribe);

		splitClan.SetToUpdate ();
		dominantClan.SetToUpdate ();

		originalTribe.AddEventMessage (new TribeSplitEventMessage (splitClan, originalTribe, newTribe, splitClan.World.CurrentDate));
	}

	private void AllowSplit () {

		LeaderAllowsSplit (_demandClan, _dominantClan, _tribe);
	}

	public override Option[] GetOptions () {

		if (_cantPrevent) {

			return new Option[] {
				new Option ("Oh well...", "Effects:\n" + GenerateAllowSplitResultMessage (), AllowSplit),
			};
		}

		return new Option[] {
			new Option ("Allow clan to form a new tribe...", "Effects:\n" + GenerateAllowSplitResultMessage (), AllowSplit),
			new Option ("Prevent clan from leaving tribe...", "Effects:\n" + GeneratePreventSplitResultEffectsString (), PreventSplit)
		};
	}

	public override void ExecutePreferredOption ()
	{
		if (_acceptDemand)
			AllowSplit ();
		else
			PreventSplit ();
	}
}
	