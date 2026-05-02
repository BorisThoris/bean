using Sandbox;
using System;
using System.Linq;

public sealed partial class PhysicalFastestTapperGame
{
	private void UpdateCentralDisplays()
	{
		SetText( TitleText, "TAPPER ARENA" );
		SetText( TimerText, GetEventHeadlineText() );
		var settings = GetModeSettings();
		SetText( ModeText, $"{settings.Label}\nROUND {TournamentRound}/{Math.Max( TournamentRounds, 1 )}\n{GetDisplayActiveCount()} CLAIMED\n{GetVenueWorldLabel()}" );
		SetText( LeaderboardText, BuildLeaderboardText() );

		if ( ModeText.IsValid() )
			ModeText.Color = GetModeAccentColor();
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

		UpdateArenaReadabilityVisuals();
		UpdateAmbientVenueMotion();
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

		if ( station.ButtonTop.IsValid() )
			station.ButtonTop.LocalPosition = station.ButtonTopBasePosition + Vector3.Up * (-station.ButtonPunch * 10f);

		var hotColor = Color.Lerp( IdleButtonColor, HotButtonColor, heat );
		if ( station.ButtonRenderer.IsValid() )
			station.ButtonRenderer.Tint = hotColor;

		if ( station.ButtonTopRenderer.IsValid() )
			station.ButtonTopRenderer.Tint = Color.Lerp( new Color( 1f, 0.22f, 0.16f, 1f ), new Color( 1f, 0.95f, 0.16f, 1f ), heat );

		var duration = Math.Max( RoundDuration, 0.001f );
		var progress = (RoundTimeLeft / duration).Clamp( 0f, 1f );
		if ( State != RoundState.Playing )
			progress = State == RoundState.Countdown ? 1f : 0f;

		if ( station.ProgressFill.IsValid() )
		{
			station.ProgressFill.LocalScale = new Vector3( station.ProgressBaseScale.x * progress, station.ProgressBaseScale.y, station.ProgressBaseScale.z );
			station.ProgressFill.LocalPosition = station.ProgressBasePosition + Vector3.Right * ((station.ProgressBaseScale.x - station.ProgressFill.LocalScale.x) * -25f);
		}

		if ( station.HeatFill.IsValid() )
		{
			var fill = Math.Max( heat, station.FinishFlash );
			station.HeatFill.LocalScale = new Vector3( station.HeatBaseScale.x * fill, station.HeatBaseScale.y, station.HeatBaseScale.z );
			station.HeatFill.LocalPosition = station.HeatBasePosition + Vector3.Right * ((station.HeatBaseScale.x - station.HeatFill.LocalScale.x) * -25f);
		}

		if ( station.HeatFillRenderer.IsValid() )
			station.HeatFillRenderer.Tint = Color.Lerp( new Color( 0.15f, 0.65f, 1f, 1f ), HotButtonColor, heat );

		UpdateRaceTraceVisuals( station, player );
		UpdateStationIdentityVisuals( station, player, heat );
		SetText( station.NameText, player?.Name ?? "OPEN STATION" );
		SetText( station.ScoreText, ShowRoundSummary && State is (RoundState.Results or RoundState.Intermission) ? BuildStationSummaryText( player ) : GetScoreText( player ) );
		SetText( station.SpeedText, $"{player?.LastSpeed ?? 0f:0.0}/s" );
		SetText( station.ComboText, $"x{player?.Combo ?? 0}" );
		SetText( station.RankText, GetRankText( player ) );
		SetText( station.StatusText, GetStationStatus( station, player ) );

		UpdateSparkVisuals( station, heat );
	}

