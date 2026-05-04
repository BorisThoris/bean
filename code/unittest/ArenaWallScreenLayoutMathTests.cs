using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

[TestClass]
public sealed class ArenaWallScreenLayoutMathTests
{
	[TestMethod]
	public void DefaultRoomPlacesUiInFrontOfWallScreen()
	{
		var room = RuntimeRoomLayoutMath.Build( 4 );
		var layout = ArenaWallScreenLayoutMath.Build( room );

		Assert.AreEqual( room.RearWallX - ArenaWallScreenLayoutMath.ScreenInsetFromRearWall, layout.ScreenX, 0.001f );
		Assert.AreEqual( ArenaWallScreenLayoutMath.ScreenCenterY, layout.ScreenY, 0.001f );
		Assert.AreEqual( room.WallHeight * 0.5f, layout.ScreenZ, 0.001f );
		AssertFacingIsNormalized( layout );
		Assert.AreEqual( -1f, MathF.Sign( layout.FacingX ) );
		Assert.AreEqual( 0f, layout.FacingY, 0.001f );
		Assert.AreEqual( layout.ScreenX + layout.FacingX * ArenaWallScreenLayoutMath.UiForwardOffset, layout.UiX, 0.001f );
		Assert.AreEqual( layout.ScreenY + layout.FacingY * ArenaWallScreenLayoutMath.UiForwardOffset, layout.UiY, 0.001f );
		Assert.AreEqual( layout.ScreenZ + layout.FacingZ * ArenaWallScreenLayoutMath.UiForwardOffset, layout.UiZ, 0.001f );
		Assert.AreEqual( ArenaWallScreenLayoutMath.UiForwardOffset, DistanceFromScreenToUi( layout ), 0.001f );
	}

	[TestMethod]
	public void ScreenDimensionsFillRearWallWithClearance()
	{
		var room = RuntimeRoomLayoutMath.Build( 4 );
		var layout = ArenaWallScreenLayoutMath.Build( room );

		Assert.AreEqual( (room.FloorDepth - ArenaWallScreenLayoutMath.WallEdgePadding * 2f) * ArenaWallScreenLayoutMath.ScreenSizeMultiplier, layout.ScreenWidth, 0.001f );
		Assert.AreEqual( (room.WallHeight - ArenaWallScreenLayoutMath.WallVerticalPadding * 2f) * ArenaWallScreenLayoutMath.ScreenSizeMultiplier, layout.ScreenHeight, 0.001f );
		Assert.IsGreaterThan( room.FloorDepth * 0.98f, layout.ScreenWidth );
		Assert.IsGreaterThan( room.WallHeight * 0.93f, layout.ScreenHeight );
	}

	[TestMethod]
	public void ScreenWidthExpandsWithEightStationRoom()
	{
		var defaultRoom = RuntimeRoomLayoutMath.Build( 4 );
		var eightPlayerRoom = RuntimeRoomLayoutMath.Build( 8 );
		var defaultLayout = ArenaWallScreenLayoutMath.Build( defaultRoom );
		var eightPlayerLayout = ArenaWallScreenLayoutMath.Build( eightPlayerRoom );

		Assert.AreEqual( (defaultRoom.FloorDepth - ArenaWallScreenLayoutMath.WallEdgePadding * 2f) * ArenaWallScreenLayoutMath.ScreenSizeMultiplier, defaultLayout.ScreenWidth, 0.001f );
		Assert.AreEqual( (eightPlayerRoom.FloorDepth - ArenaWallScreenLayoutMath.WallEdgePadding * 2f) * ArenaWallScreenLayoutMath.ScreenSizeMultiplier, eightPlayerLayout.ScreenWidth, 0.001f );
		Assert.IsGreaterThan( defaultLayout.ScreenWidth, eightPlayerLayout.ScreenWidth );
	}

	[TestMethod]
	public void UiScaleAndCssDimensionsFillScreenSurface()
	{
		var layout = ArenaWallScreenLayoutMath.Build( RuntimeRoomLayoutMath.Build( 4 ) );

		Assert.AreEqual( ArenaWallScreenLayoutMath.WorldPanelWorldScale, layout.UiScale, 0.001f );
		Assert.AreEqual( (int)MathF.Round( layout.ScreenWidth ), layout.CssWidth );
		Assert.AreEqual( (int)MathF.Round( layout.ScreenHeight ), layout.CssHeight );
		Assert.AreEqual( layout.ScreenWidth, layout.CssWidth, 0.5f );
		Assert.AreEqual( layout.ScreenHeight, layout.CssHeight, 0.5f );
	}

	[TestMethod]
	public void FallbackOnlyShowsWhenRazorWallScreenIsInvalid()
	{
		Assert.IsFalse( ArenaWallScreenLayoutMath.ShouldShowFallback( true ) );
		Assert.IsTrue( ArenaWallScreenLayoutMath.ShouldShowFallback( false ) );
	}

	[TestMethod]
	public void DisplayForwardFacesStage()
	{
		var layout = ArenaWallScreenLayoutMath.Build( RuntimeRoomLayoutMath.Build( 4 ) );

		Assert.IsTrue( ArenaWallScreenLayoutMath.IsDisplayFacingStage( layout, layout.FacingX, layout.FacingY, layout.FacingZ ) );
		Assert.IsFalse( ArenaWallScreenLayoutMath.IsDisplayFacingStage( layout, -layout.FacingX, -layout.FacingY, -layout.FacingZ ) );
	}

	[TestMethod]
	public void StageOffsetStillPlacesUiTowardStageTarget()
	{
		var room = RuntimeRoomLayoutMath.Build( 4 );
		var layout = ArenaWallScreenLayoutMath.Build( room, stageX: 100f, stageY: 50f, stageZ: 20f );

		AssertFacingIsNormalized( layout );
		Assert.IsLessThan( 0f, layout.FacingX );
		Assert.IsGreaterThan( 0f, layout.FacingY );
		Assert.IsLessThan( layout.ScreenX, layout.UiX );
		Assert.IsGreaterThan( layout.ScreenY, layout.UiY );
		Assert.AreEqual( ArenaWallScreenLayoutMath.UiForwardOffset, DistanceFromScreenToUi( layout ), 0.001f );
	}

	private static void AssertFacingIsNormalized( ArenaWallScreenLayout layout )
	{
		var length = MathF.Sqrt( layout.FacingX * layout.FacingX + layout.FacingY * layout.FacingY + layout.FacingZ * layout.FacingZ );
		Assert.AreEqual( 1f, length, 0.001f );
	}

	private static float DistanceFromScreenToUi( ArenaWallScreenLayout layout )
	{
		var dx = layout.UiX - layout.ScreenX;
		var dy = layout.UiY - layout.ScreenY;
		var dz = layout.UiZ - layout.ScreenZ;
		return MathF.Sqrt( dx * dx + dy * dy + dz * dz );
	}
}
