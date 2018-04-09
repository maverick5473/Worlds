using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine.Profiling;

public class CulturalKnowledgeInfo {

	[XmlAttribute]
	public string Id;
	
	[XmlAttribute]
	public string Name;
	
	public CulturalKnowledgeInfo () {
	}
	
	public CulturalKnowledgeInfo (string id, string name) {
		
		Id = id;
		Name = name;
	}
	
	public CulturalKnowledgeInfo (CulturalKnowledgeInfo baseInfo) {
		
		Id = baseInfo.Id;
		Name = baseInfo.Name;
	}
}

public class CulturalKnowledge : CulturalKnowledgeInfo {

	public const float ValueScaleFactor = 0.01f;

	[XmlAttribute]
	public int Value;

	public CulturalKnowledge () {
	}

	public CulturalKnowledge (string id, string name, int value) : base (id, name) {

		Value = value;
	}

	public CulturalKnowledge (CulturalKnowledge baseKnowledge) : base (baseKnowledge) {

		Value = baseKnowledge.Value;
	}

	public float ScaledValue {
		get { return Value * ValueScaleFactor; }
	}
}

public class PolityCulturalKnowledge : CulturalKnowledge {

	[XmlIgnore]
	public float AggregateValue;

	public PolityCulturalKnowledge () {
	}

	public PolityCulturalKnowledge (string id, string name, int value) : base (id, name, value) {
	}

	public PolityCulturalKnowledge (CulturalKnowledge baseKnowledge) : base (baseKnowledge) {
	}
}

public abstract class CellCulturalKnowledge : CulturalKnowledge, ISynchronizable {

	public const float MinProgressLevel = 0.001f;
	
	[XmlAttribute("PrgLvl")]
	public float ProgressLevel;
	
	[XmlAttribute("Asym")]
	public int Asymptote;

	[XmlAttribute("RO")]
	public int RngOffset;

	[XmlIgnore]
	public CellGroup Group;

	protected int _newValue;

	public float ScaledAsymptote {
		get { return Asymptote * ValueScaleFactor; }
	}
	
	public CellCulturalKnowledge () {

	}

	public CellCulturalKnowledge (CellGroup group, string id, string name, int typeRngOffset, int value) : base (id, name, value) {

		Group = group;
		RngOffset = (int)group.GenerateUniqueIdentifier (group.World.CurrentDate, 100L, typeRngOffset);

		_newValue = value;
	}

	public CellCulturalKnowledge (CellGroup group, string id, string name, int typeRngOffset, int value, int asymptote) : base (id, name, value) {

		Group = group;
		RngOffset = (int)group.GenerateUniqueIdentifier (group.World.CurrentDate, 100L, typeRngOffset);
		Asymptote = asymptote;

		_newValue = value;
	}

	public static CellCulturalKnowledge CreateCellInstance (CellGroup group, CulturalKnowledge baseKnowledge) {

		return CreateCellInstance (group, baseKnowledge, baseKnowledge.Value);
	}

	public static CellCulturalKnowledge CreateCellInstance (CellGroup group, CulturalKnowledge baseKnowledge, int initialValue) {

		if (ShipbuildingKnowledge.IsShipbuildingKnowledge (baseKnowledge)) {

			return new ShipbuildingKnowledge (group, baseKnowledge, initialValue);
		}

		if (AgricultureKnowledge.IsAgricultureKnowledge (baseKnowledge)) {

			return new AgricultureKnowledge (group, baseKnowledge, initialValue);
		}

		if (SocialOrganizationKnowledge.IsSocialOrganizationKnowledge (baseKnowledge)) {

			return new SocialOrganizationKnowledge (group, baseKnowledge, initialValue);
		}

		throw new System.Exception ("Unhandled CulturalKnowledge type: " + baseKnowledge.Id);
	}
	
	public int GetHighestAsymptote () {
		
		System.Type knowledgeType = this.GetType ();

		System.Reflection.FieldInfo fInfo = knowledgeType.GetField ("HighestAsymptote");
		
		return (int)fInfo.GetValue (this);
	}
	
