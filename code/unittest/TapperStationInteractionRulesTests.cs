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
	public void UnclaimedPlayerCannotClaimInactiveStation()
	{
		Assert.IsFalse( TapperStationInteractionRules.CanClaimStation( true, -1, false, false ) );
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

	[TestMethod]
	public void DynamicStationCapacityScalesWithPlayers()
	{
		Assert.AreEqual( 0, TapperStationInteractionRules.ResolveDynamicStationCapacity( 0, [] ) );
		Assert.AreEqual( 1, TapperStationInteractionRules.ResolveDynamicStationCapacity( 1, [] ) );
		Assert.AreEqual( 4, TapperStationInteractionRules.ResolveDynamicStationCapacity( 4, [] ) );
		Assert.AreEqual( 8, TapperStationInteractionRules.ResolveDynamicStationCapacity( 12, [] ) );
	}

	[TestMethod]
	public void DynamicStationCapacityPreservesHighestClaim()
	{
		Assert.AreEqual( 4, TapperStationInteractionRules.ResolveDynamicStationCapacity( 1, [3] ) );
		Assert.AreEqual( 6, TapperStationInteractionRules.ResolveDynamicStationCapacity( 2, [-1, 5] ) );
		Assert.AreEqual( 8, TapperStationInteractionRules.ResolveDynamicStationCapacity( 2, [12] ) );
	}

	[TestMethod]
	public void StationActiveRequiresNonNegativeIndexInActiveSet()
	{
		Assert.IsTrue( TapperStationInteractionRules.IsStationActive( 2, [0, 2, 4] ) );
		Assert.IsFalse( TapperStationInteractionRules.IsStationActive( 3, [0, 2, 4] ) );
		Assert.IsFalse( TapperStationInteractionRules.IsStationActive( -1, [0, 2, 4] ) );
	}

	[TestMethod]
	public void InvalidClaimsAreDroppedOnlyWhenStationIsInactive()
	{
		Assert.IsFalse( TapperStationInteractionRules.ShouldDropStationClaim( -1, [0, 1] ) );
		Assert.IsFalse( TapperStationInteractionRules.ShouldDropStationClaim( 1, [0, 1] ) );
		Assert.IsTrue( TapperStationInteractionRules.ShouldDropStationClaim( 3, [0, 1] ) );
	}

	[TestMethod]
	public void ClaimRangeUsesHorizontalDistance()
	{
		Assert.IsTrue( TapperStationInteractionRules.IsWithinClaimRange2D( 120f, 90f, 190f ) );
		Assert.IsFalse( TapperStationInteractionRules.IsWithinClaimRange2D( 200f, 0f, 190f ) );
		Assert.IsFalse( TapperStationInteractionRules.IsWithinClaimRange2D( 0f, 0f, -1f ) );
	}

	[TestMethod]
	public void ClaimedPlayerUnclaimsWhenLeavingBoundsInLobbyPhase()
	{
		Assert.IsTrue( TapperStationInteractionRules.ShouldUnclaimStationOnExit( true, 0, false ) );
		Assert.IsFalse( TapperStationInteractionRules.ShouldUnclaimStationOnExit( true, 0, true ) );
		Assert.IsFalse( TapperStationInteractionRules.ShouldUnclaimStationOnExit( true, -1, false ) );
		Assert.IsFalse( TapperStationInteractionRules.ShouldUnclaimStationOnExit( false, 0, false ) );
	}

	[TestMethod]
	public void StationBoundsUseLocalRectangle()
	{
		Assert.IsTrue( TapperStationInteractionRules.IsWithinStationBounds2D( 0f, 0f, 220f, 310f ) );
		Assert.IsTrue( TapperStationInteractionRules.IsWithinStationBounds2D( 220f, -310f, 220f, 310f ) );
		Assert.IsFalse( TapperStationInteractionRules.IsWithinStationBounds2D( 221f, 0f, 220f, 310f ) );
		Assert.IsFalse( TapperStationInteractionRules.IsWithinStationBounds2D( 0f, -311f, 220f, 310f ) );
	}

	[TestMethod]
	public void StationBoundsNormalizeNegativeExtents()
	{
		Assert.IsTrue( TapperStationInteractionRules.IsWithinStationBounds2D( 10f, 20f, -220f, -310f ) );
	}
}
