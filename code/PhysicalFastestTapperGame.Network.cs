using System;
using System.Linq;

public sealed partial class PhysicalFastestTapperGame
{
	private static bool IsAuthoritativeInstance()
	{
		return !Networking.IsActive || Networking.IsHost;
	}

	private void PublishNetworkState()
	{
		if ( !Networking.IsActive || !Networking.IsHost )
			return;

		SyncedRoundState = (int)State;
		SyncedStateTimeLeft = StateTimeLeft;
		SyncedRoundTimeLeft = RoundTimeLeft;
		SyncedWinnerStation = LastWinnerStation;
		SyncedGameMode = (int)GameMode;
		SyncedTournamentRound = TournamentRound;
		SyncedEventPhase = (int)EventPhase;
		SyncedStationCapacity = CurrentGeneratedStationCount;
		SyncedResultOrder = string.Join( ",", GetOrderedResults().Select( x => x.StationIndex ) );

		SyncedScores.Clear();
		SyncedStations.Clear();
		SyncedReady.Clear();
		SyncedTournamentPoints.Clear();
		SyncedFocusHits.Clear();
		SyncedNames.Clear();

		foreach ( var player in Players )
		{
			var key = player.ConnectionKey ?? ConnectionKey( player.Connection );
			SyncedScores[key] = player.Score;
			SyncedStations[key] = player.StationIndex;
			SyncedReady[key] = player.Ready;
			SyncedTournamentPoints[key] = player.TournamentPoints;
			SyncedFocusHits[key] = player.FocusHits;
			SyncedNames[key] = string.IsNullOrWhiteSpace( player.Name ) ? "PLAYER" : player.Name;
		}
	}

	private void ApplySyncedRoundState()
	{
		if ( !Networking.IsActive || Networking.IsHost )
			return;

		State = (RoundState)SyncedRoundState;
		StateTimeLeft = SyncedStateTimeLeft;
		RoundTimeLeft = SyncedRoundTimeLeft;
		LastWinnerStation = SyncedWinnerStation;
		GameMode = (TapperGameMode)SyncedGameMode;
		TournamentRound = SyncedTournamentRound;
		EventPhase = (TapperEventPhase)SyncedEventPhase;
		ApplySyncedStationCapacity();
		ApplySyncedDisplayPlayers();
	}

	private void ApplySyncedStationCapacity()
	{
		var target = Math.Clamp( SyncedStationCapacity, 1, 8 );
		if ( target <= 0 || target == CurrentGeneratedStationCount )
			return;

		RebuildArenaForStationCapacity( target );
	}

	private void ApplySyncedDisplayPlayers()
	{
		var activeKeys = SyncedStations.Keys.ToHashSet();

		for ( var i = Players.Count - 1; i >= 0; i-- )
		{
			var player = Players[i];
			if ( activeKeys.Contains( player.ConnectionKey ) )
				continue;

			PlayersByConnection.Remove( player.ConnectionKey );
			Players.RemoveAt( i );
		}

		foreach ( var entry in SyncedStations )
		{
			if ( !PlayersByConnection.TryGetValue( entry.Key, out var player ) )
			{
				player = new PlayerScore
				{
					ConnectionKey = entry.Key,
					Name = $"STATION {entry.Value + 1}"
				};
				Players.Add( player );
				PlayersByConnection[entry.Key] = player;
			}

			player.StationIndex = entry.Value;
			player.Spectating = entry.Value < 0;
			player.Score = SyncedScores.TryGetValue( entry.Key, out var score ) ? score : 0;
			player.Ready = SyncedReady.TryGetValue( entry.Key, out var ready ) && ready;
			player.TournamentPoints = SyncedTournamentPoints.TryGetValue( entry.Key, out var points ) ? points : 0;
			player.FocusHits = SyncedFocusHits.TryGetValue( entry.Key, out var focusHits ) ? focusHits : 0;
			player.Name = SyncedNames.TryGetValue( entry.Key, out var name ) && !string.IsNullOrWhiteSpace( name )
				? name
				: entry.Value >= 0 ? $"STATION {entry.Value + 1}" : "PLAYER";
		}
	}

	private string BuildSyncedLeaderboardText()
	{
		if ( !Networking.IsActive || Networking.IsHost || string.IsNullOrWhiteSpace( SyncedResultOrder ) )
			return "";

		var orderedStations = SyncedResultOrder
			.Split( ',', StringSplitOptions.RemoveEmptyEntries )
			.Select( x => int.TryParse( x, out var stationIndex ) ? stationIndex : -1 )
			.Where( x => x >= 0 )
			.ToArray();

		if ( orderedStations.Length == 0 )
			return "";

		var lines = orderedStations.Select( ( stationIndex, index ) =>
		{
			var row = SyncedScores.FirstOrDefault( x => SyncedStations.TryGetValue( x.Key, out var syncedStation ) && syncedStation == stationIndex );
			var points = SyncedTournamentPoints.TryGetValue( row.Key, out var syncedPoints ) ? syncedPoints : 0;
			return $"{index + 1}. STATION {stationIndex + 1}  {row.Value} taps  {points} pts";
		} );

		return string.Join( "\n", lines );
	}

	private int GetDisplayActiveCount()
	{
		return Networking.IsActive && !Networking.IsHost ? SyncedStations.Values.Count( x => x >= 0 ) : ActiveCompetitorCount();
	}

	private string GetCameraDebugText()
	{
		var stationIndex = GetLocalStationIndex();
		var mode = GetCameraMode();
		var pressable = CanPressStation( stationIndex ) ? "PRESS OK" : "VIEW ONLY";
		var stationLabel = stationIndex >= 0 ? $"S{stationIndex + 1}" : "UNCLAIMED";
		return $"{mode.ToString().ToUpperInvariant()} {stationLabel}\n{pressable}";
	}
}
