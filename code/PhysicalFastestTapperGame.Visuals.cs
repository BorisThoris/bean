using Sandbox;
using System;
using System.Linq;

public sealed partial class PhysicalFastestTapperGame
{
	private void UpdateCentralDisplays()
	{
		if ( WallScreen.IsValid() )
			WallScreen.StateHasChanged();

		UpdateWallFallbackText();
	}

	private string GetTimerText()
	{
		return State switch
		{
			RoundState.WaitingForPlayers => "READY UP",
			RoundState.Countdown => $"TAP IN\n{Math.Max( 1, (int)MathF.Ceiling( StateTimeLeft ) )}",
			RoundState.Playing => $"TIME\n{Math.Max( 0, (int)MathF.Ceiling( RoundTimeLeft ) ):00}",
			RoundState.Results => "RESULTS",
			_ => $"NEXT\n{Math.Max( 0, (int)MathF.Ceiling( StateTimeLeft ) ):00}"
		};
	}

	private string BuildLeaderboardText()
	{
		var syncedLeaderboard = BuildSyncedLeaderboardText();
		if ( !string.IsNullOrWhiteSpace( syncedLeaderboard ) )
			return syncedLeaderboard;

		var ordered = GetOrderedResults();
		if ( ordered.Length == 0 )
			return "WALK TO A PLATFORM\nHIT ITS BUTTON TO CLAIM";

		var lines = ordered.Select( ( player, index ) => $"{index + 1}. {player.Name}  {player.Score} taps  {player.PeakSpeed:0.0}/s  {player.TournamentPoints} pts" );
		return string.Join( "\n", lines );
	}

	private void UpdateVisuals()
	{
		foreach ( var station in Stations )
		{
			var player = Players.FirstOrDefault( x => x.StationIndex == station.Index );
			UpdateStationVisuals( station, player );
		}

		ConfigureCamera();
	}

	private void UpdateStationVisuals( TapperStation station, PlayerScore player )
	{
		var heat = player?.Heat ?? 0f;
		var comboPulse = player?.ComboPulse ?? 0f;
		var countdownPulse = State == RoundState.Countdown ? (MathF.Sin( RealTime.Now * 9f ) + 1f) * 0.08f : 0f;
		var punch = station.ButtonPunch * 0.18f;
		var buttonScale = 1f + heat * 0.32f + punch + comboPulse * 0.18f + countdownPulse;

		if ( station.Button.IsValid() )
			station.Button.LocalScale = station.ButtonBaseScale * Math.Min( buttonScale, 2.2f );

		var hotColor = Color.Lerp( IdleButtonColor, HotButtonColor, heat );
		if ( station.ButtonRenderer.IsValid() )
			station.ButtonRenderer.Tint = hotColor;

		var duration = Math.Max( RoundDuration, 0.001f );
		var progress = (RoundTimeLeft / duration).Clamp( 0f, 1f );
		if ( State != RoundState.Playing )
			progress = State == RoundState.Countdown ? 1f : 0f;

		if ( station.ProgressFill.IsValid() )
		{
			station.ProgressFill.LocalScale = new Vector3( station.ProgressBaseScale.x * progress, station.ProgressBaseScale.y, station.ProgressBaseScale.z );
			station.ProgressFill.LocalPosition = station.ProgressBasePosition + Vector3.Right * ((station.ProgressBaseScale.x - station.ProgressFill.LocalScale.x) * -station.BarModelHalfExtentX);
		}

		if ( station.HeatFill.IsValid() )
		{
			var fill = Math.Max( heat, station.FinishFlash );
			station.HeatFill.LocalScale = new Vector3( station.HeatBaseScale.x * fill, station.HeatBaseScale.y, station.HeatBaseScale.z );
			station.HeatFill.LocalPosition = station.HeatBasePosition + Vector3.Right * ((station.HeatBaseScale.x - station.HeatFill.LocalScale.x) * -station.BarModelHalfExtentX);
		}

		if ( station.HeatFillRenderer.IsValid() )
			station.HeatFillRenderer.Tint = Color.Lerp( new Color( 0.15f, 0.65f, 1f, 1f ), HotButtonColor, heat );

		UpdateClaimFrameVisuals( station, player );
	}

