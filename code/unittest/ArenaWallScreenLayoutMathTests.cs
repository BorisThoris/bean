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
		Assert.AreEqual( ArenaWallScreenLayoutMath.ScreenCenterZ, layout.ScreenZ, 0.001f );
		AssertFacingIsNormalized( layout );
		Assert.AreEqual( -1f, MathF.Sign( layout.FacingX ) );
		Assert.AreEqual( 0f, layout.FacingY, 0.001f );
		Assert.AreEqual( layout.ScreenX + layout.FacingX * ArenaWallScreenLayoutMath.UiForwardOffset, layout.UiX, 0.001f );
		Assert.AreEqual( layout.ScreenY + layout.FacingY * ArenaWallScreenLayoutMath.UiForwardOffset, layout.UiY, 0.001f );
		Assert.AreEqual( layout.ScreenZ + layout.FacingZ * ArenaWallScreenLayoutMath.UiForwardOffset, layout.UiZ, 0.001f );
		Assert.AreEqual( ArenaWallScreenLayoutMath.UiForwardOffset, DistanceFromScreenToUi( layout ), 0.001f );
	}

	[TestMethod]
	public void ScreenWidthTracksRoomDepthButClampsToMaximum()
	{
		var defaultLayout = ArenaWallScreenLayoutMath.Build( RuntimeRoomLayoutMath.Build( 4 ) );
		var eightPlayerLayout = ArenaWallScreenLayoutMath.Build( RuntimeRoomLayoutMath.Build( 8 ) );

		Assert.AreEqual( RuntimeRoomLayoutMath.MinimumFloorDepth - ArenaWallScreenLayoutMath.ScreenWidthMargin, defaultLayout.ScreenWidth, 0.001f );
		Assert.AreEqual( ArenaWallScreenLayoutMath.ScreenMaxWidth, eightPlayerLayout.ScreenWidth, 0.001f );
		Assert.AreEqual( ArenaWallScreenLayoutMath.ScreenHeight, defaultLayout.ScreenHeight, 0.001f );
		Assert.AreEqual( ArenaWallScreenLayoutMath.ScreenHeight, eightPlayerLayout.ScreenHeight, 0.001f );
	}

	[TestMethod]
	public void UiScaleAndCssDimensionsStayInSyncWithRazorStyles()
	{
		var layout = ArenaWallScreenLayoutMath.Build( RuntimeRoomLayoutMath.Build( 4 ) );

		Assert.AreEqual( 0.75f, layout.UiScale, 0.001f );
		Assert.AreEqual( 2200, layout.CssWidth );
		Assert.AreEqual( 900, layout.CssHeight );
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