	public void SetHighestAsymptote (int value) {
		
		System.Type knowledgeType = this.GetType ();
		
		System.Reflection.FieldInfo fInfo = knowledgeType.GetField ("HighestAsymptote");

		int currentValue = (int)fInfo.GetValue (this);
		fInfo.SetValue (this, Mathf.Max (value, currentValue));
	}

	public void Merge (CulturalKnowledge knowledge, float percentage) {

		float d;
		// _newvalue should have been set correctly either by the constructor or by the Update function
		int mergedValue = (int)MathUtility.MergeAndGetDecimals (_newValue, knowledge.Value, percentage, out d);

		if (d > Group.GetNextLocalRandomFloat (RngOffsets.KNOWLEDGE_MERGE + RngOffset))
			mergedValue++;

		#if DEBUG
		if ((Id == SocialOrganizationKnowledge.SocialOrganizationKnowledgeId) && (mergedValue < SocialOrganizationKnowledge.MinValueForHoldingTribalism)) {

			if (Group.GetFactionCores ().Count > 0) {

				Debug.LogWarning ("group with low social organization has faction cores - Id: " + Group.Id);
			}
		}
		#endif
	
		_newValue = mergedValue;
	}

	// This method should be called only once after a Knowledge is copied from another source group
	public void DecreaseValue (float percentage) {

		float d;
		int modifiedValue = (int)MathUtility.MultiplyAndGetDecimals (_newValue, percentage, out d);

		if (d > Group.GetNextLocalRandomFloat (RngOffsets.KNOWLEDGE_MODIFY_VALUE + RngOffset))
			modifiedValue++;

		#if DEBUG
		if ((Id == SocialOrganizationKnowledge.SocialOrganizationKnowledgeId) && (modifiedValue < SocialOrganizationKnowledge.MinValueForHoldingTribalism)) {

			if (Group.GetFactionCores ().Count > 0) {

				Debug.LogWarning ("group with low social organization has faction cores - Id: " + Group.Id);
			}
		}
		#endif
		
		_newValue = modifiedValue;
	}

	public virtual void Synchronize () {

	}

	public virtual void FinalizeLoad () {

	}
	
	public void UpdateProgressLevel () {

		ProgressLevel = 0;

		if (Asymptote > 0)
			ProgressLevel = MathUtility.RoundToSixDecimals (Mathf.Clamp01 (Value / (float)Asymptote));
	}

	public void CalculateAsymptote () {

		Asymptote = CalculateBaseAsymptote ();

		UpdateProgressLevel ();

		SetHighestAsymptote (Asymptote);
	}
	
	public void RecalculateAsymptote () {

		Asymptote = CalculateBaseAsymptote ();

		Group.Culture.Discoveries.ForEach (d => Asymptote = Mathf.Max (CalculateAsymptoteInternal (d), Asymptote));

		UpdateProgressLevel ();

		SetHighestAsymptote (Asymptote);
	}

	public void CalculateAsymptote (CulturalDiscovery discovery) {

		int newAsymptote = CalculateAsymptoteInternal (discovery);

		if (newAsymptote > Asymptote) {

			Asymptote = newAsymptote;

			UpdateProgressLevel ();

			SetHighestAsymptote (Asymptote);
		}
	}

	public void Update (long timeSpan) {

		UpdateInternal (timeSpan);
	}

