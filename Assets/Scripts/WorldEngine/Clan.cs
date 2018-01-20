﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

// Clan Leadership:
// -- Authority factors:
// ---- Agent Charisma
// ---- Agent Age * Clan's Elder Respect
// ---- Agent Timespan as Leader * Clan's Authority Respect

// -- Leader Authority has an effect on the chances of the tribe splitting: Greater authority = less chance of splitting
// -- Clan Family Cohesiveness has also an effect on the chances of the tribe splitting: Greater cohesiveness = less chance of splitting
// -- Preventing a clan from splitting will reduce the clan's respect for authority but increases the overall clan family cohesiveness

public class Clan : Faction {

	public const int LeadershipAvgSpan = 20;
	public const int MinClanLeaderStartAge = 16;
	public const int MaxClanLeaderStartAge = 50;
	
	public const int MinSocialOrganizationValue = 400;

	public const int MinCoreMigrationPopulation = 500;
	public const float MinCoreMigrationPolityInfluence = 0.3f;

	public const int TerminalAdministrativeLoadValue = 500000;

	public const int ClanSplitDateSpanFactorConstant = CellGroup.GenerationSpan * 400;

	public const string ClanType = "Clan";

	public const float Split_MinCoreInfluenceValue = 0.5f;
	public const float Split_MinCoreDistance = 1000f;
	public const float Split_MinProminenceTrigger = 0.3f;
	public const float Split_MinProminenceTransfer = 0.25f;
	public const float Split_ProminenceTransferProportion = 0.75f;

	[XmlAttribute("CoreMigDate")]
	public int ClanCoreMigrationEventDate;

	[XmlIgnore]
	public ClanCoreMigrationEvent ClanCoreMigrationEvent;

	public Clan () {

	}

	public Clan (Polity polity, CellGroup coreGroup, float prominence, Clan parentClan = null) : base (ClanType, polity, coreGroup, prominence, parentClan) {

		if (ClanCoreMigrationEvent.CanBeAssignedTo (this)) {

			ClanCoreMigrationEventDate = ClanCoreMigrationEvent.CalculateTriggerDate (this);

			ClanCoreMigrationEvent = new ClanCoreMigrationEvent (this, ClanCoreMigrationEventDate);

			World.InsertEventToHappen (ClanCoreMigrationEvent);
		}

//		string logMessage = "New clan '" + Name + "' spawned in Polity '" + Polity.Name + "'";
//
//		if (parentClan != null) {
//		
//			logMessage += " from clan '" + parentClan.Name + "'";
//		}
//
//		logMessage += " in year " + World.CurrentDate + ", starting prominence: " + prominence;
//
//		if (parentClan != null) {
//			
//			Debug.Log (logMessage);
//		}
	}

	public CellGroup GetCoreGroupMigrationTarget () {

		Direction migrationDirection = CoreGroup.GenerateCoreMigrationDirection ();

		if (migrationDirection == Direction.Null) {
			return null;
		}

		return CoreGroup.Neighbors [migrationDirection];
	}

	public override void FinalizeLoad () {
	
		base.FinalizeLoad ();

		if (ClanCoreMigrationEvent.CanBeAssignedTo (this)) {

			ClanCoreMigrationEvent = new ClanCoreMigrationEvent (this, ClanCoreMigrationEventDate);

			World.InsertEventToHappen (ClanCoreMigrationEvent);
		}
	}

	public override void HandleUpdateEvent () {

		if (ShouldTrySplitting ()) {
			EvaluateSplitDecision ();
		}
	}

	protected override void UpdateInternal () {

		if (NewCoreGroup != null) {
			if ((NewCoreGroup != null) && (IsGroupValidCore (NewCoreGroup))) {
				MigrateToNewCoreGroup ();
			}

			NewCoreGroup = null;

			ClanCoreMigrationEventDate = ClanCoreMigrationEvent.CalculateTriggerDate (this);

			ClanCoreMigrationEvent.Reset (ClanCoreMigrationEventDate);

			World.InsertEventToHappen (ClanCoreMigrationEvent);
		}
	}