	private void UpdateClaimFrameVisuals( TapperStation station, PlayerScore stationPlayer )
	{
		if ( station.ClaimFrame is null || station.ClaimFrameRenderers is null || station.ClaimFrameBaseScales is null )
			return;

		var local = GetLocalPlayer();
		var lobbyPhase = State is RoundState.WaitingForPlayers or RoundState.Results or RoundState.Intermission;
		var visible = stationPlayer is null
			&& lobbyPhase
			&& local is not null
			&& !local.Spectating
			&& local.StationIndex < 0;

		var inRange = visible && local.BeanController.IsValid() && local.BeanController.IsWithinClaimRange( station.Origin );
		var pulse = (MathF.Sin( RealTime.Now * (inRange ? 8f : 4f) ) + 1f) * 0.5f;
		var color = Color.Lerp( inRange ? ClaimFrameActiveColor : ClaimFrameIdleColor, Color.White, inRange ? pulse * 0.28f : pulse * 0.12f );
		var scaleMultiplier = 1f + (inRange ? 0.045f : 0.02f) * pulse;

		for ( var i = 0; i < station.ClaimFrame.Length; i++ )
		{
			var frame = station.ClaimFrame[i];
			if ( !frame.IsValid() )
				continue;

			frame.Enabled = visible;
			if ( visible && i < station.ClaimFrameBaseScales.Length )
				frame.LocalScale = station.ClaimFrameBaseScales[i] * scaleMultiplier;

			if ( i < station.ClaimFrameRenderers.Length && station.ClaimFrameRenderers[i].IsValid() )
				station.ClaimFrameRenderers[i].Tint = color;
		}
	}

	public string GetWallScreenTitle()
	{
		return "TAPPER ARENA";
	}

	public string GetWallScreenHeadline()
	{
		return GetEventHeadlineText();
	}

	public string GetWallScreenModeText()
	{
		var settings = GetModeSettings();
		return $"{settings.Label}  |  ROUND {TournamentRound}/{Math.Max( TournamentRounds, 1 )}  |  {GetDisplayActiveCount()} CLAIMED  |  {GetVenueWorldLabel()}";
	}

	public string GetWallScreenLeaderboardText()
	{
		return BuildLeaderboardText();
	}

	public string GetWallScreenHtmlDebugText()
	{
		return "NATIVE RAZOR WALL";
	}

	public TapperWallScreenState GetWallScreenState()
	{
		var duration = Math.Max( RoundDuration, 0.001f );
		var timeRemaining = State == RoundState.Playing ? RoundTimeLeft : StateTimeLeft;
		var ordered = GetOrderedResults();
		var leaders = ordered.Select( ( player, index ) => new TapperWallLeaderboardDisplay(
			index + 1,
			player.Name,
			GetPlayerSteamId( player ),
			player.Score,
			player.PeakSpeed,
			player.TournamentPoints,
			player.StationIndex == LastWinnerStation ) ).ToArray();

		return new TapperWallScreenState(
			GetWallScreenTitle(),
			GetWallScreenHeadline(),
			GetWallScreenModeText(),
			GetWallScreenHtmlDebugText(),
			State.ToString(),
			EventPhase.ToString(),
			GetModeLabel( GameMode ),
			TournamentRound,
			Math.Max( TournamentRounds, 1 ),
			GetDisplayActiveCount(),
			timeRemaining,
			State == RoundState.Playing ? (1f - (RoundTimeLeft / duration)).Clamp( 0f, 1f ) : 0f,
			leaders,
			GetWallScreenStationRows() );
	}

	public TapperWallStationDisplay[] GetWallScreenStationRows()
	{
		return Stations
			.OrderBy( x => x.Index )
			.Select( station =>
			{
				var player = Players.FirstOrDefault( x => x.StationIndex == station.Index );
				var name = player?.Name ?? "OPEN";
				var status = GetStationStatus( station, player );
				var score = player?.Score ?? 0;
				var speed = player?.LastSpeed ?? 0f;
				var meta = $"{score} taps  {speed:0.0}/s";
				var cssClass = GetWallStationRowCssClass( station, player );
				var progress = State == RoundState.Playing && RoundDuration > 0f ? (1f - (RoundTimeLeft / RoundDuration)).Clamp( 0f, 1f ) : 0f;
				return new TapperWallStationDisplay( $"S{station.Index + 1}", name, GetPlayerSteamId( player ), status, meta, cssClass, score, speed, progress, player?.Heat ?? 0f );
			} )
			.ToArray();
	}

	private static string GetPlayerSteamId( PlayerScore player )
	{
		if ( player?.Connection is null )
			return "";

		return player.Connection.SteamId.ToString();
	}

	private void UpdateWallFallbackText()
	{
		if ( WallFallbackText is null )
			return;

		var showFallback = ArenaWallScreenLayoutMath.ShouldShowFallback( IsPrimaryWallScreenValid() );
		SetWallFallbackVisible( showFallback );
		if ( !showFallback )
			return;

		SetText( WallFallbackText.Title, GetWallScreenTitle() );
		SetText( WallFallbackText.Debug, "FALLBACK" );
		SetText( WallFallbackText.Headline, GetWallScreenHeadline() );
		SetText( WallFallbackText.Mode, GetWallScreenModeText() );
		SetText( WallFallbackText.Leaderboard, GetWallScreenLeaderboardText() );
		SetText( WallFallbackText.Stations, BuildWallFallbackStationText() );
	}

	private string BuildWallFallbackStationText()
	{
		var rows = GetWallScreenStationRows();
		if ( rows.Length == 0 )
			return "NO STATIONS";

		return string.Join( "\n", rows.Select( row => $"{row.Station}  {row.Name}  {row.Status}  {row.Meta}" ) );
	}