	protected void UpdateValueInternal (long timeSpan, float timeEffectFactor, float specificModifier) {

		TerrainCell groupCell = Group.Cell;

		int rngOffset = RngOffsets.KNOWLEDGE_UPDATE_VALUE_INTERNAL + RngOffset;

		float randomModifier = groupCell.GetNextLocalRandomFloat (rngOffset++);
		randomModifier *= randomModifier;
		float randomFactor = specificModifier - randomModifier;
		randomFactor = Mathf.Clamp (randomFactor, -1, 1);

		float maxTargetValue = Asymptote;
		float minTargetValue = 0;
		float targetValue = 0;

		if (randomFactor > 0) {
			targetValue = Value + (maxTargetValue - Value) * randomFactor;
		} else {
			targetValue = Value - (minTargetValue - Value) * randomFactor;
		}

		float timeEffect = timeSpan / (float)(timeSpan + timeEffectFactor);

		float d;
		int newValue = (int)MathUtility.MergeAndGetDecimals (Value, targetValue, timeEffect, out d);

		if (d > Group.GetNextLocalRandomFloat (rngOffset++))
			newValue++;

		#if DEBUG
		if ((Asymptote > 1) && (newValue > Asymptote) && (newValue > Value)) {
			Debug.LogError ("UpdateValueInternal: new value " + newValue + " above Asymptote " + Asymptote);
		}
		#endif

		#if DEBUG
		if (newValue > 1000000) {
			Debug.LogError ("UpdateValueInternal: new value " + newValue + " above 1000000000");
		}
		#endif

		_newValue = newValue;
	}

	public abstract void PolityCulturalInfluence (CulturalKnowledge polityKnowledge, PolityInfluence polityInfluence, long timeSpan);

	protected void PolityCulturalInfluenceInternal (CulturalKnowledge polityKnowledge, PolityInfluence polityInfluence, long timeSpan, float timeEffectFactor) {

		int rngOffset = RngOffsets.KNOWLEDGE_POLITY_INFLUENCE + RngOffset + (int)polityInfluence.PolityId;

		int targetValue = polityKnowledge.Value;
		float influenceEffect = polityInfluence.Value;

		TerrainCell groupCell = Group.Cell;

		float randomEffect = groupCell.GetNextLocalRandomFloat (rngOffset++);

		float timeEffect = timeSpan / (float)(timeSpan + timeEffectFactor);

		int valueDelta = targetValue - _newValue;

		float d;
		// _newvalue should have been set correctly either by the constructor or by the Update function
		int valueChange = (int)MathUtility.MultiplyAndGetDecimals (valueDelta, influenceEffect * timeEffect * randomEffect, out d);

		if (d > Group.GetNextLocalRandomFloat (rngOffset++))
			valueChange++;

		_newValue = _newValue + valueChange;
	}

	public void PostUpdate () {
	
		Value = _newValue;

		UpdateProgressLevel ();
	}

	public abstract float CalculateExpectedProgressLevel ();
	public abstract float CalculateTransferFactor ();

	public abstract bool WillBeLost ();
	public abstract void LossConsequences ();

	protected abstract void UpdateInternal (long timeSpan);
	protected abstract int CalculateAsymptoteInternal (CulturalDiscovery discovery);
	protected abstract int CalculateBaseAsymptote ();
}

public class ShipbuildingKnowledge : CellCulturalKnowledge {

	public const string ShipbuildingKnowledgeId = "ShipbuildingKnowledge";
	public const string ShipbuildingKnowledgeName = "Shipbuilding";

	public const int ShipbuildingKnowledgeRngOffset = 0;

	public const int MinKnowledgeValueForSailingSpawnEvent = 500;
	public const int MinKnowledgeValueForSailing = 300;
	public const int OptimalKnowledgeValueForSailing = 1000;

	public const float TimeEffectConstant = CellGroup.GenerationSpan * 500;
	public const float NeighborhoodOceanPresenceModifier = 1.5f;

	public static int HighestAsymptote = 0;

	private float _neighborhoodOceanPresence;
	
	public ShipbuildingKnowledge () {

		if (Asymptote > HighestAsymptote) {
			
			HighestAsymptote = Asymptote;
		}
	}

	public ShipbuildingKnowledge (CellGroup group, int value = 100) : base (group, ShipbuildingKnowledgeId, ShipbuildingKnowledgeName, ShipbuildingKnowledgeRngOffset, value) {

		CalculateNeighborhoodOceanPresence ();
	}