	protected override void GenerateName (Faction parentFaction) {

		int rngOffset = RngOffsets.CLAN_GENERATE_NAME + (int)Polity.Id;

		if (parentFaction != null)
			rngOffset += (int)parentFaction.Id;

		GetRandomIntDelegate getRandomInt = (int maxValue) => Polity.GetNextLocalRandomInt (rngOffset++, maxValue);
		Language.GetRandomFloatDelegate getRandomFloat = () => Polity.GetNextLocalRandomFloat (rngOffset++);

		Language language = Polity.Culture.Language;
		Region region = CoreGroup.Cell.Region;

		string untranslatedName = "";
		Language.Phrase namePhrase = null;

		if (region.Elements.Count <= 0) {
			throw new System.Exception ("No elements to choose name from");
		}

		IEnumerable<string> possibleAdjectives = null;

		List<Element> remainingElements = new List<Element> (region.Elements);

		bool addMoreWords = true;

		bool isPrimaryNoun = true;
		float extraWordChance = 0.2f;

		List<Element> usedElements = new List<Element> ();

		while (addMoreWords) {

			addMoreWords = false;

			bool hasRemainingElements = remainingElements.Count > 0;

			if ((!hasRemainingElements) && (usedElements.Count <= 0)) {

				throw new System.Exception ("No elements to use for name");
			}

			Element element = null;

			if (hasRemainingElements) {
				element = remainingElements.RandomSelectAndRemove (getRandomInt);

				usedElements.Add (element);

			} else {
				element = usedElements.RandomSelect (getRandomInt);
			}

			if (isPrimaryNoun) {
				untranslatedName = element.SingularName;
				isPrimaryNoun = false;

				possibleAdjectives = element.Adjectives;

			} else {
				bool first = true;
				foreach (Element usedElement in usedElements) {
					if (first) {
						untranslatedName = usedElement.SingularName;
						first = false;
					} else {
						untranslatedName = usedElement.SingularName + ":" + untranslatedName;
					}
				}
			}

			string adjective = possibleAdjectives.RandomSelect (getRandomInt, 2 * usedElements.Count);

			if (!string.IsNullOrEmpty (adjective)) {
				untranslatedName = "[adj]" + adjective + " " + untranslatedName;
			}

			addMoreWords = extraWordChance > getRandomFloat ();

			if (!addMoreWords) {
				
				foreach (Faction faction in Polity.GetFactions ()) {

					if (Language.ClearConstructCharacters(untranslatedName) == faction.Name.Meaning) {
						addMoreWords = true;
						break;
					}
				}
			}

			extraWordChance /= 2f;
		}

		untranslatedName = "[Proper][NP](" + untranslatedName + ")";

		namePhrase = language.TranslatePhrase (untranslatedName);

		Name = new Name (namePhrase, untranslatedName, language, World);

//		#if DEBUG
//		Debug.Log ("Clan #" + Id + " name: " + Name);
//		#endif
	}

	protected override Agent RequestCurrentLeader ()
	{
		return RequestCurrentLeader (LeadershipAvgSpan, MinClanLeaderStartAge, MaxClanLeaderStartAge, RngOffsets.CLAN_LEADER_GEN_OFFSET);
	}

	protected override Agent RequestNewLeader ()
	{
		return RequestNewLeader (LeadershipAvgSpan, MinClanLeaderStartAge, MaxClanLeaderStartAge, RngOffsets.CLAN_LEADER_GEN_OFFSET);
	}

	public static bool CanBeClanCore (CellGroup group)
	{
		CulturalKnowledge knowledge = group.Culture.GetKnowledge (SocialOrganizationKnowledge.SocialOrganizationKnowledgeId);

		if (knowledge == null)
			return false;

		if (knowledge.Value < MinSocialOrganizationValue)
			return false;

		return true;
	}

	public bool IsGroupValidCore (CellGroup group)
	{
		if (!CanBeClanCore(group))
			return false;

		CulturalDiscovery discovery = group.Culture.GetDiscovery (TribalismDiscovery.TribalismDiscoveryId);

		if (discovery == null)
			return false;

		PolityInfluence pi = group.GetPolityInfluence (Polity);

		if (pi == null)
			return false;

		if (pi.Value < MinCoreMigrationPolityInfluence)
			return false;

		if (group.Population < MinCoreMigrationPopulation)
			return false;

		return true;
	}

