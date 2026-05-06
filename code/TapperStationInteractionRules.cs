using System;
using System.Collections.Generic;
using System.Linq;

public static class TapperStationInteractionRules
{
	public static bool CanClaimStation( bool lobbyPhase, int playerStationIndex, bool stationOwned )
	{
		return CanClaimStation( lobbyPhase, playerStationIndex, stationOwned, true );
	}

	public static bool CanClaimStation( bool lobbyPhase, int playerStationIndex, bool stationOwned, bool stationActive )
	{
		return stationActive && lobbyPhase && playerStationIndex < 0 && !stationOwned;
	}

	public static bool CanUseClaimedStation( int playerStationIndex, int requestedStationIndex )
	{
		return playerStationIndex >= 0 && playerStationIndex == requestedStationIndex;
	}

	public static bool CanTapStation( bool playing, int playerStationIndex, int requestedStationIndex )
	{
		return playing && CanUseClaimedStation( playerStationIndex, requestedStationIndex );
	}

	public static int ResolveDynamicStationCapacity( int playerCount, IEnumerable<int> claimedStationIndexes, int maxStations = 8 )
	{
		var safeMaxStations = Math.Max( 0, maxStations );
		var highestClaimedStation = (claimedStationIndexes ?? Array.Empty<int>())
			.Where( x => x >= 0 )
			.Select( x => x + 1 )
			.DefaultIfEmpty( 0 )
			.Max();

		return Math.Clamp( Math.Max( Math.Max( 0, playerCount ), highestClaimedStation ), 0, safeMaxStations );
	}

	public static bool IsStationActive( int stationIndex, IEnumerable<int> activeStationIndexes )
	{
		return stationIndex >= 0 && (activeStationIndexes ?? Array.Empty<int>()).Contains( stationIndex );
	}

	public static bool ShouldDropStationClaim( int playerStationIndex, IEnumerable<int> activeStationIndexes )
	{
		return playerStationIndex >= 0 && !IsStationActive( playerStationIndex, activeStationIndexes );
	}

	public static bool IsWithinClaimRange2D( float deltaX, float deltaY, float claimRange )
	{
		if ( claimRange < 0f )
			return false;

		return deltaX * deltaX + deltaY * deltaY <= claimRange * claimRange;
	}

	public static bool ShouldUnclaimStationOnExit( bool lobbyPhase, int playerStationIndex, bool withinClaimRange )
	{
		return lobbyPhase && playerStationIndex >= 0 && !withinClaimRange;
	}

	public static bool IsWithinStationBounds2D( float localX, float localY, float halfWidth, float halfDepth )
	{
		return Math.Abs( localX ) <= Math.Abs( halfWidth ) && Math.Abs( localY ) <= Math.Abs( halfDepth );
	}
}
