using Sandbox;
using System;
using System.Linq;

public sealed partial class PhysicalFastestTapperGame
{
	private void EnterWaiting()
	{
		State = RoundState.WaitingForPlayers;
		EventPhase = TapperEventPhase.Warmup;
		StateTimeLeft = 0f;
		RoundDuration = GetModeSettings().Duration;
		RoundTimeLeft = RoundDuration;
		foreach ( var player in Players )
			player.Ready = false;
	}

	private void EnterCountdown()
	{
		if ( State == RoundState.Intermission )
			AdvanceModeAfterRound();

		State = RoundState.Countdown;
		EventPhase = TapperEventPhase.Countdown;
		StateTimeLeft = CountdownSeconds;
		RoundDuration = GetModeSettings().Duration;
		RoundTimeLeft = RoundDuration;
		LastWinnerStation = -1;
		LastCountdownSecond = Math.Max( 1, (int)MathF.Ceiling( CountdownSeconds ) );
		ResetRoundScores();
		TryPlaySound( "ui.navigate.forward" );
	}

	private void EnterPlaying()
	{
		State = RoundState.Playing;
		EventPhase = TapperEventPhase.Live;
		RoundTimeLeft = RoundDuration;
		LastCountdownSecond = -1;
		foreach ( var player in Players )
			player.LastTapTime = RealTime.Now;
		TryPlaySound( "ui.button.press" );
	}

	private void EnterResults()
	{
		State = RoundState.Results;
		EventPhase = TapperEventPhase.Podium;
		StateTimeLeft = IntermissionSeconds;

		var ordered = GetOrderedResults();
		var winner = ordered.FirstOrDefault();
		LastWinnerStation = winner?.StationIndex ?? -1;

		foreach ( var station in Stations )
			station.FinishFlash = station.Index == LastWinnerStation ? 1.4f : 0.65f;

		for ( var i = 0; i < ordered.Length; i++ )
		{
			var player = ordered[i];
			player.LastRoundPlacement = i + 1;
			player.LastRoundPoints = GetTournamentPointsForPlacement( i + 1 );
			player.TournamentPoints += player.LastRoundPoints;
		}

		foreach ( var player in Players )
		{
			player.LastRoundScore = player.Score;
			player.LastRoundPeakSpeed = player.PeakSpeed;
			player.LastRoundMaxCombo = player.MaxCombo;
			player.LastRoundFocusHits = player.FocusHits;
			player.RaceTrace = Math.Max( player.RaceTrace, GetRoundProgress() );

			if ( player.Score > player.BestScore )
				player.BestScore = player.Score;

			if ( !player.BestScoreByMode.TryGetValue( GameMode, out var modeBest ) || player.Score > modeBest )
				player.BestScoreByMode[GameMode] = player.Score;

			if ( player.PeakSpeed > player.BestPeakSpeed )
				player.BestPeakSpeed = player.PeakSpeed;

			player.Ready = false;
		}

		foreach ( var player in Players.Where( IsActiveCompetitor ) )
			player.ConsecutiveWins = player == winner ? player.ConsecutiveWins + 1 : 0;

		if ( winner is not null )
			winner.SessionWins++;

		TryPlaySound( winner is not null ? "ui.button.press" : "ui.navigate.forward" );
	}

	private void EnterIntermission()
	{
		State = RoundState.Intermission;
		EventPhase = TapperEventPhase.NextModePreview;
		StateTimeLeft = IntermissionSeconds;
		foreach ( var player in Players )
			player.Ready = false;

		TournamentRound = Math.Min( TournamentRound + 1, Math.Max( TournamentRounds, 1 ) );
	}

	private void ResetRoundScores()
	{
		foreach ( var player in Players )
		{
			player.Score = 0;
			player.Combo = 0;
			player.MaxCombo = 0;
			player.LastSpeed = 0f;
			player.PeakSpeed = 0f;
			player.Heat = 0f;
			player.ComboPulse = 0f;
			player.LastTapTime = RealTime.Now;
			player.FocusHits = 0;
			player.RaceTrace = 0f;
			player.FocusWindowActive = false;
		}

		foreach ( var station in Stations )
		{
			station.ButtonPunch = 0f;
			station.FinishFlash = 0f;
		}
	}

	private bool TryRegisterTap( PlayerScore player )
	{
		if ( State != RoundState.Playing || !IsActiveCompetitor( player ) )
			return false;

		var station = Stations.FirstOrDefault( x => x.Index == player.StationIndex );
		if ( station is null )
			return false;

		var settings = GetModeSettings();
		var now = RealTime.Now;
		var elapsed = Math.Max( now - player.LastTapTime, 0.001f );
		var focusActive = IsFocusWindowActive( player.StationIndex );
		player.LastSpeed = player.Score == 0 ? 1f : 1f / elapsed;
		player.PeakSpeed = Math.Max( player.PeakSpeed, player.LastSpeed );
		player.LastTapTime = now;
		player.Score += settings.ComboScoring && elapsed <= CooldownSeconds ? 1 + Math.Min( player.Combo / 10, 4 ) : 1;
		player.SessionTotalTaps++;
		player.Combo = elapsed <= CooldownSeconds ? player.Combo + 1 : 1;
		if ( focusActive )
		{
			player.FocusHits++;
			player.Combo = Math.Max( player.Combo, 2 );
			player.ComboPulse = 1f;
		}
		player.MaxCombo = Math.Max( player.MaxCombo, player.Combo );
		player.Heat = Math.Min( player.Heat + (0.12f + player.LastSpeed * 0.025f + (focusActive ? 0.035f : 0f)) * settings.HeatGain, 1f );
		player.RaceTrace = Math.Max( player.RaceTrace, GetRoundProgress() );
		station.ButtonPunch = 1f;
		if ( player.Combo > 0 && player.Combo % 10 == 0 )
			player.ComboPulse = 1f;

		if ( now - player.LastSoundTime > 0.035f )
		{
			TryPlaySound( player.Combo >= 15 ? "ui.button.press" : "ui.button.over" );
			player.LastSoundTime = now;
		}

		return true;
	}