	public override bool ShouldMigrateFactionCore (CellGroup sourceGroup, CellGroup targetGroup) {

		if (!CanBeClanCore (targetGroup))
			return false;

		PolityInfluence piTarget = targetGroup.GetPolityInfluence (Polity);

		if (piTarget != null) {
			int targetGroupPopulation = targetGroup.Population;
			float targetGroupInfluence = piTarget.Value;

			return ShouldMigrateFactionCore (sourceGroup, targetGroup.Cell, targetGroupInfluence, targetGroupPopulation);
		}

		return false;
	}

	public override bool ShouldMigrateFactionCore (CellGroup sourceGroup, TerrainCell targetCell, float targetInfluence, int targetPopulation) {

		float targetInfluenceFactor = Mathf.Max (0, targetInfluence - MinCoreMigrationPolityInfluence);

		if (targetInfluenceFactor <= 0)
			return false;

		float targetPopulationFactor = Mathf.Max (0, targetPopulation - MinCoreMigrationPopulation);

		if (targetPopulationFactor <= 0)
			return false;

		int sourcePopulation = sourceGroup.Population;

		PolityInfluence pi = sourceGroup.GetPolityInfluence (Polity);

		if (pi == null) {
			Debug.LogError ("Unable to find Polity with Id: " + Polity.Id);
		}

		float sourceInfluence = pi.Value;

		float sourceInfluenceFactor = Mathf.Max (0, sourceInfluence - MinCoreMigrationPolityInfluence);
		float sourcePopulationFactor = Mathf.Max (0, sourcePopulation - MinCoreMigrationPopulation);

		float sourceFactor = sourceInfluenceFactor * sourcePopulationFactor;

		if (sourceFactor <= 0)
			return true;

		float targetFactor = targetInfluenceFactor * targetPopulationFactor;

		float migrateCoreFactor = sourceFactor / (sourceFactor + targetFactor);

		float randomValue = sourceGroup.GetNextLocalRandomFloat (RngOffsets.MIGRATING_GROUP_MOVE_FACTION_CORE + (int)Id);

		if (randomValue > migrateCoreFactor)
			return true;

		return false;
	}

	public float Split_GetGroupWeight (CellGroup group) {

		if (group == CoreGroup)
			return 0;

		PolityInfluence pi = group.GetPolityInfluence (Polity);

		if (group.HighestPolityInfluence != pi)
			return 0;

		if (!CanBeClanCore (group))
			return 0;

		float coreDistance = pi.FactionCoreDistance - Split_MinCoreDistance;

		if (coreDistance <= 0)
			return 0;

		float coreDistanceFactor = Split_MinCoreDistance / (Split_MinCoreDistance + coreDistance);

		float minCoreInfluenceValue = Split_MinCoreInfluenceValue * coreDistanceFactor;

		float value = pi.Value - minCoreInfluenceValue;

		if (value <= 0)
			return 0;

		float weight = pi.Value;

		if (weight < 0)
			return float.MaxValue;

		return weight;
	}

	public bool ShouldTrySplitting () {

		if (Prominence < Split_MinProminenceTrigger)
			return false;

		int rngOffset = (int)(RngOffsets.CLAN_SHOULD_SPLIT + Id);

		_splitFactionCoreGroup = Polity.GetRandomGroup (rngOffset++, Split_GetGroupWeight, true);

		if (_splitFactionCoreGroup == null)
			return false;

		float administrativeLoadFactor = CalculateAdministrativeLoadFactor ();

		if (administrativeLoadFactor < 0)
			return true;

		float splitValue = administrativeLoadFactor / (administrativeLoadFactor + TerminalAdministrativeLoadValue);

		float triggerValue = GetNextLocalRandomFloat (rngOffset++);

		if (triggerValue > splitValue)
			return false;

		return true;
	}

