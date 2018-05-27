using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

public class HandleFosterTribeRelationAttemptDecision : PolityDecision {

	public const float BaseMinPreferencePercentChange = 0.15f;
	public const float BaseMaxPreferencePercentChange = 0.30f;

	public const float BaseMinRelationshipPercentChange = 0.05f;
	public const float BaseMaxRelationshipPercentChange = 0.15f;

	public const float BaseMinInfluencePercentChange = 0.05f;
	public const float BaseMaxInfluencePercentChange = 0.15f;

	private bool _acceptOffer = true;

	private Tribe _targetTribe;
	private Tribe _sourceTribe;

	private static string GenerateDescriptionIntro (Tribe sourceTribe, Tribe targetTribe) {

		return 
			sourceTribe.CurrentLeader.Name.BoldText + ", leader of " + sourceTribe.GetNameAndTypeStringBold () + ", is trying to improve the relationship with " + 
			targetTribe.GetNameAndTypeStringBold () + ".\n\n";
	}

	public HandleFosterTribeRelationAttemptDecision (Tribe sourceTribe, Tribe targetTribe, bool acceptOffer) : base (sourceTribe) {

		_sourceTribe = sourceTribe;
		_targetTribe = targetTribe;

		Description = GenerateDescriptionIntro (sourceTribe, targetTribe) +
			"Should the leader of " + _targetTribe.GetNameAndTypeStringBold () + ", " + _targetTribe.CurrentLeader.Name.BoldText + ", reciprocate such attempts?";

		_acceptOffer = acceptOffer;
	}

	private string GenerateRejectOfferEffectsMessage () {

		return 
			"\t• " + GenerateEffectsString_IncreasePreference (_targetTribe, CulturalPreference.IsolationPreferenceId, BaseMinPreferencePercentChange, BaseMaxPreferencePercentChange) + "\n" +
			"\t• " + GenerateEffectsString_DecreaseRelationship (_targetTribe, _sourceTribe, BaseMinRelationshipPercentChange, BaseMaxRelationshipPercentChange);
	}

	public static void LeaderRejectsOffer_notifySourceTribe (Tribe sourceTribe, Tribe targetTribe) {

		#if DEBUG
		if (targetTribe.Id == 6993753500213400) {
			bool debug = true;
		}
		#endif

		World world = targetTribe.World;

		Clan sourceDominantClan = sourceTribe.DominantFaction as Clan;

		if (sourceTribe.IsUnderPlayerFocus || sourceDominantClan.IsUnderPlayerGuidance) {

			Decision decision = new RejectedFosterTribeRelationDecision (sourceTribe, targetTribe); // Notify player that tribe leader rejected offer

			if (sourceDominantClan.IsUnderPlayerGuidance) {

				world.AddDecisionToResolve (decision);

			} else {

				decision.ExecutePreferredOption ();
			}

		} else {

			RejectedFosterTribeRelationDecision.TargetTribeRejectedOffer (sourceTribe, targetTribe);
		}
	}

	public static void LeaderRejectsOffer (Tribe sourceTribe, Tribe targetTribe) {

		int rngOffset = RngOffsets.FOSTER_TRIBE_RELATION_EVENT_TARGETTRIBE_LEADER_REJECTS_OFFER_MODIFY_ATTRIBUTE;

		Effect_IncreasePreference (targetTribe, CulturalPreference.IsolationPreferenceId, BaseMinPreferencePercentChange, BaseMaxPreferencePercentChange, rngOffset++);
		Effect_DecreaseRelationship (targetTribe, sourceTribe, BaseMinRelationshipPercentChange, BaseMaxRelationshipPercentChange, rngOffset++);

		LeaderRejectsOffer_notifySourceTribe (sourceTribe, targetTribe);
	}

	private void RejectOffer () {

		LeaderRejectsOffer (_sourceTribe, _targetTribe);
	}

	private string GenerateAcceptOfferEffectsMessage () {

		return 
			"\t• " + GenerateEffectsString_DecreasePreference (_targetTribe, CulturalPreference.IsolationPreferenceId, BaseMinPreferencePercentChange, BaseMaxPreferencePercentChange) + "\n" +
			"\t• " + GenerateEffectsString_IncreaseRelationship (_targetTribe, _sourceTribe, BaseMinRelationshipPercentChange, BaseMaxRelationshipPercentChange);
	}

	public static void LeaderAcceptsOffer_notifySourceTribe (Tribe sourceTribe, Tribe targetTribe) {

		#if DEBUG
		if (targetTribe.Id == 6993753500213400) {
			bool debug = true;
		}
		#endif

		World world = sourceTribe.World;

		Clan sourceDominantClan = sourceTribe.DominantFaction as Clan;

		if (sourceTribe.IsUnderPlayerFocus || sourceDominantClan.IsUnderPlayerGuidance) {

			Decision decision = new AcceptedFosterTribeRelationDecision (sourceTribe, targetTribe); // Notify player that tribe leader acepted offer

			if (sourceDominantClan.IsUnderPlayerGuidance) {

				world.AddDecisionToResolve (decision);

			} else {

				decision.ExecutePreferredOption ();
			}

		} else {

			AcceptedFosterTribeRelationDecision.TargetTribeAcceptedOffer (sourceTribe, targetTribe);
		}
	}

	public static void LeaderAcceptsOffer (Tribe sourceTribe, Tribe targetTribe) {

		int rngOffset = RngOffsets.FOSTER_TRIBE_RELATION_EVENT_TARGETTRIBE_LEADER_ACCEPTS_OFFER_MODIFY_ATTRIBUTE;

		Effect_DecreasePreference (targetTribe, CulturalPreference.IsolationPreferenceId, BaseMinPreferencePercentChange, BaseMaxPreferencePercentChange, rngOffset++);
		Effect_IncreaseRelationship (targetTribe, sourceTribe, BaseMinRelationshipPercentChange, BaseMaxRelationshipPercentChange, rngOffset++);

		sourceTribe.DominantFaction.SetToUpdate ();
		targetTribe.DominantFaction.SetToUpdate ();

		LeaderAcceptsOffer_notifySourceTribe (sourceTribe, targetTribe);
	}

	private void AcceptOffer () {

		LeaderAcceptsOffer (_sourceTribe, _targetTribe);
	}

	public override Option[] GetOptions () {

		return new Option[] {
			new Option ("Accept offer...", "Effects:\n" + GenerateAcceptOfferEffectsMessage (), AcceptOffer),
			new Option ("Reject offer...", "Effects:\n" + GenerateRejectOfferEffectsMessage (), RejectOffer)
		};
	}

	public override void ExecutePreferredOption ()
	{
		if (_acceptOffer)
			AcceptOffer ();
		else
			RejectOffer ();
	}
}
	