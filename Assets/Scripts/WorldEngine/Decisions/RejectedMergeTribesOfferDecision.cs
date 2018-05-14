﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

public class RejectedMergeTribesOfferDecision : PolityDecision {

	private Tribe _sourceTribe;
	private Tribe _targetTribe;

	public RejectedMergeTribesOfferDecision (Tribe sourceTribe, Tribe targetTribe) : base (sourceTribe) {

		Description = "The leader of " + targetTribe.GetNameAndTypeStringBold () + ", " + targetTribe.CurrentLeader.Name.BoldText + ", has rejected the attempt from " + sourceTribe.GetNameAndTypeStringBold () + 
			" to merge the tribes into one";

		_targetTribe = targetTribe;
		_sourceTribe = sourceTribe;
	}

	private string GenerateRejectedOfferResultEffectsString () {

		return 
			"\t• " + GenerateResultEffectsString_DecreaseRelationship (_targetTribe, _sourceTribe) + "\n" + 
			"\t• " + GenerateResultEffectsString_IncreasePreference (_targetTribe, CulturalPreference.IsolationPreferenceId);
	}

	public static void TargetTribeRejectedOffer (Tribe sourceTribe, Tribe targetTribe) {

		sourceTribe.DominantFaction.SetToUpdate ();
		targetTribe.DominantFaction.SetToUpdate ();

		WorldEventMessage message = new RejectedMergeTribesOfferEventMessage (sourceTribe, targetTribe, targetTribe.CurrentLeader, sourceTribe.World.CurrentDate);

		sourceTribe.AddEventMessage (message);
		targetTribe.AddEventMessage (message);
	}

	private void RejectedOffer () {

		TargetTribeRejectedOffer (_sourceTribe, _targetTribe);
	}

	public override Option[] GetOptions () {

		return new Option[] {
			new Option ("Oh well...", "Effects:\n" + GenerateRejectedOfferResultEffectsString (), RejectedOffer)
		};
	}

	public override void ExecutePreferredOption ()
	{
		RejectedOffer ();
	}
}
	