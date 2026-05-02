public static class TapperStationInteractionRules
{
	public static bool CanClaimStation( bool lobbyPhase, int playerStationIndex, bool stationOwned )
	{
		return lobbyPhase && playerStationIndex < 0 && !stationOwned;
	}

	public static bool CanUseClaimedStation( int playerStationIndex, int requestedStationIndex )
	{
		return playerStationIndex >= 0 && playerStationIndex == requestedStationIndex;
	}

	public static bool CanTapStation( bool playing, int playerStationIndex, int requestedStationIndex )
	{
		return playing && CanUseClaimedStation( playerStationIndex, requestedStationIndex );
	}
}