	public ShipbuildingKnowledge (CellGroup group, ShipbuildingKnowledge baseKnowledge) : base (group, baseKnowledge.Id, baseKnowledge.Name, ShipbuildingKnowledgeRngOffset, baseKnowledge.Value, baseKnowledge.Asymptote) {
		
		CalculateNeighborhoodOceanPresence ();
	}
	
	public ShipbuildingKnowledge (CellGroup group, ShipbuildingKnowledge baseKnowledge, int initialValue) : base (group, baseKnowledge.Id, baseKnowledge.Name, ShipbuildingKnowledgeRngOffset, initialValue) {
		
		CalculateNeighborhoodOceanPresence ();
	}

	public ShipbuildingKnowledge (CellGroup group, CulturalKnowledge baseKnowledge, int initialValue) : base (group, baseKnowledge.Id, baseKnowledge.Name, ShipbuildingKnowledgeRngOffset, initialValue) {

		CalculateNeighborhoodOceanPresence ();
	}

	public static bool IsShipbuildingKnowledge (CulturalKnowledge knowledge) {

		return knowledge.Id.Contains (ShipbuildingKnowledgeId);
	}

	public override void FinalizeLoad () {

		base.FinalizeLoad ();

		CalculateNeighborhoodOceanPresence ();
	}
	
	public void CalculateNeighborhoodOceanPresence () {
		
		_neighborhoodOceanPresence = CalculateNeighborhoodOceanPresenceIn (Group);
	}
	
	public static float CalculateNeighborhoodOceanPresenceIn (CellGroup group) {

		float neighborhoodPresence;
		
		int groupCellBonus = 1;
		int cellCount = groupCellBonus;
		
		TerrainCell groupCell = group.Cell;
		
		float totalPresence = groupCell.GetBiomePresence ("Ocean") * groupCellBonus;

		foreach (TerrainCell c in groupCell.Neighbors.Values) {
			
			totalPresence += c.GetBiomePresence ("Ocean");
			cellCount++;
		}
		
		neighborhoodPresence = totalPresence / cellCount;

		if ((neighborhoodPresence < 0) || (neighborhoodPresence > 1)) {
			
			throw new System.Exception ("Neighborhood Ocean Presence outside range: " + neighborhoodPresence);
		}

		return neighborhoodPresence;
	}

	protected override void UpdateInternal (long timeSpan) {

		UpdateValueInternal (timeSpan, TimeEffectConstant, _neighborhoodOceanPresence * NeighborhoodOceanPresenceModifier);

		TryGenerateSailingDiscoveryEvent ();
	}

	public override void PolityCulturalInfluence (CulturalKnowledge polityKnowledge, PolityInfluence polityInfluence, long timeSpan) {

		PolityCulturalInfluenceInternal (polityKnowledge, polityInfluence, timeSpan, TimeEffectConstant);

		TryGenerateSailingDiscoveryEvent ();
	}

	private void TryGenerateSailingDiscoveryEvent () {

		if (Value < SailingDiscoveryEvent.MinShipBuildingKnowledgeSpawnEventValue)
			return;

		if (Value > SailingDiscoveryEvent.OptimalShipBuildingKnowledgeValue)
			return;

		if (SailingDiscoveryEvent.CanSpawnIn (Group)) {

			long triggerDate = SailingDiscoveryEvent.CalculateTriggerDate (Group);

			if (triggerDate > World.MaxSupportedDate)
				return;

			if (triggerDate == long.MinValue)
				return;

			Group.World.InsertEventToHappen (new SailingDiscoveryEvent (Group, triggerDate));
		}
	}

	protected override int CalculateAsymptoteInternal (CulturalDiscovery discovery) {
		
		switch (discovery.Id) {

		case BoatMakingDiscovery.BoatMakingDiscoveryId:
			return 1000;
		case SailingDiscovery.SailingDiscoveryId:
			return 3000;
		}

		return 0;
	}

