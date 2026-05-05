using Sandbox;
using System;
using System.Linq;

public sealed partial class PhysicalFastestTapperGame
{
	private bool TryHandleStationPress( int stationIndex )
	{
		return TryHandleStationPress( GetInteractingPlayer(), stationIndex );
	}

	private bool TryHandleStationPress( PlayerScore player, int stationIndex )
	{
		if ( player is null || stationIndex < 0 )
			return false;

		if ( !TryClaimStationFromPress( player, stationIndex ) && !CanPlayerUseStation( player, stationIndex ) )
		{
			SetInteractionMessage( player, GetDeniedStationMessage( player, stationIndex ) );
			return false;
		}

		if ( State is RoundState.WaitingForPlayers or RoundState.Results or RoundState.Intermission )
			return TryMarkReady( player );

		if ( State != RoundState.Playing )
			return false;

		return TryRegisterTap( player );
	}

	private bool TryClaimStationFromPress( PlayerScore player, int stationIndex )
	{
		var lobbyPhase = State is RoundState.WaitingForPlayers or RoundState.Results or RoundState.Intermission;
		var stationOwned = Players.Any( x => x != player && x.StationIndex == stationIndex );
		if ( !TapperStationInteractionRules.CanClaimStation( lobbyPhase, player.StationIndex, stationOwned ) )
			return false;

		var station = Stations.FirstOrDefault( x => x.Index == stationIndex );
		if ( station is null )
			return false;

		if ( !IsPlayerCloseEnoughToClaim( player, station ) )
		{
			SetInteractionMessage( player, "WALK CLOSER" );
			return false;
		}

		player.StationIndex = stationIndex;
		player.Spectating = false;
		player.Ready = false;
		player.LastInteractionMessage = "STATION LOCKED";
		player.LastInteractionMessageTime = RealTime.Now;
		TryPlaySound( "ui.button.press" );
		return true;
	}

	private bool TryMarkReady( PlayerScore player )
	{
		if ( !IsActiveCompetitor( player ) )
			return false;

		player.Ready = true;
		TryPlaySound( "ui.button.over" );

		if ( AllActivePlayersReady() )
			EnterCountdown();

		return true;
	}

	public bool CanPressStation( int stationIndex )
	{
		return CanInteractWithStation( stationIndex );
	}

	public bool CanInteractWithStation( int stationIndex )
	{
		var player = GetInteractingPlayer();
		if ( player is null )
			return false;

		if ( CanPlayerUseStation( player, stationIndex ) )
			return true;

		var lobbyPhase = State is RoundState.WaitingForPlayers or RoundState.Results or RoundState.Intermission;
		var stationOwned = Players.Any( x => x != player && x.StationIndex == stationIndex );
		return TapperStationInteractionRules.CanClaimStation( lobbyPhase, player.StationIndex, stationOwned );
	}

	private static bool CanPlayerUseStation( PlayerScore player, int stationIndex )
	{
		return player is not null
			&& !player.Spectating
			&& TapperStationInteractionRules.CanUseClaimedStation( player.StationIndex, stationIndex );
	}

	private void EnsureLocalFallbackPlayer()
	{
		if ( Players.Count == 0 )
			EnsurePlayer( Connection.Local );
	}

	private void EnsureKnownConnections()
	{
		PurgeDisconnectedPlayers();

		foreach ( var connection in Connection.All )
			EnsurePlayer( connection );
	}

	private PlayerScore EnsurePlayer( Connection connection )
	{
		var key = ConnectionKey( connection );
		if ( PlayersByConnection.TryGetValue( key, out var existing ) )
			return existing;

		var player = new PlayerScore
		{
			Connection = connection,
			ConnectionKey = key,
			Name = connection?.DisplayName ?? "LOCAL PLAYER",
			StationIndex = -1,
			Ready = false,
			Spectating = State is RoundState.Countdown or RoundState.Playing
		};

		Players.Add( player );
		PlayersByConnection[key] = player;
		EnsureStationCapacityForLobby();
		EnsurePlayerBean( player );
		return player;
	}

	private static string ConnectionKey( Connection connection )
	{
		return connection is null ? "local" : connection.Id.ToString();
	}

	private void PurgeDisconnectedPlayers()
	{
		var activeKeys = Connection.All.Select( ConnectionKey ).ToHashSet();
		if ( ShouldUseLocalFallbackPlayer() )
			activeKeys.Add( ConnectionKey( Connection.Local ) );

		for ( var i = Players.Count - 1; i >= 0; i-- )
		{
			var player = Players[i];
			player.ConnectionKey ??= ConnectionKey( player.Connection );

			if ( activeKeys.Contains( player.ConnectionKey ) )
				continue;

			DeletePlayerBean( player );
			Players.RemoveAt( i );
			PlayersByConnection.Remove( player.ConnectionKey );
		}

		if ( Players.Count == 0 )
			EnsureLocalFallbackPlayer();
	}

	private static bool ShouldUseLocalFallbackPlayer()
	{
		return !Networking.IsActive || !Connection.All.Any();
	}

