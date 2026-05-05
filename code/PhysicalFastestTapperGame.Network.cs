using Sandbox;
using System;
using System.Globalization;
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
		SyncedHeat.Clear();
		SyncedLookDirections.Clear();

		foreach ( var player in Players )
		{
			var key = player.ConnectionKey ?? ConnectionKey( player.Connection );
			SyncedScores[key] = player.Score;
			SyncedStations[key] = player.StationIndex;
			SyncedReady[key] = player.Ready;
			SyncedTournamentPoints[key] = player.TournamentPoints;
			SyncedFocusHits[key] = player.FocusHits;
			SyncedNames[key] = string.IsNullOrWhiteSpace( player.Name ) ? "PLAYER" : player.Name;
			SyncedHeat[key] = player.Heat;
			SyncedLookDirections[key] = EncodeLookDirection( player.LookDirection );
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
		if ( UseAuthoredScene )
		{
			var authoredTarget = Math.Clamp( SyncedStationCapacity, 0, 8 );
			if ( authoredTarget == CurrentGeneratedStationCount )
				return;

			EnsureAuthoredPlayStationCapacity( authoredTarget );
			BindAuthoredArena();
			return;
		}

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
			player.Heat = SyncedHeat.TryGetValue( entry.Key, out var heat ) ? heat : 0f;
			player.LookDirection = SyncedLookDirections.TryGetValue( entry.Key, out var lookDirectionText ) && TryDecodeLookDirection( lookDirectionText, out var lookDirection )
				? lookDirection
				: player.LookDirection;
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

	[Rpc.Host]
	private void RequestPlayerLookDirection( float x, float y, float z )
	{
		var player = GetInteractingPlayer();
		if ( player is null )
			return;

		player.LookDirection = NormalizeLookDirection( new Vector3( x, y, z ), player.LookDirection );
	}

	private static string EncodeLookDirection( Vector3 direction )
	{
		var normal = NormalizeLookDirection( direction, Vector3.Forward );
		return FormattableString.Invariant( $"{normal.x:0.###},{normal.y:0.###},{normal.z:0.###}" );
	}

	private static bool TryDecodeLookDirection( string value, out Vector3 direction )
	{
		direction = Vector3.Forward;
		if ( string.IsNullOrWhiteSpace( value ) )
			return false;

		var parts = value.Split( ',' );
		if ( parts.Length != 3 )
			return false;

		if ( !float.TryParse( parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x )
			|| !float.TryParse( parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y )
			|| !float.TryParse( parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var z ) )
			return false;

		direction = NormalizeLookDirection( new Vector3( x, y, z ), Vector3.Forward );
		return true;
	}

	private static Vector3 NormalizeLookDirection( Vector3 direction, Vector3 fallback )
	{
		if ( direction.LengthSquared > 0.001f )
			return direction.Normal;

		return fallback.LengthSquared > 0.001f ? fallback.Normal : Vector3.Forward;
	}
}