	private void UpdateStationIdentityVisuals( TapperStation station, PlayerScore player, float heat )
	{
		var occupied = player is not null;
		var ready = player?.Ready ?? false;
		var winner = State is RoundState.Results or RoundState.Intermission && station.Index == LastWinnerStation;
		var local = station.Index == GetLocalStationIndex();
		var modeAccent = GetModeAccentColor();
		var stateColor = winner ? WinnerStationColor : ready ? ReadyStationColor : occupied ? Color.Lerp( modeAccent, HotButtonColor, heat ) : OpenStationColor;
		if ( local && !winner )
			stateColor = Color.Lerp( stateColor, ReadyStationColor, 0.35f + MathF.Sin( RealTime.Now * 4f ).Remap( -1f, 1f, 0f, 0.2f ) );

		if ( station.FloorMarkerRenderer.IsValid() )
			station.FloorMarkerRenderer.Tint = Color.Lerp( OpenStationColor, stateColor, occupied ? 0.75f : 0.25f );

		if ( station.FloorMarker.IsValid() )
		{
			var localPulse = local ? 1.12f + MathF.Sin( RealTime.Now * 5f ) * 0.04f : 0.9f;
			station.FloorMarker.LocalScale = new Vector3( 4.8f * localPulse, 4.8f * localPulse, 0.05f );
		}

		if ( station.ReadyLightRenderer.IsValid() )
			station.ReadyLightRenderer.Tint = stateColor;

		if ( station.ReadyLight.IsValid() )
			station.ReadyLight.LocalScale = new Vector3( 0.45f + heat * 0.18f + station.FinishFlash * 0.12f, 0.45f + heat * 0.18f + station.FinishFlash * 0.12f, 0.08f );

		if ( station.WinnerGlowRenderer.IsValid() )
			station.WinnerGlowRenderer.Tint = winner ? WinnerStationColor : Color.Lerp( new Color( 0.04f, 0.05f, 0.06f, 1f ), HotButtonColor, Math.Max( heat * 0.25f, station.FinishFlash * 0.45f ) );

		if ( station.WinnerGlow.IsValid() )
		{
			var pulse = winner ? 1.25f + MathF.Sin( RealTime.Now * 7f ) * 0.18f : 0.7f + heat * 0.12f + station.FinishFlash * 0.18f;
			station.WinnerGlow.LocalScale = new Vector3( 2.6f * pulse, 2.6f * pulse, 0.08f );
		}

		if ( station.FocusRingRenderer.IsValid() )
			station.FocusRingRenderer.Tint = player?.FocusWindowActive == true ? Color.Lerp( GetModeAccentColor(), HotButtonColor, 0.35f ) : new Color( 0.04f, 0.08f, 0.1f, 1f );

		if ( station.FocusRing.IsValid() )
		{
			var focusPulse = player?.FocusWindowActive == true ? 1.2f + MathF.Sin( RealTime.Now * 12f ) * 0.12f : 0.82f;
			station.FocusRing.LocalScale = new Vector3( 3.4f * focusPulse, 3.4f * focusPulse, 0.04f );
		}

		SetText( station.StationNumberText, local ? "YOUR STATION" : $"STATION {station.Index + 1}" );
	}

	private void UpdateRaceTraceVisuals( TapperStation station, PlayerScore player )
	{
		if ( !station.RaceTraceFill.IsValid() )
			return;

		var trace = State is RoundState.Results or RoundState.Intermission
			? player?.RaceTrace ?? 0f
			: player?.Score > 0 ? GetRoundProgress() : 0f;

		trace = trace.Clamp( 0f, 1f );
		station.RaceTraceFill.LocalScale = new Vector3( 4.9f * trace, 0.2f, 0.09f );
		station.RaceTraceFill.LocalPosition = station.Origin + new Vector3( 34f - (4.9f - station.RaceTraceFill.LocalScale.x) * 25f, 86f, 138f );

		if ( station.RaceTraceFillRenderer.IsValid() )
			station.RaceTraceFillRenderer.Tint = Color.Lerp( GetModeAccentColor(), WinnerStationColor, player?.LastRoundPlacement == 1 ? 0.65f : 0.15f );
	}

	private void UpdateArenaReadabilityVisuals()
	{
		if ( !ArenaKeyGlowRenderer.IsValid() || !ArenaKeyGlow.IsValid() )
			return;

		var hottest = Players.Where( IsActiveCompetitor ).Select( x => x.Heat ).DefaultIfEmpty( 0f ).Max();
		var winnerPulse = State is RoundState.Results or RoundState.Intermission && LastWinnerStation >= 0
			? (MathF.Sin( RealTime.Now * 6f ) + 1f) * 0.5f
			: 0f;

		ArenaKeyGlowRenderer.Tint = Color.Lerp( Color.Lerp( new Color( 0.08f, 0.12f, 0.18f, 1f ), GetModeAccentColor(), 0.35f ), WinnerStationColor, Math.Max( hottest * 0.35f, winnerPulse * 0.55f ) );
		ArenaKeyGlow.LocalScale = new Vector3( 18f + hottest * 2f + winnerPulse * 3f, 0.2f, 2.8f + winnerPulse * 0.4f );
	}

