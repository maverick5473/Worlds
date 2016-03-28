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

		float groupTotalInfluenceValue = 0.0001f;

		if (targetGroup != null) {

			groupTotalInfluenceValue = Mathf.Max (targetGroup.TotalPolityInfluenceValue, groupTotalInfluenceValue);
		}

		float influenceFactor = sourceInfluenceValue / (groupTotalInfluenceValue + sourceInfluenceValue);

		return Mathf.Clamp01 (influenceFactor);
	}

	public override void MergingEffects (CellGroup targetGroup, float sourceValue, float percentOfTarget) {

		foreach (PolityInfluence pInfluence in targetGroup.GetPolityInfluences ()) {

			float currentValue = pInfluence.Value;

			float newValue = currentValue * (1 - percentOfTarget);

			if (Id == pInfluence.PolityId) {
			
				newValue += sourceValue * percentOfTarget;
			}

			targetGroup.SetPolityInfluenceValue (pInfluence.Polity, newValue);
		}
	}

	public override void UpdateEffects (CellGroup group, float influence, int timeSpan) {

		if (group.Culture.GetDiscovery (TribalismDiscovery.TribalismDiscoveryId) == null) {
		
			group.SetPolityInfluenceValue (this, 0);

			return;
		}
	}
}