	public override float CalculateExpectedProgressLevel () {
		
		if (_neighborhoodOceanPresence <= 0)
			return 1;

		return Mathf.Clamp (ProgressLevel / _neighborhoodOceanPresence, MinProgressLevel, 1);
	}

	public override float CalculateTransferFactor () {
		
		return (_neighborhoodOceanPresence * 0.9f) + 0.1f;
	}

	public override bool WillBeLost () {

		if (Value < 100) {
			return !Group.InfluencingPolityHasKnowledge (Id);
		}

		return false;
	}

	public override void LossConsequences () {
		
		Profiler.BeginSample ("BoatMakingDiscoveryEvent.CanSpawnIn");

		if (BoatMakingDiscoveryEvent.CanSpawnIn (Group)) {

			Profiler.BeginSample ("BoatMakingDiscoveryEvent.CalculateTriggerDate");

			long triggerDate = BoatMakingDiscoveryEvent.CalculateTriggerDate (Group);

			Profiler.EndSample ();

			if ((triggerDate <= World.MaxSupportedDate) && (triggerDate > long.MinValue)) {

				Profiler.BeginSample ("InsertEventToHappen: BoatMakingDiscoveryEvent");

				Group.World.InsertEventToHappen (new BoatMakingDiscoveryEvent (Group, triggerDate));

				Profiler.EndSample ();
			}
		}

		Profiler.EndSample ();
	}

	protected override int CalculateBaseAsymptote () {
		
		return 0;
	}
}

public class AgricultureKnowledge : CellCulturalKnowledge {

	public const string AgricultureKnowledgeId = "AgricultureKnowledge";
	public const string AgricultureKnowledgeName = "Agriculture";

	public const int AgricultureKnowledgeRngOffset = 1;

	public const float TimeEffectConstant = CellGroup.GenerationSpan * 2000;
	public const float TerrainFactorModifier = 1.5f;
	public const float MinAccesibility = 0.2f;

	public static int HighestAsymptote = 0;

	private float _terrainFactor;

	public AgricultureKnowledge () {

		if (Asymptote > HighestAsymptote) {

			HighestAsymptote = Asymptote;
		}
	}

	public AgricultureKnowledge (CellGroup group, int value = 100) : base (group, AgricultureKnowledgeId, AgricultureKnowledgeName, AgricultureKnowledgeRngOffset, value) {

		CalculateTerrainFactor ();
	}

	public AgricultureKnowledge (CellGroup group, AgricultureKnowledge baseKnowledge) : base (group, baseKnowledge.Id, baseKnowledge.Name, AgricultureKnowledgeRngOffset, baseKnowledge.Value, baseKnowledge.Asymptote) {

		CalculateTerrainFactor ();
	}

	public AgricultureKnowledge (CellGroup group, AgricultureKnowledge baseKnowledge, int initialValue) : base (group, baseKnowledge.Id, baseKnowledge.Name, AgricultureKnowledgeRngOffset, initialValue) {

		CalculateTerrainFactor ();
	}

	public AgricultureKnowledge (CellGroup group, CulturalKnowledge baseKnowledge, int initialValue) : base (group, baseKnowledge.Id, baseKnowledge.Name, AgricultureKnowledgeRngOffset, initialValue) {

		CalculateTerrainFactor ();
	}

	public static bool IsAgricultureKnowledge (CulturalKnowledge knowledge) {

		return knowledge.Id.Contains (AgricultureKnowledgeId);
	}

	public override void FinalizeLoad () {

		base.FinalizeLoad ();

		CalculateTerrainFactor ();
	}

	public void CalculateTerrainFactor () {

		_terrainFactor = CalculateTerrainFactorIn (Group.Cell);
	}

	public static float CalculateTerrainFactorIn (TerrainCell cell) {

		float accesibilityFactor = (cell.Accessibility - MinAccesibility) / (1f - MinAccesibility);

		return Mathf.Clamp01 (cell.Arability * cell.Accessibility * accesibilityFactor);
	}

	protected override void UpdateInternal (long timeSpan) {

		UpdateValueInternal (timeSpan, TimeEffectConstant, _terrainFactor * TerrainFactorModifier);
	}

