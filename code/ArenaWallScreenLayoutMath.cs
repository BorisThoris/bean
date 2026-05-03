using System;

public readonly struct ArenaWallScreenLayout
{
	public readonly float ScreenX;
	public readonly float ScreenY;
	public readonly float ScreenZ;
	public readonly float ScreenWidth;
	public readonly float ScreenHeight;
	public readonly float FacingX;
	public readonly float FacingY;
	public readonly float FacingZ;
	public readonly float UiX;
	public readonly float UiY;
	public readonly float UiZ;
	public readonly float UiOffset;
	public readonly float UiScale;
	public readonly int CssWidth;
	public readonly int CssHeight;

	public ArenaWallScreenLayout(
		float screenX,
		float screenY,
		float screenZ,
		float screenWidth,
		float screenHeight,
		float facingX,
		float facingY,
		float facingZ,
		float uiX,
		float uiY,
		float uiZ,
		float uiOffset,
		float uiScale,
		int cssWidth,
		int cssHeight )
	{
		ScreenX = screenX;
		ScreenY = screenY;
		ScreenZ = screenZ;
		ScreenWidth = screenWidth;
		ScreenHeight = screenHeight;
		FacingX = facingX;
		FacingY = facingY;
		FacingZ = facingZ;
		UiX = uiX;
		UiY = uiY;
		UiZ = uiZ;
		UiOffset = uiOffset;
		UiScale = uiScale;
		CssWidth = cssWidth;
		CssHeight = cssHeight;
	}
}

public static class ArenaWallScreenLayoutMath
{
	public const float ScreenInsetFromRearWall = 28f;
	public const float ScreenCenterY = 0f;
	public const float ScreenModelThickness = 30f;
	public const float WallEdgePadding = 20f;
	public const float WallVerticalPadding = 20f;
	public const float TargetZOffset = 180f;
	public const float UiForwardOffset = 48f;
	public const float WorldPanelWorldScale = 10f;

	public static ArenaWallScreenLayout Build( RuntimeRoomLayout roomLayout, float stageX = 0f, float stageY = 0f, float stageZ = 0f )
	{
		var screenX = roomLayout.RearWallX - ScreenInsetFromRearWall;
		var screenY = ScreenCenterY;
		var screenZ = roomLayout.WallHeight * 0.5f;
		var screenWidth = MathF.Max( 1f, roomLayout.FloorDepth - WallEdgePadding * 2f );
		var screenHeight = MathF.Max( 1f, roomLayout.WallHeight - WallVerticalPadding * 2f );
		var uiScale = WorldPanelWorldScale;
		var cssWidth = Math.Max( 1, (int)MathF.Round( screenWidth ) );
		var cssHeight = Math.Max( 1, (int)MathF.Round( screenHeight ) );

		var targetX = stageX;
		var targetY = stageY;
		var targetZ = stageZ + TargetZOffset;
		var facingX = targetX - screenX;
		var facingY = targetY - screenY;
		var facingZ = targetZ - screenZ;
		var facingLength = MathF.Sqrt( facingX * facingX + facingY * facingY + facingZ * facingZ );

		if ( facingLength <= 0.001f )
		{
			facingX = -1f;
			facingY = 0f;
			facingZ = 0f;
		}
		else
		{
			facingX /= facingLength;
			facingY /= facingLength;
			facingZ /= facingLength;
		}

		return new ArenaWallScreenLayout(
			screenX,
			screenY,
			screenZ,
			screenWidth,
			screenHeight,
			facingX,
			facingY,
			facingZ,
			screenX + facingX * UiForwardOffset,
			screenY + facingY * UiForwardOffset,
			screenZ + facingZ * UiForwardOffset,
			UiForwardOffset,
			uiScale,
			cssWidth,
			cssHeight );
	}

	public static bool ShouldShowFallback( bool wallScreenValid )
	{
		return !wallScreenValid;
	}

	public static bool IsDisplayFacingStage( ArenaWallScreenLayout layout, float displayForwardX, float displayForwardY, float displayForwardZ )
	{
		var dot = layout.FacingX * displayForwardX + layout.FacingY * displayForwardY + layout.FacingZ * displayForwardZ;
		return dot > 0.99f;
	}
}