	private void UpdateRoundFlow()
	{
		UpdateEventPhase();

		foreach ( var station in Stations )
		{
			station.ButtonPunch = station.ButtonPunch.Approach( 0f, RealTime.Delta * 9f );
			station.FinishFlash = station.FinishFlash.Approach( 0f, RealTime.Delta * 2.2f );
		}

		var settings = GetModeSettings();
		foreach ( var player in Players )
		{
			var coolingRate = State == RoundState.Playing ? settings.HeatDecay : settings.HeatDecay + 0.6f;
			player.Heat = player.Heat.Approach( 0f, RealTime.Delta * coolingRate );
			player.ComboPulse = player.ComboPulse.Approach( 0f, RealTime.Delta * 5f );
			player.FocusWindowActive = State == RoundState.Playing && IsFocusWindowActive( player.StationIndex );

			if ( State == RoundState.Playing && RealTime.Now - player.LastTapTime > CooldownSeconds )
			{
				player.Combo = 0;
				player.LastSpeed = player.LastSpeed.Approach( 0f, RealTime.Delta * 7f );
			}
		}

		if ( State == RoundState.WaitingForPlayers )
			return;

		StateTimeLeft -= RealTime.Delta;

		if ( State == RoundState.Countdown )
		{
			var countdownSecond = Math.Max( 1, (int)MathF.Ceiling( StateTimeLeft ) );
			if ( countdownSecond != LastCountdownSecond )
			{
				LastCountdownSecond = countdownSecond;
				TryPlaySound( "ui.button.over" );
			}

			if ( StateTimeLeft <= 0f )
				EnterPlaying();
			return;
		}

		if ( State == RoundState.Playing )
		{
			RoundTimeLeft -= RealTime.Delta;
			if ( RoundTimeLeft <= 0f )
				EnterResults();
			return;
		}

		if ( State == RoundState.Results && StateTimeLeft <= 0f )
		{
			EnterIntermission();
			return;
		}

		if ( State == RoundState.Intermission )
		{
			if ( AllActivePlayersReady() || StateTimeLeft <= 0f )
				EnterCountdown();
		}
	}

	private PlayerScore[] GetOrderedResults()
	{
		return Players
			.Where( IsActiveCompetitor )
			.OrderByDescending( x => x.Score )
			.ThenByDescending( x => x.PeakSpeed )
			.ThenByDescending( x => x.MaxCombo )
			.ToArray();
	}

	private ModeSettings GetModeSettings()
	{
		return GameMode switch
		{
			TapperGameMode.Sprint5 => new ModeSettings( "SPRINT 5s", 5f, 1.35f, 1.65f, false ),
			TapperGameMode.Endurance30 => new ModeSettings( "ENDURANCE 30s", 30f, 0.78f, 2.1f, false ),
			TapperGameMode.Combo => new ModeSettings( "COMBO 10s", GameDurationSeconds, 1.05f, 1.35f, true ),
			_ => new ModeSettings( "CLASSIC 10s", GameDurationSeconds, 1f, 1.2f, false )
		};
	}

	private void AdvanceModeAfterRound()
	{
		if ( TournamentMode )
		{
			GameMode = GetTournamentMode( TournamentRound );
			return;
		}

		if ( !AutoRotateModes )
			return;

		GameMode = GameMode switch
		{
			TapperGameMode.Classic10 => TapperGameMode.Sprint5,
			TapperGameMode.Sprint5 => TapperGameMode.Combo,
			TapperGameMode.Combo => TapperGameMode.Endurance30,
			_ => TapperGameMode.Classic10
		};
	}

	private void UpdateEventPhase()
	{
		EventPhase = State switch
		{
			RoundState.WaitingForPlayers => AllActivePlayersReady() ? TapperEventPhase.ReadyCheck : TapperEventPhase.Warmup,
			RoundState.Countdown => TapperEventPhase.Countdown,
			RoundState.Playing => RoundTimeLeft <= 2f ? TapperEventPhase.PhotoFinish : TapperEventPhase.Live,
			RoundState.Results => TapperEventPhase.Podium,
			RoundState.Intermission => TapperEventPhase.NextModePreview,
			_ => EventPhase
		};
	}

	private bool IsFocusWindowActive( int stationIndex )
	{
		if ( !EnableFocusWindows || State != RoundState.Playing )
			return false;

		var elapsed = RoundDuration - RoundTimeLeft;
		var cycle = (elapsed + stationIndex * 0.33f) % 3f;
		return cycle >= 1.85f && cycle <= 2.45f;
	}

	private float GetRoundProgress()
	{
		var duration = Math.Max( RoundDuration, 0.001f );
		return ((duration - RoundTimeLeft) / duration).Clamp( 0f, 1f );
	}

	private static int GetTournamentPointsForPlacement( int placement )
	{
		return placement switch
		{
			1 => 5,
			2 => 3,
			3 => 2,
			_ => 1
		};
	}

	private TapperGameMode GetTournamentMode( int round )
	{
		var finalRound = Math.Max( TournamentRounds, 1 );
		if ( UseFinalTieBreaker && round >= finalRound )
			return round % 2 == 0 ? TapperGameMode.Sprint5 : TapperGameMode.Combo;

		return ((round - 1) % 4) switch
		{
			1 => TapperGameMode.Sprint5,
			2 => TapperGameMode.Combo,
			3 => TapperGameMode.Endurance30,
			_ => TapperGameMode.Classic10
		};
	}
}