	public void EvaluateSplitDecision () {

		bool shouldSucceed = GetNextLocalRandomInt (RngOffsets.CLAN_PREFER_SPLIT, 4) == 0;

		if (IsFocused) {

			Decision splitDecision = new ClanSplitDecision (this, _splitFactionCoreGroup, shouldSucceed);

			if (IsControlled) {

				World.AddDecisionToResolve (splitDecision);

			} else {

				splitDecision.ExecutePreferredOption ();
			}

		} else if (shouldSucceed) {

			SetToSplit (_splitFactionCoreGroup);
		}
	}

	public override void Split () {

//		#if DEBUG
//		if (Polity.Territory.IsSelected) {
//			bool debug = true;
//		}
//		#endif

		float randomValue = GetNextLocalRandomFloat ((int)(RngOffsets.CLAN_SPLIT + Id));
		float randomFactor = Split_MinProminenceTransfer + (randomValue * Split_ProminenceTransferProportion);

		float oldProminence = Prominence;

		Prominence = oldProminence * randomFactor;

		float newClanProminence = oldProminence * (1f - randomFactor);

		Clan newClan = new Clan (Polity as Tribe, _splitFactionCoreGroup, newClanProminence, this);

		Polity.AddFaction (newClan);

		World.AddFactionToUpdate (this);
		World.AddFactionToUpdate (newClan);

		World.AddPolityToUpdate (Polity);

		Polity.AddEventMessage (new ClanSplitEventMessage (this, newClan, World.CurrentDate));
	}

	public float CalculateAdministrativeLoadFactor () {

		float socialOrganizationValue = 0;

		CulturalKnowledge socialOrganizationKnowledge = Polity.Culture.GetKnowledge (SocialOrganizationKnowledge.SocialOrganizationKnowledgeId);

		if (socialOrganizationKnowledge != null)
			socialOrganizationValue = socialOrganizationKnowledge.Value;

		if (socialOrganizationValue < 0) {

			return float.MaxValue / 2f;
		}

		float administrativeLoad = Polity.TotalAdministrativeCost * Prominence;

		return Mathf.Pow (administrativeLoad / socialOrganizationValue, 2);
	}

	public override int CalculateNextUpdateDate () {

		int nextDate_adminLoad = CalculateNextUpdateDate_AdminLoad ();

		return nextDate_adminLoad;
	}

	public int CalculateNextUpdateDate_AdminLoad () {

		int updateSpan = CellGroup.GenerationSpan * 40;

		float randomFactor = GetNextLocalRandomFloat (RngOffsets.FACTION_CALCULATE_NEXT_UPDATE);
		randomFactor = Mathf.Pow (randomFactor, 2);

		float administrativeLoadFactor = CalculateAdministrativeLoadFactor ();

		if (administrativeLoadFactor < 0)
			administrativeLoadFactor = float.MaxValue / 2f;

		float loadFactor = TerminalAdministrativeLoadValue / (administrativeLoadFactor + TerminalAdministrativeLoadValue);

		float dateSpan = (1 - randomFactor) * ClanSplitDateSpanFactorConstant * loadFactor;

		updateSpan += (int)dateSpan;

		if (updateSpan < 0)
			updateSpan = CellGroup.MaxUpdateSpan;

		return World.CurrentDate + updateSpan;
	}
}

public class PreventClanSplitEventMessage : FactionEventMessage {

	[XmlAttribute]
	public long AgentId;

	public PreventClanSplitEventMessage () {

	}

	public PreventClanSplitEventMessage (Faction faction, Agent agent, long date) : base (faction, WorldEvent.PreventClanSplitEventId, date) {

		faction.World.AddMemorableAgent (agent);

		AgentId = agent.Id;
	}

	protected override string GenerateMessage ()
	{
		Agent leader = World.GetMemorableAgent (AgentId);

		return leader.Name.Text + " has prevented clan " +  Faction.Name.Text + " from splitting";
	}
}

public class ClanSplitEventMessage : FactionEventMessage {

	[XmlAttribute]
	public long NewClanId;

	public ClanSplitEventMessage () {

	}

	public ClanSplitEventMessage (Faction faction, Clan newClan, long date) : base (faction, WorldEvent.ClanSplitEventId, date) {

		NewClanId = newClan.Id;
	}

	protected override string GenerateMessage ()
	{
		Faction newClan = World.GetFaction (NewClanId);

		return "A new clan, " + newClan.Name.Text + ", has split from clan " +  Faction.Name.Text;
	}
}

