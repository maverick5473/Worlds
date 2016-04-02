﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

public class Tribe : Polity {

	public const float BaseCoreInfluence = 0.5f;

	public Tribe () : base () {

	}

	private Tribe (CellGroup coreGroup, float coreGroupInfluence) : base (coreGroup, coreGroupInfluence) {

	}

	public static Tribe GenerateNewTribe (CellGroup coreGroup) {

		float randomValue = coreGroup.Cell.GetNextLocalRandomFloat ();
		float coreInfluence = BaseCoreInfluence + randomValue * (1 - BaseCoreInfluence);
	
		Tribe newTribe = new Tribe (coreGroup, coreInfluence);

		return newTribe;
	}

	public override float MigrationValue (TerrainCell targetCell, float sourceInfluenceValue)
	{
		sourceInfluenceValue = Mathf.Max (sourceInfluenceValue, 0);

		CellGroup targetGroup = targetCell.Group;

		float groupTotalInfluenceValue = 0;

//		float popFactor = 0;
		float socialOrgFactor = 0;
//		float socialOrgFactor = 1;

		if (targetGroup != null) {

//			float areaFactor = targetCell.Area / TerrainCell.MaxArea;
//
//			float densityFactor = SocialOrganizationKnowledge.PopulationDensityModifier * areaFactor / 10;
//
//			popFactor = areaFactor * targetGroup.Population / (targetGroup.Population + densityFactor);

			CulturalKnowledge socialOrgKnowledge = targetGroup.Culture.GetKnowledge (SocialOrganizationKnowledge.SocialOrganizationKnowledgeId);

			socialOrgFactor = Mathf.Clamp01 (socialOrgKnowledge.Value / SocialOrganizationKnowledge.MinKnowledgeValueForTribalism);
			socialOrgFactor = 1 - Mathf.Pow (1 - socialOrgFactor, 2);

			groupTotalInfluenceValue = targetGroup.TotalPolityInfluenceValue;
		}

		float sourceInfluenceFactor = 0.05f + (sourceInfluenceValue * 0.95f);

		//float influenceFactor = popFactor * sourceInfluenceValue / (groupTotalInfluenceValue + sourceInfluenceFactor);
		float influenceFactor = socialOrgFactor * sourceInfluenceValue / (groupTotalInfluenceValue + sourceInfluenceFactor);

		return Mathf.Clamp01 (influenceFactor);
	}

	public override void MergingEffects (CellGroup targetGroup, float sourceValue, float percentOfTarget) {

		foreach (PolityInfluence pInfluence in targetGroup.GetPolityInfluences ()) {

			float influenceValue = pInfluence.Value;

			float newInfluenceValue = influenceValue * (1 - percentOfTarget);

			targetGroup.SetPolityInfluenceValue (pInfluence.Polity, newInfluenceValue);
		}

		#if DEBUG
		if (sourceValue > 0.5f) {
		
			bool debug = true;
		}
		#endif

		float currentValue = targetGroup.GetPolityInfluenceValue (this);

		float newValue = currentValue + (sourceValue * percentOfTarget);

		#if DEBUG
		if (targetGroup.Cell.IsSelected) {
		
			bool debug = true;
		}
		#endif

		targetGroup.SetPolityInfluenceValue (this, newValue);
	}

	public override void UpdateEffects (CellGroup group, float influence, int timeSpan) {

		if (group.Culture.GetDiscovery (TribalismDiscovery.TribalismDiscoveryId) == null) {
		
			group.SetPolityInfluenceValue (this, 0);

			return;
		}
	}
}