	private bool AllActivePlayersReady()
	{
		var activePlayers = Players.Where( IsActiveCompetitor ).ToArray();
		return activePlayers.Length > 0 && activePlayers.All( x => x.Ready );
	}

	private void HandleLocalFallbackInput()
	{
		if ( Input.Keyboard.Pressed( "SPACE" ) || Input.Keyboard.Pressed( "ENTER" ) )
		{
			PressPhysicalButton( GetLocalStationIndex() );
			return;
		}

		if ( !IsPrimaryClickPressed() )
			return;

		if ( !IsCameraLookActive() && TryGetStationIndexUnderCursor( out var stationIndex ) )
		{
			PressPhysicalButton( stationIndex );
			return;
		}

		if ( TryGetNearestClaimableStationInRange( GetInteractingPlayer(), out stationIndex ) )
			PressPhysicalButton( stationIndex );
		else
			PressPhysicalButton( GetLocalStationIndex() );
	}

	private int GetLocalStationIndex()
	{
		var localKey = ConnectionKey( Connection.Local );
		if ( PlayersByConnection.TryGetValue( localKey, out var local ) && IsActiveCompetitor( local ) )
			return local.StationIndex;

		return -1;
	}

	private static bool IsActiveCompetitor( PlayerScore player )
	{
		return player is not null && !player.Spectating && player.StationIndex >= 0;
	}

	private int ActiveCompetitorCount()
	{
		return Players.Count( IsActiveCompetitor );
	}

	private PlayerScore GetInteractingPlayer()
	{
		if ( Rpc.Calling )
		{
			var key = ConnectionKey( Rpc.Caller );
			return PlayersByConnection.TryGetValue( key, out var caller ) ? caller : EnsurePlayer( Rpc.Caller );
		}

		var localKey = ConnectionKey( Connection.Local );
		if ( PlayersByConnection.TryGetValue( localKey, out var local ) )
			return local;

		if ( ShouldUseLocalFallbackPlayer() )
			return EnsurePlayer( Connection.Local );

		return null;
	}

	private string GetDeniedStationMessage( PlayerScore player, int stationIndex )
	{
		if ( Players.Any( x => x != player && x.StationIndex == stationIndex ) )
			return "TAKEN";

		if ( player.StationIndex >= 0 && player.StationIndex != stationIndex )
			return $"LOCKED TO S{player.StationIndex + 1}";

		if ( State is RoundState.Countdown or RoundState.Playing )
			return "RACE LOCKED";

		return "WAIT";
	}

	private static void SetInteractionMessage( PlayerScore player, string message )
	{
		if ( player is null )
			return;

		player.LastInteractionMessage = message;
		player.LastInteractionMessageTime = RealTime.Now;
	}

	public bool IsStationUnderCenterRay( int stationIndex )
	{
		return TryGetStationIndexUnderCursor( out var tracedStationIndex ) && tracedStationIndex == stationIndex;
	}

	private bool TryGetStationIndexUnderCursor( out int stationIndex )
	{
		stationIndex = -1;

		var camera = Scene.Camera;
		if ( !camera.IsValid() )
			return false;

		var ray = camera.ScreenPixelToRay( Mouse.Position );
		var trace = Scene.Trace
			.Ray( ray, 10000f )
			.HitTriggers()
			.Run();

		if ( !trace.Hit )
			return false;

		var hitObject = trace.Collider?.GameObject ?? trace.GameObject;
		return TryGetStationIndexFromButtonObject( hitObject, out stationIndex );
	}

	private bool TryGetNearestClaimableStationInRange( PlayerScore player, out int stationIndex )
	{
		stationIndex = -1;

		if ( player is null || player.StationIndex >= 0 )
			return false;

		var lobbyPhase = State is RoundState.WaitingForPlayers or RoundState.Results or RoundState.Intermission;
		if ( !lobbyPhase )
			return false;

		var station = Stations
			.Where( x => !Players.Any( playerScore => playerScore != player && playerScore.StationIndex == x.Index ) )
			.Where( x => IsPlayerCloseEnoughToClaim( player, x ) )
			.OrderBy( x => player.Bean.IsValid() ? player.Bean.WorldPosition.Distance( x.Origin ) : float.MaxValue )
			.FirstOrDefault();

		if ( station is null )
			return false;

		stationIndex = station.Index;
		return true;
	}

	internal static bool TryGetStationIndexFromButtonObject( GameObject hitObject, out int stationIndex )
	{
		stationIndex = -1;

		var current = hitObject;
		while ( current.IsValid() )
		{
			var tapButton = current.GetComponent<PhysicalTapButton>();
			if ( tapButton.IsValid() )
			{
				stationIndex = tapButton.StationIndex;
				return stationIndex >= 0;
			}

			if ( TapperStationObjectNames.TryParseStationIndex( current.Name, out stationIndex ) )
				return true;

			current = current.Parent;
		}

		stationIndex = -1;
		return false;
	}

	internal static bool IsPrimaryClickPressed()
	{
		return Input.Pressed( "attack1" ) || Input.Keyboard.Pressed( "MOUSE1" );
	}
}