public class ClanSplitDecision : FactionDecision {

	private bool _preferSplit;

	private CellGroup _newCoreGroup;

	public ClanSplitDecision (Clan clan, CellGroup newCoreGroup, bool preferSplit) : base (clan) {

		Description = "(" + preferSplit + ") Several family groups belonging to clan <b>" + clan.Name.Text + "</b> no longer feel to be connected to the rest of the clan. " +
			"Should the clan leader, <b>" + clan.CurrentLeader.Name.Text + "</b>, try to keep them from splitting apart?";

		_preferSplit = preferSplit;

		_newCoreGroup = newCoreGroup;
	}

	private void PreventSplit () {

		Faction.Polity.AddEventMessage (new PreventClanSplitEventMessage (Faction, Faction.CurrentLeader, Faction.World.CurrentDate));
	}

	private void AllowSplit () {

		Faction.SetToSplit (_newCoreGroup);
	}

	public override Option[] GetOptions () {

		return new Option[] {
			new Option ("Allow clan to split in two...", AllowSplit, _preferSplit),
			new Option ("Prevent clan from splitting...", PreventSplit, !_preferSplit)
		};
	}

	public override void ExecutePreferredOption ()
	{
		if (_preferSplit)
			AllowSplit ();
		else
			PreventSplit ();
	}
}

public class ClanCoreMigrationEvent : FactionEvent {

	public const int DateSpanFactorConstant = CellGroup.GenerationSpan * 500;

	private CellGroup _targetGroup;

	//	public const string EventSetFlag = "ClanCoreMigrationEvent_Set";

	public ClanCoreMigrationEvent () {

		DoNotSerialize = true;
	}

	public ClanCoreMigrationEvent (Clan clan, int triggerDate) : base (clan, triggerDate, ClanCoreMigrationEventId) {

		DoNotSerialize = true;
	}

	public static int CalculateTriggerDate (Clan clan) {

		float randomFactor = clan.GetNextLocalRandomFloat (RngOffsets.CLAN_CORE_MIGRATION_EVENT_CALCULATE_TRIGGER_DATE);
		randomFactor = Mathf.Pow (randomFactor, 2);

		float dateSpan = (1 - randomFactor) * DateSpanFactorConstant;

		int targetDate = (int)(clan.World.CurrentDate + dateSpan) + 1;

		return targetDate;
	}

	public static bool CanBeAssignedTo (Clan clan) {

		//		if (clan.IsFlagSet (EventSetFlag))
		//			return false;

		return true;
	}

	public override bool CanTrigger () {

		if (!base.CanTrigger ())
			return false;

//		#if DEBUG
//		if (Faction.Polity.Territory.IsSelected) {
//			bool debug = true;
//		}
//		#endif

		Clan clan = Faction as Clan;

		_targetGroup = clan.GetCoreGroupMigrationTarget ();

		if (_targetGroup == null)
			return false;

		return Faction.ShouldMigrateFactionCore (Faction.CoreGroup, _targetGroup);
	}

	public override void Trigger () {

		Faction.PrepareNewCoreGroup (_targetGroup);

		World.AddGroupToUpdate (Faction.CoreGroup);
		World.AddGroupToUpdate (_targetGroup);

		World.AddFactionToUpdate (Faction);

		World.AddPolityToUpdate (Faction.Polity);
	}

	protected override void DestroyInternal () {

		base.DestroyInternal ();

		//		if (Faction != null) {
		//			Faction.UnsetFlag (EventSetFlag);
		//		}

		if ((Faction != null) && (Faction.StillPresent)) {

			Clan clan = Faction as Clan;

			if (CanBeAssignedTo (clan)) {

				clan.ClanCoreMigrationEvent = this;

				clan.ClanCoreMigrationEventDate = CalculateTriggerDate (clan);

				Reset (clan.ClanCoreMigrationEventDate);

				World.InsertEventToHappen (this);
			}
		}
	}

	public override void FinalizeLoad () {

		base.FinalizeLoad ();

		Clan clan = Faction as Clan;

		clan.ClanCoreMigrationEvent = this;
	}
}
