using Sandbox;
using System;
using System.Collections.Generic;
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

		UpdateVenueDynamicLights();
		UpdateProjectionSphereEffects();
		ConfigureCamera();
	}

	private void UpdateProjectionSphereEffects()
	{
		if ( !UseProjectionSphere || ProjectionSphereRadius <= 0f )
			return;

		UpdateProjectionSphereRotation();
		UpdateProjectionTopLight();

		if ( ProjectionSphereRenderer.IsValid() )
			ProjectionSphereRenderer.Tint = ProjectionSphereTint;
	}

	private void UpdateProjectionSphereRotation()
	{
		var baseRotation = Rotation.FromPitch( ProjectionSpherePitchTilt ) * Rotation.FromYaw( RealTime.Now * ProjectionSphereRotationSpeed );

		if ( ProjectionSphereObject.IsValid() )
			ProjectionSphereObject.LocalRotation = baseRotation;
	}

	private void UpdateProjectionTopLight()
	{
		if ( !ProjectionTopLightObject.IsValid() )
			return;

		ProjectionTopLightObject.LocalPosition = ProjectionSphereCenter + Vector3.Up * ProjectionSphereRadius * 0.62f;
		if ( ProjectionTopLightMarkerObject.IsValid() )
		{
			ProjectionTopLightMarkerObject.LocalPosition = ProjectionTopLightObject.LocalPosition;
			ProjectionTopLightMarkerObject.LocalRotation = Rotation.FromYaw( RealTime.Now * 24f );
		}

		if ( ProjectionTopLightMarkerRenderer.IsValid() )
			ProjectionTopLightMarkerRenderer.Tint = ProjectionTopLightColor * (1.6f + (MathF.Sin( RealTime.Now * 2.2f ) + 1f) * 0.45f);

		if ( ProjectionTopLight.IsValid() )
		{
			ProjectionTopLight.LightColor = ProjectionTopLightColor * ProjectionTopLightIntensity;
			ProjectionTopLight.Radius = ProjectionTopLightRadius;
		}
	}

	private readonly struct ProjectionCycleState
	{
		public readonly float Progress;
		public readonly float Daylight;
		public readonly float Night;
		public readonly float Sunset;

		public ProjectionCycleState( float progress, float daylight, float night, float sunset )
		{
			Progress = progress;
			Daylight = daylight;
			Night = night;
			Sunset = sunset;
		}
	}

	private ProjectionCycleState GetProjectionCycleState()
	{
		if ( !EnableSphereDayNightCycle )
			return new ProjectionCycleState( 0.25f, 1f, 0f, 0f );

		var dayLength = Math.Max( 1f, SphereDayLengthSeconds );
		var nightLength = Math.Max( 1f, SphereNightLengthSeconds );
		var total = dayLength + nightLength;
		var startOffset = StartProjectionCycleAtNight ? dayLength + nightLength * 0.12f : 0f;
		var elapsed = (RealTime.Now - ProjectionCycleStartTime + startOffset) % total;
		var isDay = elapsed < dayLength;
		var phase = isDay ? elapsed / dayLength : (elapsed - dayLength) / nightLength;
		var progress = isDay ? phase * 0.5f : 0.5f + phase * 0.5f;
		var daylight = isDay ? MathF.Sin( phase * MathF.PI ).Clamp( 0f, 1f ) : 0f;
		var night = isDay ? (1f - daylight).Clamp( 0f, 1f ) * 0.35f : 1f;
		night = Math.Max( night, NightSkyMinimumVisibility.Clamp( 0f, 1f ) );
		var sunset = isDay ? (1f - MathF.Abs( phase - 0.5f ) * 2.8f).Clamp( 0f, 1f ) * 0.28f : 0f;
		return new ProjectionCycleState( progress, daylight, night, sunset );
	}

	private void UpdateProjectionSun( ProjectionCycleState cycle )
	{
		if ( !ProjectionSunObject.IsValid() )
			return;

		var angle = cycle.Progress * MathF.PI * 2f;
		var orbit = new Vector3(
			MathF.Cos( angle ) * 0.74f,
			MathF.Sin( angle * 0.72f ) * 0.38f,
			MathF.Sin( angle ) ).Normal;
		var position = ProjectionSphereCenter + orbit * ProjectionSphereRadius * SunOrbitRadiusScale;
		var tint = Color.Lerp( new Color( 0.32f, 0.18f, 0.08f, 1f ), SunDayColor, cycle.Daylight );
		tint = Color.Lerp( tint, new Color( 1f, 0.32f, 0.16f, 1f ), cycle.Sunset );
		var lightScale = EnableSphereDayNightCycle ? (0.04f + cycle.Daylight * 1.08f + cycle.Sunset * 0.32f) : 1f;

		ProjectionSunObject.LocalPosition = position;
		ProjectionSunObject.LocalRotation = Rotation.LookAt( (ProjectionSphereCenter - position).Normal, Vector3.Up );
		ProjectionSunObject.Enabled = !EnableSphereDayNightCycle || lightScale > 0.08f;

		if ( ProjectionSunRenderer.IsValid() )
			ProjectionSunRenderer.Tint = tint;

		if ( ProjectionSunLight.IsValid() )
		{
			ProjectionSunLight.LightColor = tint * lightScale;
			ProjectionSunLight.Radius = SunLightRadius * (0.7f + lightScale * 0.45f);
		}
	}

	private void UpdateProjectionMoon( ProjectionCycleState cycle )
	{
		if ( !ProjectionMoonObject.IsValid() )
			return;

		var nightScale = EnableSphereDayNightCycle ? cycle.Night : 1f;
		var angle = cycle.Progress * MathF.PI * 2f + MathF.PI;
		var orbit = new Vector3(
			MathF.Cos( angle ) * 0.62f,
			MathF.Sin( angle * 0.72f ) * 0.34f,
			MathF.Sin( angle ) ).Normal;
		var position = ProjectionSphereCenter + orbit * ProjectionSphereRadius * MoonOrbitRadiusScale;
		var glowPulse = 1f + MathF.Sin( RealTime.Now * 1.4f ) * 0.08f;
		var moonScale = MathF.Max( 82f, ProjectionSphereRadius * 0.072f );
		var tint = MoonNightColor * (0.35f + nightScale * 1.85f);

		ProjectionMoonObject.Enabled = EnableProjectionMoon && nightScale > 0.05f;
		ProjectionMoonObject.LocalPosition = position;
		ProjectionMoonObject.LocalRotation = Rotation.LookAt( (ProjectionSphereCenter - position).Normal, Vector3.Up );
		ProjectionMoonObject.LocalScale = Vector3.One * moonScale * (0.88f + nightScale * 0.18f);

		if ( ProjectionMoonRenderer.IsValid() )
			ProjectionMoonRenderer.Tint = tint;

		if ( ProjectionMoonGlowObject.IsValid() )
		{
			ProjectionMoonGlowObject.Enabled = ProjectionMoonObject.Enabled;
			ProjectionMoonGlowObject.LocalPosition = position + (ProjectionSphereCenter - position).Normal * 3f;
			ProjectionMoonGlowObject.LocalRotation = ProjectionMoonObject.LocalRotation;
			ProjectionMoonGlowObject.LocalScale = Vector3.One * moonScale * MathF.Max( 1f, MoonGlowScale ) * glowPulse;
		}

		if ( ProjectionMoonGlowRenderer.IsValid() )
			ProjectionMoonGlowRenderer.Tint = new Color( 0.72f, 0.86f, 1f, 1f ) * (0.32f + nightScale * 1.55f);

		if ( ProjectionMoonLight.IsValid() )
		{
			ProjectionMoonLight.LightColor = MoonNightColor * MoonLightIntensityScale * (0.35f + nightScale * 1.1f);
			ProjectionMoonLight.Radius = MoonLightRadius * (0.85f + nightScale * 0.45f);
		}
	}

	private void UpdateProjectionVisuals( List<ProjectionSphereVisual> visuals, ProjectionCycleState cycle, bool stars )
	{
		if ( visuals.Count == 0 )
			return;

		var enabled = stars ? EnableSphereStars : EnablePixelClothesProjection;
		foreach ( var visual in visuals )
		{
			if ( !visual.GameObject.IsValid() )
				continue;

			visual.GameObject.Enabled = enabled;
			if ( !enabled )
				continue;

			var time = RealTime.Now;
			var longitude = visual.Longitude + visual.Phase + time * visual.OrbitSpeed;
			var latitude = visual.Latitude;
			if ( !stars )
				latitude += MathF.Sin( time * 0.7f + visual.Phase ) * 0.08f;

			var direction = ProjectionSphereDirection( longitude, latitude );
			var position = ProjectionSphereCenter + direction * ProjectionSphereRadius * visual.RadiusScale;
			visual.GameObject.LocalPosition = position;
			visual.GameObject.LocalRotation = Rotation.LookAt( (ProjectionSphereCenter - position).Normal, Vector3.Up );
			if ( visual.GlowObject.IsValid() )
			{
				visual.GlowObject.Enabled = enabled;
				visual.GlowObject.LocalPosition = position + (ProjectionSphereCenter - position).Normal * 2f;
				visual.GlowObject.LocalRotation = visual.GameObject.LocalRotation;
			}

			if ( stars )
			{
				var twinkle = 0.72f + (MathF.Sin( time * StarTwinkleSpeed + visual.Phase ) + 1f) * 0.22f;
				visual.GameObject.LocalScale = Vector3.One * visual.BaseScale * twinkle;
				if ( visual.GlowObject.IsValid() )
					visual.GlowObject.LocalScale = Vector3.One * visual.BaseScale * MathF.Max( 1f, StarGlowScale ) * (0.88f + twinkle * 0.22f);
			}
			else
			{
				var pulse = 1f + MathF.Sin( time * 2.4f + visual.Phase ) * 0.06f;
				visual.GameObject.LocalScale = Vector3.One * visual.BaseScale * pulse;
			}

			if ( visual.Renderer.IsValid() )
			{
				var tint = Color.Lerp( visual.DayColor, visual.NightColor, stars ? cycle.Night : cycle.Night * 0.55f );
				if ( stars )
					tint *= 0.55f + cycle.Night * (1.25f + MathF.Sin( time * 1.7f + visual.Phase ) * 0.32f);

				visual.Renderer.Tint = tint;
			}

			if ( stars && visual.GlowRenderer.IsValid() )
			{
				var glow = 0.22f + cycle.Night * (0.95f + (MathF.Sin( time * StarTwinkleSpeed + visual.Phase ) + 1f) * 0.28f);
				visual.GlowRenderer.Tint = visual.NightColor * glow;
			}

			if ( stars && visual.Light.IsValid() )
			{
				var pulse = 0.75f + (MathF.Sin( time * StarTwinkleSpeed + visual.Phase ) + 1f) * 0.18f;
				visual.Light.LightColor = visual.NightColor * cycle.Night * pulse * 1.8f;
				visual.Light.Radius = (520f + MathF.Sin( visual.Phase ) * 120f) * (0.75f + cycle.Night * 0.55f);
			}
		}
	}

	private void UpdateProjectionShootingStars( ProjectionCycleState cycle )
	{
		if ( ProjectionShootingStars.Count == 0 )
			return;

		var enabled = EnableShootingStars && cycle.Night > 0.45f;
		var now = RealTime.Now;
		if ( enabled && now >= NextShootingStarTime )
		{
			SpawnProjectionShootingStar( now );
			var cadence = MathF.Max( 0.45f, ShootingStarIntervalSeconds );
			NextShootingStarTime = now + cadence * (0.72f + (ProjectionShootingStars.Count % 3) * 0.18f);
		}

		foreach ( var visual in ProjectionShootingStars )
		{
			if ( !visual.GameObject.IsValid() )
				continue;

			if ( !enabled || !visual.Active )
			{
				visual.GameObject.Enabled = false;
				if ( visual.Light.IsValid() )
					visual.Light.Radius = 0f;
				continue;
			}

			var life = ((now - visual.SpawnTime) / MathF.Max( 0.1f, visual.Duration )).Clamp( 0f, 1f );
			if ( life >= 1f )
			{
				visual.Active = false;
				visual.GameObject.Enabled = false;
				if ( visual.Light.IsValid() )
					visual.Light.Radius = 0f;
				continue;
			}

			var longitude = visual.StartLongitude + visual.Direction * life * ShootingStarSpeed * 2.4f;
			var latitude = visual.StartLatitude - life * 0.34f;
			var direction = ProjectionSphereDirection( longitude, latitude );
			var position = ProjectionSphereCenter + direction * ProjectionSphereRadius * 0.955f;
			var tangent = ProjectionSphereDirection( longitude + visual.Direction * 0.08f, latitude - 0.02f ) - direction;
			var intensity = MathF.Sin( life * MathF.PI ).Clamp( 0f, 1f ) * cycle.Night;

			visual.GameObject.Enabled = true;
			visual.GameObject.LocalPosition = position;
			visual.GameObject.LocalRotation = Rotation.LookAt( (ProjectionSphereCenter - position).Normal, tangent.Normal );
			visual.GameObject.LocalScale = new Vector3( 1f, ShootingStarTrailLength, 0.45f ) * visual.BaseScale * (0.78f + intensity * 0.46f);

			if ( visual.Renderer.IsValid() )
				visual.Renderer.Tint = Color.Lerp( ShootingStarColor, Color.White, 0.55f ) * (0.65f + intensity * 2.25f);

			if ( visual.Light.IsValid() )
			{
				visual.Light.LightColor = ShootingStarColor * (0.35f + intensity * 2.2f);
				visual.Light.Radius = ShootingStarLightRadius * intensity;
			}
		}
	}

	private void SpawnProjectionShootingStar( float now )
	{
		var visual = ProjectionShootingStars.FirstOrDefault( x => !x.Active );
		if ( visual is null )
			return;

		var seed = now * 1.37f + visual.Phase;
		visual.Active = true;
		visual.SpawnTime = now;
		visual.Duration = 0.9f + (MathF.Sin( seed ) + 1f) * 0.28f;
		visual.Direction = MathF.Sin( seed * 0.73f ) >= 0f ? 1f : -1f;
		visual.StartLongitude = seed + visual.Phase;
		visual.StartLatitude = 0.72f + (MathF.Sin( seed * 1.9f ) + 1f) * 0.22f;
	}

	private static Vector3 ProjectionSphereDirection( float longitude, float latitude )
	{
		var clampedLatitude = latitude.Clamp( -1.25f, 1.25f );
		var cosLatitude = MathF.Cos( clampedLatitude );
		return new Vector3(
			MathF.Cos( longitude ) * cosLatitude,
			MathF.Sin( longitude ) * cosLatitude,
			MathF.Sin( clampedLatitude ) ).Normal;
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

		if ( station.ButtonRenderer.IsValid() )
			station.ButtonRenderer.Tint = Color.White;

		var frameColor = ReadyStationColor * 0.55f;
		var localPlayer = GetLocalPlayer();
		if ( player is not null )
			frameColor = player.StationIndex == LastWinnerStation && station.FinishFlash > 0f
				? Color.Lerp( ReadyStationColor, WinnerStationColor, station.FinishFlash )
				: ReadyStationColor;

		if ( localPlayer is not null && (player is null || ReferenceEquals( player, localPlayer )) && IsPlayerInsideStationBounds( localPlayer, station ) )
			frameColor = Color.Lerp( ReadyStationColor, HotButtonColor, 0.75f );

		foreach ( var frameRenderer in station.ClaimFrameRenderers )
		{
			if ( frameRenderer.IsValid() )
				frameRenderer.Tint = frameColor;
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

	private void UpdateVenueDynamicLights()
	{
		if ( VenueDynamicLights.Count == 0 )
			return;

		var pulse = (MathF.Sin( RealTime.Now * GetVenueLightPulseSpeed() ) + 1f) * 0.5f;
		var phaseColor = GetVenueLightPhaseColor();

		foreach ( var light in VenueDynamicLights )
		{
			if ( !light.GameObject.IsValid() )
				continue;

			var targetColor = phaseColor;
			var intensity = 1f;
			var radiusScale = 1f;

			if ( light.Role == VenueLightRole.Ambient )
			{
				intensity = 0.72f + pulse * 0.22f;
				radiusScale = 0.95f + pulse * 0.08f;
				targetColor = Color.Lerp( light.BaseColor, phaseColor, 0.45f );
			}
			else if ( light.Role == VenueLightRole.WallScreen )
			{
				intensity = State == RoundState.Playing ? 1.35f + pulse * 0.28f : 1.05f + pulse * 0.18f;
				radiusScale = 1.02f + pulse * 0.12f;
				targetColor = Color.Lerp( ReadyStationColor, phaseColor, 0.65f );
			}
			else
			{
				var player = Players.FirstOrDefault( x => x.StationIndex == light.StationIndex );
				var heat = player?.Heat ?? 0f;
				var isWinner = State is RoundState.Results or RoundState.Intermission && light.StationIndex == LastWinnerStation;
				targetColor = isWinner ? WinnerStationColor : Color.Lerp( ReadyStationColor, HotButtonColor, heat );
				intensity = 0.62f + heat * 1.15f + pulse * (State == RoundState.Countdown ? 0.55f : 0.18f) + (isWinner ? 0.65f : 0f);
				radiusScale = 0.76f + heat * 0.38f + (isWinner ? 0.18f : 0f);
			}

			ApplyVenueLightVisuals( light, targetColor * intensity, light.BaseRadius * radiusScale );
		}
	}

	private static void ApplyVenueLightVisuals( VenueDynamicLight light, Color color, float radius )
	{
		if ( light.Point.IsValid() )
		{
			light.Point.LightColor = color;
			light.Point.Radius = radius;
		}

		if ( light.Spot.IsValid() )
		{
			light.Spot.LightColor = color;
			light.Spot.Radius = radius;
		}
	}

	private float GetVenueLightPulseSpeed()
	{
		return EventPhase switch
		{
			TapperEventPhase.Countdown => 8.5f,
			TapperEventPhase.Live => 5.2f,
			TapperEventPhase.PhotoFinish => 10f,
			TapperEventPhase.Podium => 3.4f,
			_ => 2.2f
		};
	}

	private Color GetVenueLightPhaseColor()
	{
		return EventPhase switch
		{
			TapperEventPhase.ReadyCheck => new Color( 0.25f, 1f, 0.62f, 1f ),
			TapperEventPhase.Countdown => new Color( 1f, 0.52f, 0.16f, 1f ),
			TapperEventPhase.Live => GetModeAccentColor(),
			TapperEventPhase.PhotoFinish => new Color( 1f, 0.18f, 0.12f, 1f ),
			TapperEventPhase.Podium => WinnerStationColor,
			TapperEventPhase.NextModePreview => new Color( 0.45f, 0.64f, 1f, 1f ),
			_ => ReadyStationColor
		};
	}

}