	public override void PolityCulturalInfluence (CulturalKnowledge polityKnowledge, PolityInfluence polityInfluence, long timeSpan) {

		PolityCulturalInfluenceInternal (polityKnowledge, polityInfluence, timeSpan, TimeEffectConstant);
	}

	protected override int CalculateAsymptoteInternal (CulturalDiscovery discovery) {
		
		switch (discovery.Id) {

		case PlantCultivationDiscovery.PlantCultivationDiscoveryId:
			return 1000;
		}

		return 0;
	}

	public override float CalculateExpectedProgressLevel () {
		
		if (_terrainFactor <= 0)
			return 1;

		return Mathf.Clamp (ProgressLevel / _terrainFactor, MinProgressLevel, 1);
	}

	public override float CalculateTransferFactor () {
		
		return (_terrainFactor * 0.9f) + 0.1f;
	}

	public override bool WillBeLost () {
		
		if (Value < 100) {
			return !Group.InfluencingPolityHasKnowledge (Id);
		}

		return false;
	}

	public override void LossConsequences () {
		
		Profiler.BeginSample ("RemoveActivity: FarmingActivity");

		Group.Culture.RemoveActivity (CellCulturalActivity.FarmingActivityId);

		Profiler.EndSample ();

		Profiler.BeginSample ("PlantCultivationDiscoveryEvent.CanSpawnIn");

		if (PlantCultivationDiscoveryEvent.CanSpawnIn (Group)) {

			Profiler.BeginSample ("PlantCultivationDiscoveryEvent.CalculateTriggerDate");

			long triggerDate = PlantCultivationDiscoveryEvent.CalculateTriggerDate (Group);

			Profiler.EndSample ();

			if ((triggerDate <= World.MaxSupportedDate) && (triggerDate > int.MinValue)) {

				Profiler.BeginSample ("new PlantCultivationDiscoveryEvent");

				PlantCultivationDiscoveryEvent plantCultivationDiscoveryEvent = new PlantCultivationDiscoveryEvent (Group, triggerDate);

				Profiler.EndSample ();

				Profiler.BeginSample ("InsertEventToHappen: PlantCultivationDiscoveryEvent");

				Group.World.InsertEventToHappen (plantCultivationDiscoveryEvent);

				Profiler.EndSample ();
			}
		}

		Profiler.EndSample ();

		Group.Cell.FarmlandPercentage = 0;
	}

	protected override int CalculateBaseAsymptote () {
		
		return 0;
	}
}

public class SocialOrganizationKnowledge : CellCulturalKnowledge {

	public const string SocialOrganizationKnowledgeId = "SocialOrganizationKnowledge";
	public const string SocialOrganizationKnowledgeName = "Social Organization";

	public const int SocialOrganizationKnowledgeRngOffset = 2;

	public const int StartValue = 100;
	public const int MinValueForTribalismDiscovery = 500;
	public const int MinValueForHoldingTribalism = 200;
	public const int OptimalValueForTribalism = 10000;

	public const float TimeEffectConstant = CellGroup.GenerationSpan * 500;
	public const float PopulationDensityModifier = 10000f;

	public static int HighestAsymptote = 0;

	public SocialOrganizationKnowledge () {

		if (Asymptote > HighestAsymptote) {

			HighestAsymptote = Asymptote;
		}
	}

	public SocialOrganizationKnowledge (CellGroup group, int value = StartValue) : base (group, SocialOrganizationKnowledgeId, SocialOrganizationKnowledgeName, SocialOrganizationKnowledgeRngOffset, value) {

	}

	public SocialOrganizationKnowledge (CellGroup group, SocialOrganizationKnowledge baseKnowledge) : base (group, baseKnowledge.Id, baseKnowledge.Name, SocialOrganizationKnowledgeRngOffset, baseKnowledge.Value, baseKnowledge.Asymptote) {

	}