	private void UpdateAmbientVenueMotion()
	{
		if ( !EnableAmbientVenueMotion )
			return;

		var hottest = Players.Where( IsActiveCompetitor ).Select( x => x.Heat ).DefaultIfEmpty( 0f ).Max();
		var celebration = State is RoundState.Results or RoundState.Intermission && LastWinnerStation >= 0;
		var modeAccent = GetModeAccentColor();

		foreach ( var ambient in AmbientVenueObjects )
		{
			if ( ambient?.GameObject is null || !ambient.GameObject.IsValid() )
				continue;

			var wave = MathF.Sin( RealTime.Now * 1.6f + ambient.Phase ).Remap( -1f, 1f, 0f, 1f );
			switch ( ambient.Role )
			{
				case AmbientVenueRole.Crowd:
					var bob = wave * (2f + hottest * 4f);
					ambient.GameObject.LocalPosition = ambient.BasePosition + Vector3.Up * bob;
					ambient.GameObject.LocalScale = ambient.BaseScale * (1f + wave * 0.035f);
					break;
				case AmbientVenueRole.Light:
					ambient.GameObject.LocalScale = ambient.BaseScale * (1f + wave * 0.18f + hottest * 0.12f);
					break;
				case AmbientVenueRole.Sign:
					ambient.GameObject.LocalScale = new Vector3( ambient.BaseScale.x * (1f + hottest * 0.08f), ambient.BaseScale.y, ambient.BaseScale.z * (1f + wave * 0.25f) );
					break;
				case AmbientVenueRole.Celebration:
					var pulse = celebration ? 0.45f + wave * 0.55f : hottest * 0.35f;
					ambient.GameObject.LocalScale = ambient.BaseScale * (1f + pulse * 0.45f);
					break;
			}

			if ( ambient.Renderer.IsValid() )
			{
				var intensity = ambient.Role == AmbientVenueRole.Crowd ? hottest * 0.25f : 0.25f + wave * 0.25f + hottest * 0.35f;
				ambient.Renderer.Tint = Color.Lerp( ambient.BaseColor, modeAccent, intensity.Clamp( 0f, celebration ? 0.85f : 0.55f ) );
			}
		}
	}

	private string GetRankText( PlayerScore player )
	{
		if ( player is null )
			return "RANK\nEMPTY";

		if ( State is RoundState.Results or RoundState.Intermission && player.StationIndex == LastWinnerStation )
			return "RANK\nWINNER";

		if ( State != RoundState.Results )
			return player.Heat > 0.75f ? "RANK\nON FIRE" : "RANK\nREADY";

		return $"RANK\n{GetRoundRating( player )}";
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

	private string GetScoreText( PlayerScore player )
	{
		if ( player is null )
			return "0";

		if ( State is RoundState.Results or RoundState.Intermission )
		{
			player.BestScoreByMode.TryGetValue( GameMode, out var modeBest );
			return $"{player.Score}\nBEST {modeBest}";
		}

		return $"{player.Score}";
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

	private string BuildStationSummaryText( PlayerScore player )
	{
		if ( player is null )
			return "OPEN";

		player.BestScoreByMode.TryGetValue( GameMode, out var modeBest );
		return $"{player.LastRoundScore}\n+{player.LastRoundPoints} pts\nBEST {modeBest}";
	}

	private string GetRoundRating( PlayerScore player )
	{
		if ( player is null )
			return "EMPTY";

		return player.LastRoundScore switch
		{
			>= 90 => "LEGEND",
			>= 70 => "S-TIER",
			>= 50 => "HYPER",
			>= 30 => "FAST",
			_ => "WARMUP"
		};
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

	private void UpdateSparkVisuals( TapperStation station, float heat )
	{
		if ( station.Sparks is null )
			return;

		for ( var i = 0; i < station.Sparks.Length; i++ )
		{
			var spark = station.Sparks[i];
			if ( !spark.IsValid() )
				continue;

			var phase = RealTime.Now * (1.8f + i * 0.12f) + i * 0.9f;
			var flicker = (MathF.Sin( phase * 4f ) + 1f) * 0.5f;
			var intensity = (heat * 0.85f + station.FinishFlash * 0.45f) * flicker;
			var angle = phase + MathF.PI * 2f * i / station.Sparks.Length;
			var radius = 92f + heat * 34f;

			spark.LocalPosition = station.Origin + new Vector3( MathF.Cos( angle ) * radius, MathF.Sin( angle ) * radius, 112f + flicker * 32f );
			spark.LocalScale = Vector3.One * (0.08f + intensity * 0.18f);
			spark.Enabled = intensity > 0.08f;

			var renderer = spark.GetComponent<ModelRenderer>();
			if ( renderer.IsValid() )
				renderer.Tint = Color.Lerp( new Color( 0.2f, 0.75f, 1f, 0.9f ), HotButtonColor, intensity );
		}
	}

}
