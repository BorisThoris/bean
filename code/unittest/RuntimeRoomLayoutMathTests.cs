using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class RuntimeRoomLayoutMathTests
{
	[TestMethod]
	public void RuntimeRoomDefaultsKeepFourStationsInsideFloorMargins()
	{
		var layout = RuntimeRoomLayoutMath.Build( 4 );

		Assert.AreEqual( 4, layout.StationCount );
		Assert.AreEqual( 1080f, layout.StationSpanY, 0.001f );
		Assert.IsGreaterThanOrEqualTo( RuntimeRoomLayoutMath.MinimumFloorDepth, layout.FloorDepth );
		Assert.IsGreaterThanOrEqualTo( layout.LeftWallY + 300f, layout.StationY( 0 ) );
		Assert.IsLessThan( layout.RightWallY - 300f, layout.StationY( 3 ) );
	}

	[TestMethod]
	public void RuntimeRoomExpandsDepthForEightStations()
	{
		var layout = RuntimeRoomLayoutMath.Build( 8 );

		Assert.AreEqual( 8, layout.StationCount );
		Assert.AreEqual( 2520f, layout.StationSpanY, 0.001f );
		Assert.AreEqual( 3420f, layout.FloorDepth, 0.001f );
		Assert.AreEqual( -1260f, layout.StationY( 0 ), 0.001f );
		Assert.AreEqual( 1260f, layout.StationY( 7 ), 0.001f );
	}

	[TestMethod]
	public void RuntimeRoomClampsStationCount()
	{
		Assert.AreEqual( 1, RuntimeRoomLayoutMath.Build( -4 ).StationCount );
		Assert.AreEqual( 8, RuntimeRoomLayoutMath.Build( 99 ).StationCount );
	}

	[TestMethod]
	public void StationCapacityUsesDefaultUntilLobbyExceedsIt()
	{
		Assert.AreEqual( 4, RuntimeRoomLayoutMath.ResolveStationCapacity( 4, 0 ) );
		Assert.AreEqual( 4, RuntimeRoomLayoutMath.ResolveStationCapacity( 4, 3 ) );
		Assert.AreEqual( 5, RuntimeRoomLayoutMath.ResolveStationCapacity( 4, 5 ) );
		Assert.AreEqual( 8, RuntimeRoomLayoutMath.ResolveStationCapacity( 4, 12 ) );
	}

	[TestMethod]
	public void RuntimeRoomProvidesFullEnclosureBounds()
	{
		var layout = RuntimeRoomLayoutMath.Build( 4 );

		Assert.AreEqual( layout.RearWallX - layout.FloorWidth, RuntimeRoomLayoutMath.FrontWallX( layout ), 0.001f );
		Assert.AreEqual( layout.RearWallX - layout.FloorWidth * 0.5f, RuntimeRoomLayoutMath.RoomCenterX( layout ), 0.001f );
		Assert.AreEqual( 0f, RuntimeRoomLayoutMath.RoomCenterY( layout ), 0.001f );
		Assert.IsLessThan( 0f, RuntimeRoomLayoutMath.FrontWallX( layout ) );
		Assert.IsGreaterThan( 0f, layout.RearWallX );
		Assert.AreEqual( layout.FloorWidth, layout.RearWallX - RuntimeRoomLayoutMath.FrontWallX( layout ), 0.001f );
		Assert.AreEqual( layout.FloorDepth, layout.RightWallY - layout.LeftWallY, 0.001f );
		Assert.AreEqual( layout.CeilingHeight + 200f, RuntimeRoomLayoutMath.EffectiveCeilingHeight( layout, layout.CeilingHeight + 200f ), 0.001f );
		Assert.AreEqual( layout.CeilingHeight, RuntimeRoomLayoutMath.EffectiveCeilingHeight( layout, layout.CeilingHeight - 100f ), 0.001f );
	}

	[TestMethod]
	public void SegmentLayoutUsesMidpointLengthAndMajorAxis()
	{
		var horizontal = RuntimeRoomLayoutMath.BuildSegment( -10f, 20f, 5f, 30f, 20f, 5f );
		Assert.AreEqual( 10f, horizontal.CenterX, 0.001f );
		Assert.AreEqual( 20f, horizontal.CenterY, 0.001f );
		Assert.AreEqual( 5f, horizontal.CenterZ, 0.001f );
		Assert.AreEqual( 40f, horizontal.Length, 0.001f );
		Assert.AreEqual( 0, horizontal.MajorAxis );

		var vertical = RuntimeRoomLayoutMath.BuildSegment( 0f, 0f, 12f, 0f, 0f, 112f );
		Assert.AreEqual( 62f, vertical.CenterZ, 0.001f );
		Assert.AreEqual( 100f, vertical.Length, 0.001f );
		Assert.AreEqual( 2, vertical.MajorAxis );
	}

	[TestMethod]
	public void SafeScalingFallsBackForTinyModelAxis()
	{
		Assert.AreEqual( 50f, RuntimeRoomLayoutMath.SafeAxisSize( 0f, 50f ), 0.001f );
		Assert.AreEqual( 2f, RuntimeRoomLayoutMath.ScaleForDesiredSize( 0f, 100f, 50f ), 0.001f );
		Assert.AreEqual( 4f, RuntimeRoomLayoutMath.ScaleForDesiredSize( 25f, 100f, 50f ), 0.001f );
	}
}