	public SocialOrganizationKnowledge (CellGroup group, SocialOrganizationKnowledge baseKnowledge, int initialValue) : base (group, baseKnowledge.Id, baseKnowledge.Name, SocialOrganizationKnowledgeRngOffset, initialValue) {

	}

	public SocialOrganizationKnowledge (CellGroup group, CulturalKnowledge baseKnowledge, int initialValue) : base (group, baseKnowledge.Id, baseKnowledge.Name, SocialOrganizationKnowledgeRngOffset, initialValue) {

	}

	public static bool IsSocialOrganizationKnowledge (CulturalKnowledge knowledge) {

		return knowledge.Id.Contains (SocialOrganizationKnowledgeId);
	}

	public override void FinalizeLoad () {

		base.FinalizeLoad ();
	}

	private float CalculatePopulationFactor () {

		float areaFactor = Group.Cell.Area / TerrainCell.MaxArea;

//		float popFactor = Group.Population * areaFactor;
		float popFactor = (float)Group.Population;

		float densityFactor = PopulationDensityModifier * Asymptote * ValueScaleFactor * areaFactor;

		float finalPopFactor = popFactor / (popFactor + densityFactor);
		finalPopFactor = 0.1f + finalPopFactor * 0.9f;

		return finalPopFactor;
	}

	private float CalculatePolityInfluenceFactor () {

		float totalInfluence = Group.TotalPolityInfluenceValue * 0.5f;

		return totalInfluence;
	}

	protected override void UpdateInternal (long timeSpan) {

		float populationFactor = CalculatePopulationFactor ();

		float influenceFactor = CalculatePolityInfluenceFactor ();

		float totalFactor = populationFactor + (influenceFactor * (1 - populationFactor));

		UpdateValueInternal (timeSpan, TimeEffectConstant, totalFactor);

		TryGenerateTribalismDiscoveryEvent ();
	}

	public override void PolityCulturalInfluence (CulturalKnowledge polityKnowledge, PolityInfluence polityInfluence, long timeSpan) {

		PolityCulturalInfluenceInternal (polityKnowledge, polityInfluence, timeSpan, TimeEffectConstant);

		#if DEBUG
		if (_newValue < SocialOrganizationKnowledge.MinValueForHoldingTribalism) {

			if (Group.GetFactionCores ().Count > 0) {
				Debug.LogWarning ("group with low social organization has faction cores - Id: " + Group.Id);
			}
		}
		#endif

		TryGenerateTribalismDiscoveryEvent ();
	}

	private void TryGenerateTribalismDiscoveryEvent () {

		if (Value < TribalismDiscoveryEvent.MinSocialOrganizationKnowledgeForTribalismDiscovery)
			return;

		if (Value > TribalismDiscoveryEvent.OptimalSocialOrganizationKnowledgeValue)
			return;

		if (TribalismDiscoveryEvent.CanSpawnIn (Group)) {

			long triggerDate = TribalismDiscoveryEvent.CalculateTriggerDate (Group);

			if (triggerDate == long.MinValue)
				return;

			Group.World.InsertEventToHappen (new TribalismDiscoveryEvent (Group, triggerDate));
		}
	}

	protected override int CalculateAsymptoteInternal (CulturalDiscovery discovery) {
		
		switch (discovery.Id) {

		case TribalismDiscovery.TribalismDiscoveryId:
			return OptimalValueForTribalism;
		}

		return 0;
	}

	public override float CalculateExpectedProgressLevel () {
		
		float populationFactor = CalculatePopulationFactor ();

		if (populationFactor <= 0)
			return 1;

		return Mathf.Clamp (ProgressLevel / populationFactor, MinProgressLevel, 1);
	}

	public override float CalculateTransferFactor () {
		
		float populationFactor = CalculatePopulationFactor ();

		return (populationFactor * 0.9f) + 0.1f;
	}

	public override bool WillBeLost () {
		
		return false;
	}

	public override void LossConsequences () {
		
	}

	protected override int CalculateBaseAsymptote () {
		
		return 1000;
	}
}