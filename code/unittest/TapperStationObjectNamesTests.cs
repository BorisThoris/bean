using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class TapperStationObjectNamesTests
{
	[TestMethod]
	public void TryParseStationIndex_ReadsPhysicalTapButtonNames()
	{
		var parsed = TapperStationObjectNames.TryParseStationIndex( "Station 0 Physical Tap Button", out var stationIndex );

		Assert.IsTrue( parsed );
		Assert.AreEqual( 0, stationIndex );
	}

	[TestMethod]
	public void TryParseStationIndex_ReadsButtonHitboxNames()
	{
		var parsed = TapperStationObjectNames.TryParseStationIndex( "Station 7 Button Hitbox", out var stationIndex );

		Assert.IsTrue( parsed );
		Assert.AreEqual( 7, stationIndex );
	}

	[TestMethod]
	public void TryParseStationIndex_ReadsClaimFrameNames()
	{
		foreach ( var name in new[]
		{
			"Station 2 Claim Frame Front",
			"Station 2 Claim Frame Back",
			"Station 2 Claim Frame Left",
			"Station 2 Claim Frame Right"
		} )
		{
			var parsed = TapperStationObjectNames.TryParseStationIndex( name, out var stationIndex );

			Assert.IsTrue( parsed, name );
			Assert.AreEqual( 2, stationIndex, name );
		}
	}

	[TestMethod]
	public void TryParseStationIndex_RejectsInvalidNames()
	{
		var invalidNames = new[]
		{
			null,
			"",
			"Station Physical Tap Button",
			"Station A Button Hitbox",
			"Station A Claim Frame Front",
			"Station 2 Play Platform",
			"Not Station 2 Button Hitbox"
		};

		foreach ( var name in invalidNames )
		{
			var parsed = TapperStationObjectNames.TryParseStationIndex( name, out var stationIndex );

			Assert.IsFalse( parsed, name );
			Assert.AreEqual( -1, stationIndex, name );
		}
	}
}
