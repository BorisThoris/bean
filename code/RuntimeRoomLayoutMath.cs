using System;

public readonly struct RuntimeRoomLayout
{
	public readonly int StationCount;
	public readonly float StationSpacing;
	public readonly float StationSpanY;
	public readonly float FloorWidth;
	public readonly float FloorDepth;
	public readonly float FloorThickness;
	public readonly float WallHeight;
	public readonly float CeilingHeight;
	public readonly float LeftWallY;
	public readonly float RightWallY;
	public readonly float RearWallX;

	public RuntimeRoomLayout(
		int stationCount,
		float stationSpacing,
		float stationSpanY,
		float floorWidth,
		float floorDepth,
		float floorThickness,
		float wallHeight,
		float ceilingHeight,
		float leftWallY,
		float rightWallY,
		float rearWallX )
	{
		StationCount = stationCount;
		StationSpacing = stationSpacing;
		StationSpanY = stationSpanY;
		FloorWidth = floorWidth;
		FloorDepth = floorDepth;
		FloorThickness = floorThickness;
		WallHeight = wallHeight;
		CeilingHeight = ceilingHeight;
		LeftWallY = leftWallY;
		RightWallY = rightWallY;
		RearWallX = rearWallX;
	}

	public float StationY( int index )
	{
		return -StationSpanY * 0.5f + index * StationSpacing;
	}
}

public readonly struct RuntimeSegmentLayout
{
	public readonly float CenterX;
	public readonly float CenterY;
	public readonly float CenterZ;
	public readonly float Length;
	public readonly int MajorAxis;

	public RuntimeSegmentLayout( float centerX, float centerY, float centerZ, float length, int majorAxis )
	{
		CenterX = centerX;
		CenterY = centerY;
		CenterZ = centerZ;
		Length = length;
		MajorAxis = majorAxis;
	}
}

public static class RuntimeRoomLayoutMath
{
	public const float StationSpacing = 360f;
	public const float StationMargin = 450f;
	public const float MinimumFloorWidth = 2600f;
	public const float MinimumFloorDepth = 2300f;
	public const float FloorThickness = 22f;
	public const float WallHeight = 640f;
	public const float CeilingHeight = 680f;
	public const float RearWallX = 650f;

	public static RuntimeRoomLayout Build( int stationCount )
	{
		var count = Math.Clamp( stationCount, 1, 8 );
		var stationSpanY = (count - 1) * StationSpacing;
		var floorDepth = Math.Max( MinimumFloorDepth, stationSpanY + StationMargin * 2f );
		var floorWidth = MinimumFloorWidth;
		var halfDepth = floorDepth * 0.5f;

		return new RuntimeRoomLayout(
			count,
			StationSpacing,
			stationSpanY,
			floorWidth,
			floorDepth,
			FloorThickness,
			WallHeight,
			CeilingHeight,
			-halfDepth,
			halfDepth,
			RearWallX );
	}

	public static int ResolveStationCapacity( int defaultStationCount, int playerCount )
	{
		return Math.Clamp( Math.Max( defaultStationCount, playerCount ), 1, 8 );
	}

	public static float FrontWallX( RuntimeRoomLayout layout )
	{
		return layout.RearWallX - layout.FloorWidth;
	}

	public static float RoomCenterX( RuntimeRoomLayout layout )
	{
		return (layout.RearWallX + FrontWallX( layout )) * 0.5f;
	}

	public static float RoomCenterY( RuntimeRoomLayout layout )
	{
		return (layout.LeftWallY + layout.RightWallY) * 0.5f;
	}

	public static float EffectiveCeilingHeight( RuntimeRoomLayout layout, float minimumClearanceHeight )
	{
		return Math.Max( layout.CeilingHeight, minimumClearanceHeight );
	}

	public static float SafeAxisSize( float value, float fallback )
	{
		return Math.Abs( value ) > 0.001f ? Math.Abs( value ) : fallback;
	}

	public static float ScaleForDesiredSize( float modelSize, float desiredSize, float fallback )
	{
		return Math.Abs( desiredSize ) / SafeAxisSize( modelSize, fallback );
	}

	public static RuntimeSegmentLayout BuildSegment( float startX, float startY, float startZ, float endX, float endY, float endZ )
	{
		var deltaX = endX - startX;
		var deltaY = endY - startY;
		var deltaZ = endZ - startZ;
		var length = MathF.Sqrt( deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ );
		var absX = Math.Abs( deltaX );
		var absY = Math.Abs( deltaY );
		var absZ = Math.Abs( deltaZ );
		var majorAxis = absX >= absY && absX >= absZ ? 0 : absY >= absZ ? 1 : 2;

		return new RuntimeSegmentLayout(
			(startX + endX) * 0.5f,
			(startY + endY) * 0.5f,
			(startZ + endZ) * 0.5f,
			length,
			majorAxis );
	}
}