	private string GetWallStationRowCssClass( TapperStation station, PlayerScore player )
	{
		if ( State is RoundState.Results or RoundState.Intermission && station.Index == LastWinnerStation )
			return "winner";

		if ( station.Index == GetLocalStationIndex() )
			return "local";

		if ( player?.Ready == true )
			return "ready";

		if ( player is null )
			return "open";

		return "claimed";
	}

	private bool IsPrimaryWallScreenValid()
	{
		return WallScreen.IsValid();
	}

	private string GetEventHeadlineText()
	{
		var baseTimer = GetTimerText();
		return EventPhase switch
		{
			TapperEventPhase.Warmup => "READY UP",
			TapperEventPhase.ReadyCheck => "LOCKED IN",
			TapperEventPhase.Countdown => baseTimer,
			TapperEventPhase.PhotoFinish => $"PHOTO\n{Math.Max( 0, (int)MathF.Ceiling( RoundTimeLeft ) ):00}",
			TapperEventPhase.Podium => GetWinnerHeadline(),
			TapperEventPhase.NextModePreview => $"NEXT\n{GetNextModeLabel()}",
			_ => baseTimer
		};
	}

	private string GetWinnerHeadline()
	{
		var winner = GetOrderedResults().FirstOrDefault();
		return winner is null ? "RESULTS" : $"WINNER\n{winner.Name}";
	}

	private string GetNextModeLabel()
	{
		var next = TournamentMode ? GetTournamentMode( Math.Min( TournamentRound + 1, Math.Max( TournamentRounds, 1 ) ) ) : GetNextAutoMode();
		return GetModeLabel( next );
	}

	private TapperGameMode GetNextAutoMode()
	{
		return GameMode switch
		{
			TapperGameMode.Classic10 => TapperGameMode.Sprint5,
			TapperGameMode.Sprint5 => TapperGameMode.Combo,
			TapperGameMode.Combo => TapperGameMode.Endurance30,
			_ => TapperGameMode.Classic10
		};
	}

	private string GetModeLabel( TapperGameMode mode )
	{
		return mode switch
		{
			TapperGameMode.Sprint5 => "SPRINT",
			TapperGameMode.Endurance30 => "ENDURANCE",
			TapperGameMode.Combo => "COMBO",
			_ => "CLASSIC"
		};
	}

	private string GetVenueWorldLabel()
	{
		if ( VenueMapLoaded )
			return "CONSTRUCT";

		return UseConstructWorld ? "CONSTRUCT FALLBACK" : "GENERATED VENUE";
	}

	private string GetStationStatus( TapperStation station, PlayerScore player )
	{
		if ( player is null )
		{
			var local = GetLocalPlayer();
			if ( local is not null && !string.IsNullOrWhiteSpace( local.LastInteractionMessage ) && RealTime.Now - local.LastInteractionMessageTime < 1.4f )
				return local.LastInteractionMessage;

			return State is RoundState.WaitingForPlayers or RoundState.Results or RoundState.Intermission ? "HIT TO CLAIM" : "OPEN";
		}

		if ( player.Spectating )
			return "SPECTATING";

		var callout = GetStationCallout( station, player );
		if ( callout != StationCallout.None )
		{
			return callout switch
			{
				StationCallout.Focus => "FOCUS",
				StationCallout.Chain => "CHAIN",
				StationCallout.Overheat => "OVERHEAT",
				StationCallout.PhotoFinish => "PHOTO FINISH",
				StationCallout.Winner => "WINNER",
				StationCallout.Spectating => "SPECTATING",
				_ => "READY"
			};
		}

		return State switch
		{
			RoundState.WaitingForPlayers => player.Ready ? "READY" : "CLICK TO READY",
			RoundState.Countdown => "GET SET",
			RoundState.Playing => "TAP",
			RoundState.Results => player.StationIndex == LastWinnerStation ? "WINNER" : "RESULT",
			_ => player.Ready ? "READY" : "PRESS FOR NEXT"
		};
	}

	private StationCallout GetStationCallout( TapperStation station, PlayerScore player )
	{
		if ( player is null )
			return StationCallout.None;

		if ( EventPhase == TapperEventPhase.PhotoFinish )
			return StationCallout.PhotoFinish;

		if ( State is RoundState.Results or RoundState.Intermission && station.Index == LastWinnerStation )
			return StationCallout.Winner;

		if ( player.FocusWindowActive )
			return StationCallout.Focus;

		if ( player.Heat >= 0.9f )
			return StationCallout.Overheat;

		if ( player.Combo >= 10 )
			return StationCallout.Chain;

		return StationCallout.None;
	}

	private Color GetModeAccentColor()
	{
		return GameMode switch
		{
			TapperGameMode.Sprint5 => new Color( 1f, 0.36f, 0.18f, 1f ),
			TapperGameMode.Endurance30 => new Color( 0.34f, 0.95f, 0.52f, 1f ),
			TapperGameMode.Combo => new Color( 1f, 0.78f, 0.24f, 1f ),
			_ => ReadyStationColor
		};
	}

}
