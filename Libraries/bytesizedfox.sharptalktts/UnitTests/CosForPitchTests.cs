using SharpTalk;

namespace UnitTests;

[TestClass]
public sealed class CosForPitchTests
{
	[TestMethod]
	public void CosForPitch_MatchesLegacyTable_SpotChecks()
	{
		Assert.AreEqual( (short)8179, Tables.CosForPitch( 256 ) );
		Assert.AreEqual( (short)8179, Tables.CosForPitch( 257 ) );
		Assert.AreEqual( (short)8175, Tables.CosForPitch( 300 ) );
		Assert.AreEqual( (short)8170, Tables.CosForPitch( 356 ) );
		Assert.AreEqual( (short)8140, Tables.CosForPitch( 512 ) );
		Assert.AreEqual( (short)7984, Tables.CosForPitch( 768 ) );
		Assert.AreEqual( (short)7370, Tables.CosForPitch( 1024 ) );

		// Historically-off-by-one pitches (correction set)
		Assert.AreEqual( (short)7335, Tables.CosForPitch( 1032 ) );
		Assert.AreEqual( (short)4942, Tables.CosForPitch( 1288 ) );
		Assert.AreEqual( (short)4582, Tables.CosForPitch( 1309 ) );
		Assert.AreEqual( (short)4126, Tables.CosForPitch( 1333 ) );
		Assert.AreEqual( (short)1804, Tables.CosForPitch( 1428 ) );
		Assert.AreEqual( (short)217, Tables.CosForPitch( 1478 ) );
		Assert.AreEqual( (short)(-26), Tables.CosForPitch( 1485 ) );
		Assert.AreEqual( (short)(-560), Tables.CosForPitch( 1500 ) );
		Assert.AreEqual( (short)(-4754), Tables.CosForPitch( 1607 ) );
		Assert.AreEqual( (short)(-6190), Tables.CosForPitch( 1645 ) );
		Assert.AreEqual( (short)(-7890), Tables.CosForPitch( 1771 ) );

		Assert.AreEqual( (short)(-7331), Tables.CosForPitch( 1791 ) );
	}

	[TestMethod]
	public void CosForPitch_ClampsToValidRange()
	{
		Assert.AreEqual( Tables.CosForPitch( 256 ), Tables.CosForPitch( 0 ) );
		Assert.AreEqual( Tables.CosForPitch( 1791 ), Tables.CosForPitch( short.MaxValue ) );
	}
}

