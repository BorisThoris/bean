using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class TapperStationInteractionRulesTests
{
	[TestMethod]
	public void UnclaimedPlayerCanClaimOpenStationInLobbyPhase()
	{
		Assert.IsTrue( TapperStationInteractionRules.CanClaimStation( true, -1, false ) );
	}

	[TestMethod]
	public void UnclaimedPlayerCannotClaimOwnedStation()
	{
		Assert.IsFalse( TapperStationInteractionRules.CanClaimStation( true, -1, true ) );
	}

	[TestMethod]
	public void ClaimedPlayerCannotClaimAnotherStation()
	{
		Assert.IsFalse( TapperStationInteractionRules.CanClaimStation( true, 1, false ) );
	}

	[TestMethod]
	public void PlayerCanUseOnlyClaimedStation()
	{
		Assert.IsTrue( TapperStationInteractionRules.CanUseClaimedStation( 2, 2 ) );
		Assert.IsFalse( TapperStationInteractionRules.CanUseClaimedStation( 2, 1 ) );
	}

	[TestMethod]
	public void UnclaimedPlayerCannotTapDuringRace()
	{
		Assert.IsFalse( TapperStationInteractionRules.CanTapStation( true, -1, 0 ) );
	}

	[TestMethod]
	public void ClaimedPlayerCanTapOnlyOwnedStationDuringRace()
	{
		Assert.IsTrue( TapperStationInteractionRules.CanTapStation( true, 0, 0 ) );
		Assert.IsFalse( TapperStationInteractionRules.CanTapStation( true, 0, 1 ) );
	}
}
