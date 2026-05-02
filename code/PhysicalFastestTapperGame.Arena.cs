using Sandbox;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public sealed partial class PhysicalFastestTapperGame
{
	private void EnsureArena()
	{
		Stations.Clear();

		var stage = GetVenueStageOrigin();
		LogConstructDiagnostics( "OnStart.EnsureArena", VenueMapLoaded, HasVenueSceneMap() );
		Log.Info( $"[TapperConstruct] phase=Stage.Placement origin='{stage}' stations={Math.Clamp( StationCount, 1, 8 )} suppressGeneratedAmbient={VenueMapLoaded}" );
		CreateModelObject( "Arena Floor", stage + new Vector3( 0, 0, 8f ), VenueMapLoaded ? new Vector3( 24f, 18f, 0.34f ) : new Vector3( 46f, 46f, 0.42f ), "models/dev/box.vmdl", StageColor, false, false );
		CreateModelObject( "Leaderboard Tower", stage + new Vector3( 260f, 0, 265f ), new Vector3( 0.3f, 10f, 4.2f ), "models/dev/box.vmdl", new Color( 0.07f, 0.085f, 0.12f, 1f ), false, false );
		ArenaKeyGlow = CreateModelObject( "Arena Key Glow", stage + new Vector3( 246f, 0, 500f ), new Vector3( 18f, 0.2f, 2.8f ), "models/dev/box.vmdl", new Color( 0.08f, 0.12f, 0.18f, 1f ), false, false );
		ArenaKeyGlowRenderer = ArenaKeyGlow.GetComponent<ModelRenderer>();
		EnsureVenueWorld();

		TitleText = CreateTextObject( "Arena Title Text", stage + new Vector3( 238f, -360f, 438f ), 0.72f );
		TimerText = CreateTextObject( "Arena Timer Text", stage + new Vector3( 240f, -465f, 354f ), 0.72f );
		ModeText = CreateTextObject( "Arena Mode Text", stage + new Vector3( 240f, -220f, 354f ), 0.52f );
		LeaderboardText = CreateTextObject( "Arena Leaderboard Text", stage + new Vector3( 242f, -350f, 262f ), 0.42f );

		var count = Math.Clamp( StationCount, 1, 8 );
		var spacing = 360f;
		var startY = -(count - 1) * spacing * 0.5f;

		for ( var i = 0; i < count; i++ )
		{
			var origin = stage + new Vector3( 0f, startY + i * spacing, 0f );
			Stations.Add( CreateStation( i, origin ) );
		}
	}

	private Vector3 GetVenueStageOrigin()
	{
		return VenueMapLoaded ? new Vector3( 0f, -900f, 32f ) : Vector3.Zero;
	}

	private async Task LoadVenueMap()
	{
		try
		{
			VenueMapLoaded = false;
			VenueWorldStatus = UseConstructWorld ? "LOADING CONSTRUCT" : "GENERATED VENUE";
			LogConstructDiagnostics( "OnLoad.Start", false, false );

			if ( !UseConstructWorld || string.IsNullOrWhiteSpace( ConstructMapName ) )
			{
				VenueWorldStatus = ConstructMapLoadDiagnostics.FormatWorldStatus( UseConstructWorld, ConstructMapName, false, false, "", "" );
				LogConstructDiagnostics( "OnLoad.Skipped", false, false );
				return;
			}

			try
			{
				LoadingScreen.Title = "Loading Construct";
				if ( HasVenueSceneMap() )
					VenueSceneMap.Delete();

				LogConstructDiagnostics( "CreateAsync.Start", false, false );
				VenueSceneMap = await SceneMap.CreateAsync( Scene.SceneWorld, ConstructMapName, CancellationToken.None );
				VenueMapLoaded = HasVenueSceneMap();
				VenueWorldStatus = ConstructMapLoadDiagnostics.FormatWorldStatus( UseConstructWorld, ConstructMapName, VenueMapLoaded, HasVenueSceneMap(), GetVenueSceneMapName(), "" );
				LogConstructDiagnostics( "CreateAsync.Completed", VenueMapLoaded, HasVenueSceneMap() );
			}
			catch ( Exception exception )
			{
				VenueMapLoaded = false;
				VenueWorldStatus = ConstructMapLoadDiagnostics.FormatWorldStatus( UseConstructWorld, ConstructMapName, false, false, "", exception.GetType().Name );
				Log.Warning( BuildConstructDiagnostics( "CreateAsync.Failed", false, false, exception ).ToLogLine() );
			}
		}
		catch ( Exception exception )
		{
			VenueMapLoaded = false;
			VenueWorldStatus = ConstructMapLoadDiagnostics.FormatWorldStatus( UseConstructWorld, ConstructMapName, false, false, "", exception.GetType().Name );
			Log.Warning( $"[TapperConstruct] phase=OnLoad.Fatal exception='{exception.GetType().Name}' message='{exception.Message}'" );
		}
	}

	private ConstructMapLoadDiagnostics BuildConstructDiagnostics( string phase, bool loaded, bool isValid, Exception exception = null )
	{
		return new ConstructMapLoadDiagnostics(
			UseConstructWorld,
			ConstructMapName,
			phase,
			loaded,
			isValid,
			GetVenueSceneMapName(),
			GetVenueSceneMapFolder(),
			GetVenueSceneMapBoundsText(),
			exception?.GetType().Name ?? "",
			exception?.Message ?? "" );
	}

	private void LogConstructDiagnostics( string phase, bool loaded, bool isValid )
	{
		try
		{
			Log.Info( BuildConstructDiagnostics( phase, loaded, isValid ).ToLogLine() );
		}
		catch ( Exception exception )
		{
			Log.Warning( $"[TapperConstruct] phase={phase}.DiagnosticsFailed exception='{exception.GetType().Name}' message='{exception.Message}'" );
		}
	}

	private bool HasVenueSceneMap()
	{
		return VenueSceneMap is not null && VenueSceneMap.IsValid;
	}

	private string GetVenueSceneMapName()
	{
		return HasVenueSceneMap() ? VenueSceneMap.MapName ?? "" : "";
	}

	private string GetVenueSceneMapFolder()
	{
		return HasVenueSceneMap() ? VenueSceneMap.MapFolder ?? "" : "";
	}

	private string GetVenueSceneMapBoundsText()
	{
		return HasVenueSceneMap() ? VenueSceneMap.Bounds.ToString() : "";
	}

	private TapperStation CreateStation( int index, Vector3 origin )
	{
		var station = new TapperStation
		{
			Index = index,
			Origin = origin,
			Root = FindOrCreate( $"Station {index} Root" )
		};

		station.FloorMarker = CreateModelObject( $"Station {index} Floor Marker", origin + new Vector3( 0f, 0f, 6f ), new Vector3( 4.8f, 4.8f, 0.05f ), "models/dev/box.vmdl", OpenStationColor, false, false );
		station.FloorMarkerRenderer = station.FloorMarker.GetComponent<ModelRenderer>();
		station.WinnerGlow = CreateModelObject( $"Station {index} Winner Glow", origin + new Vector3( 0f, 0f, 10f ), new Vector3( 2.6f, 2.6f, 0.08f ), "models/dev/box.vmdl", new Color( 0.04f, 0.05f, 0.06f, 1f ), false, false );
		station.WinnerGlowRenderer = station.WinnerGlow.GetComponent<ModelRenderer>();
		station.FocusRing = CreateModelObject( $"Station {index} Focus Ring", origin + new Vector3( 0f, 0f, 13f ), new Vector3( 3.4f, 3.4f, 0.04f ), "models/dev/box.vmdl", new Color( 0.08f, 0.16f, 0.19f, 1f ), false, false );
		station.FocusRingRenderer = station.FocusRing.GetComponent<ModelRenderer>();

		CreateModelObject( $"Station {index} Panel", origin + new Vector3( 42f, 86f, 236f ), new Vector3( 6.2f, 0.18f, 2.05f ), "models/dev/box.vmdl", PanelColor, false, false );
		DestroyLegacyStationAvatarObjects( index );
		CreateModelObject( $"Station {index} Pedestal", origin + new Vector3( 0f, 0f, 34f ), new Vector3( 4.4f, 4.4f, 0.58f ), "models/dev/box.vmdl", new Color( 0.15f, 0.18f, 0.24f, 1f ), false, true );
		station.ReadyLight = CreateModelObject( $"Station {index} Ready Light", origin + new Vector3( -116f, 208f, 330f ), new Vector3( 0.45f, 0.45f, 0.08f ), "models/dev/sphere.vmdl", OpenStationColor, false, false );
		station.ReadyLightRenderer = station.ReadyLight.GetComponent<ModelRenderer>();

		station.Button = CreateModelObject( $"Station {index} Physical Tap Button", origin + new Vector3( 0f, 0f, 84f ), new Vector3( 2.05f, 2.05f, 0.42f ), "models/dev/box.vmdl", IdleButtonColor, false, true );
		var tapButton = station.Button.Components.GetOrCreate<PhysicalTapButton>();
		tapButton.StationIndex = index;
		station.ButtonRenderer = station.Button.GetComponent<ModelRenderer>();

		station.ButtonTop = CreateModelObject( $"Station {index} Button Top", origin + new Vector3( 0f, 0f, 114f ), new Vector3( 1.55f, 1.55f, 0.1f ), "models/dev/box.vmdl", new Color( 1f, 0.22f, 0.16f, 1f ), false, false );
		station.ButtonTopRenderer = station.ButtonTop.GetComponent<ModelRenderer>();
		station.ButtonHitbox = CreateButtonHitbox( index, origin + new Vector3( 0f, 0f, 128f ) );

		CreateModelObject( $"Station {index} Progress Track", origin + new Vector3( 34f, 86f, 174f ), new Vector3( 4.9f, 0.18f, 0.11f ), "models/dev/box.vmdl", new Color( 0.2f, 0.23f, 0.29f, 1f ), false, false );
		station.ProgressFill = CreateModelObject( $"Station {index} Progress Fill", origin + new Vector3( 34f, 86f, 178f ), new Vector3( 4.9f, 0.22f, 0.12f ), "models/dev/box.vmdl", new Color( 0.2f, 0.82f, 1f, 1f ), false, false );
		CreateModelObject( $"Station {index} Heat Track", origin + new Vector3( 34f, 86f, 154f ), new Vector3( 4.9f, 0.18f, 0.11f ), "models/dev/box.vmdl", new Color( 0.2f, 0.23f, 0.29f, 1f ), false, false );
		station.HeatFill = CreateModelObject( $"Station {index} Heat Fill", origin + new Vector3( 34f, 86f, 158f ), new Vector3( 4.9f, 0.22f, 0.12f ), "models/dev/box.vmdl", new Color( 0.15f, 0.65f, 1f, 1f ), false, false );
		station.HeatFillRenderer = station.HeatFill.GetComponent<ModelRenderer>();
		CreateModelObject( $"Station {index} Race Trace Track", origin + new Vector3( 34f, 86f, 134f ), new Vector3( 4.9f, 0.16f, 0.08f ), "models/dev/box.vmdl", new Color( 0.16f, 0.17f, 0.21f, 1f ), false, false );
		station.RaceTraceFill = CreateModelObject( $"Station {index} Race Trace Fill", origin + new Vector3( 34f, 86f, 138f ), new Vector3( 4.9f, 0.2f, 0.09f ), "models/dev/box.vmdl", new Color( 1f, 0.84f, 0.18f, 1f ), false, false );
		station.RaceTraceFillRenderer = station.RaceTraceFill.GetComponent<ModelRenderer>();

		station.StationNumberText = CreateTextObject( $"Station {index} Number Text", origin + new Vector3( -122f, 230f, 342f ), 0.36f );
		station.NameText = CreateTextObject( $"Station {index} Name Text", origin + new Vector3( -28f, 230f, 326f ), 0.46f );
		station.ScoreText = CreateTextObject( $"Station {index} Score Text", origin + new Vector3( 26f, 86f, 268f ), 0.76f );
		station.SpeedText = CreateTextObject( $"Station {index} Speed Text", origin + new Vector3( -90f, 86f, 262f ), 0.52f );
		station.ComboText = CreateTextObject( $"Station {index} Combo Text", origin + new Vector3( 118f, 86f, 262f ), 0.52f );
		station.RankText = CreateTextObject( $"Station {index} Rank Text", origin + new Vector3( -82f, 226f, 246f ), 0.48f );
		station.StatusText = CreateTextObject( $"Station {index} Status Text", origin + new Vector3( -16f, 8f, 142f ), 0.48f );

		station.ButtonBaseScale = station.Button.LocalScale;
		station.ButtonTopBasePosition = station.ButtonTop.LocalPosition;
		station.ProgressBaseScale = station.ProgressFill.LocalScale;
		station.ProgressBasePosition = station.ProgressFill.LocalPosition;
		station.HeatBaseScale = station.HeatFill.LocalScale;
		station.HeatBasePosition = station.HeatFill.LocalPosition;
		station.Sparks = CreateSparkObjects( index, origin );
		return station;
	}

	private void DestroyLegacyStationAvatarObjects( int stationIndex )
	{
		foreach ( var name in new[] { $"Station {stationIndex} Avatar Panel", $"Station {stationIndex} Avatar Head", $"Station {stationIndex} Winner Crown" } )
		{
			foreach ( var gameObject in Scene.Directory.FindByName( name ).ToArray() )
			{
				if ( gameObject.IsValid() )
					gameObject.Destroy();
			}
		}
	}

	private GameObject CreateButtonHitbox( int stationIndex, Vector3 position )
	{
		var gameObject = FindOrCreate( $"Station {stationIndex} Button Hitbox" );
		gameObject.LocalPosition = position;
		gameObject.LocalRotation = Rotation.Identity;
		gameObject.LocalScale = Vector3.One;

		var collider = gameObject.Components.GetOrCreate<BoxCollider>();
		collider.Scale = new Vector3( 190f, 190f, 110f );
		collider.Static = true;
		collider.IsTrigger = true;

		var tapButton = gameObject.Components.GetOrCreate<PhysicalTapButton>();
		tapButton.StationIndex = stationIndex;
		return gameObject;
	}

	private void EnsureVenueWorld()
	{
		AmbientVenueObjects.Clear();
		var fallbackRoot = EnsureVenueFallbackRoot();
		fallbackRoot.Enabled = true;

		if ( VenueMapLoaded )
		{
			CreateConstructStageDressing();
			CreateVenuePodium();
			return;
		}

		CreateVenueBackdrop();
		CreateVenueBoundaries();
		CreateVenueTrussAndSignage();
		CreateVenueProps();
		CreateVenueSpectators();
		CreateVenuePodium();
	}

	private void CreateConstructStageDressing()
	{
		var stage = GetVenueStageOrigin();
		CreateModelObject( "Construct Tapper Stage Back Rail", stage + new Vector3( 160f, 0f, 44f ), new Vector3( 0.22f, 9f, 0.24f ), "models/dev/box.vmdl", VenueTrussColor, false, false );
		CreateModelObject( "Construct Tapper Stage Left Rail", stage + new Vector3( -120f, -650f, 32f ), new Vector3( 5.6f, 0.12f, 0.2f ), "models/dev/box.vmdl", VenueTrussColor, false, false );
		CreateModelObject( "Construct Tapper Stage Right Rail", stage + new Vector3( -120f, 650f, 32f ), new Vector3( 5.6f, 0.12f, 0.2f ), "models/dev/box.vmdl", VenueTrussColor, false, false );
		CreateModelObject( "Construct Tapper Anchor Glow", stage + new Vector3( 52f, 0f, 18f ), new Vector3( 6.8f, 5.6f, 0.04f ), "models/dev/box.vmdl", new Color( 0.04f, 0.16f, 0.2f, 1f ), false, false );
		CreateModelObject( "Construct Tapper Header Backplate", stage + new Vector3( 168f, 0f, 378f ), new Vector3( 0.22f, 4.8f, 0.56f ), "models/dev/box.vmdl", new Color( 0.035f, 0.05f, 0.075f, 1f ), false, false );

		var sign = CreateTextObject( "Construct Tapper Header Text", stage + new Vector3( 160f, -130f, 410f ), 0.58f );
		SetText( sign, "ULTIMATE TAPPER" );
	}

	private void CreateVenueBackdrop()
	{
		CreateOfficeShell();
		CreateFallbackModelObject( "Venue Backdrop Wall", new Vector3( 520f, 0f, 310f ), new Vector3( 0.45f, 28f, 5.8f ), "models/dev/box.vmdl", VenueBackdropColor, false, false );
		CreateFallbackModelObject( "Venue High Sky Panel", new Vector3( 390f, 0f, 690f ), new Vector3( 20f, 28f, 0.45f ), "models/dev/box.vmdl", new Color( 0.025f, 0.035f, 0.055f, 1f ), false, false );
		CreateFallbackModelObject( "Venue Horizon Glow", new Vector3( 506f, 0f, 478f ), new Vector3( 0.16f, 20f, 0.45f ), "models/dev/box.vmdl", new Color( 0.06f, 0.18f, 0.23f, 1f ), false, false );
	}

	private void CreateOfficeShell()
	{
		CreateFallbackModelObject( "Venue Office Rear Wall", new Vector3( 650f, 0f, 320f ), new Vector3( 0.55f, 38f, 7.2f ), "models/dev/box.vmdl", new Color( 0.105f, 0.115f, 0.125f, 1f ), false, false );
		CreateFallbackModelObject( "Venue Office Left Wall", new Vector3( 70f, -2050f, 300f ), new Vector3( 27f, 0.55f, 6.4f ), "models/dev/box.vmdl", new Color( 0.095f, 0.105f, 0.115f, 1f ), false, false );
		CreateFallbackModelObject( "Venue Office Right Wall", new Vector3( 70f, 2050f, 300f ), new Vector3( 27f, 0.55f, 6.4f ), "models/dev/box.vmdl", new Color( 0.095f, 0.105f, 0.115f, 1f ), false, false );
		CreateFallbackModelObject( "Venue Office Drop Ceiling", new Vector3( 0f, 0f, 690f ), new Vector3( 30f, 38f, 0.24f ), "models/dev/box.vmdl", new Color( 0.17f, 0.18f, 0.18f, 1f ), false, false );

		for ( var i = 0; i < 7; i++ )
		{
			var y = -1260f + i * 420f;
			CreateFallbackModelObject( $"Venue Office Fluorescent Panel {i:00}", new Vector3( -220f, y, 674f ), new Vector3( 2.6f, 0.85f, 0.08f ), "models/dev/box.vmdl", new Color( 0.64f, 0.9f, 1f, 1f ), false, false );
			CreateFallbackModelObject( $"Venue Office Ceiling Tile Seam {i:00}", new Vector3( 25f, y + 210f, 676f ), new Vector3( 28f, 0.035f, 0.05f ), "models/dev/box.vmdl", new Color( 0.045f, 0.05f, 0.055f, 1f ), false, false );
		}

		for ( var i = 0; i < 6; i++ )
		{
			var x = -560f + i * 220f;
			CreateFallbackModelObject( $"Venue Office Rear Window {i:00}", new Vector3( 642f, -1040f + i * 416f, 375f ), new Vector3( 0.08f, 1.35f, 1.25f ), "models/dev/box.vmdl", new Color( 0.035f, 0.13f, 0.19f, 1f ), false, false );
			CreateFallbackModelObject( $"Venue Office Floor Lane Line {i:00}", new Vector3( x, 0f, 9f ), new Vector3( 0.06f, 28f, 0.045f ), "models/dev/box.vmdl", new Color( 0.18f, 0.2f, 0.22f, 1f ), false, false );
		}
	}

	private void CreateVenueBoundaries()
	{
		for ( var i = 0; i < 5; i++ )
		{
			var x = -600f + i * 300f;
			CreateFallbackModelObject( $"Venue Asset Left Rail {i:00}", new Vector3( x, -1650f, 18f ), new Vector3( 2.8f, 0.16f, 0.22f ), "models/dev/box.vmdl", VenueTrussColor, false, false );
			CreateFallbackModelObject( $"Venue Asset Right Rail {i:00}", new Vector3( x, 1650f, 18f ), new Vector3( 2.8f, 0.16f, 0.22f ), "models/dev/box.vmdl", VenueTrussColor, false, false );
		}

		CreateFallbackModelObject( "Venue Left Back Wall", new Vector3( 300f, -1780f, 190f ), new Vector3( 10f, 0.24f, 2.4f ), "models/dev/box.vmdl", VenueWallColor, false, false );
		CreateFallbackModelObject( "Venue Right Back Wall", new Vector3( 300f, 1780f, 190f ), new Vector3( 10f, 0.24f, 2.4f ), "models/dev/box.vmdl", VenueWallColor, false, false );
	}

	private void CreateVenueTrussAndSignage()
	{
		CreateModelObject( "Venue Overhead Truss Main", new Vector3( 180f, 0f, 600f ), new Vector3( 11f, 0.22f, 0.22f ), "models/dev/box.vmdl", VenueTrussColor, false, false );
		CreateModelObject( "Venue Overhead Truss Left", new Vector3( 180f, -1550f, 420f ), new Vector3( 0.22f, 0.22f, 3.6f ), "models/dev/box.vmdl", VenueTrussColor, false, false );
		CreateModelObject( "Venue Overhead Truss Right", new Vector3( 180f, 1550f, 420f ), new Vector3( 0.22f, 0.22f, 3.6f ), "models/dev/box.vmdl", VenueTrussColor, false, false );

		CreateFallbackModelObject( "Venue Header Sign Backplate", new Vector3( 145f, 0f, 520f ), new Vector3( 4.8f, 0.2f, 0.72f ), "models/dev/box.vmdl", new Color( 0.035f, 0.05f, 0.075f, 1f ), false, false );

		RegisterAmbientVenueObject( CreateModelObject( "Venue Header Sign Glow", new Vector3( 140f, 0f, 524f ), new Vector3( 4.5f, 0.12f, 0.1f ), "models/dev/box.vmdl", VenueSignColor, false, false ), AmbientVenueRole.Sign, 0f, VenueSignColor );

		var sign = CreateTextObject( "Venue Header Sign Text", new Vector3( 145f, 0f, 560f ), 0.78f );
		SetText( sign, "ULTIMATE TAPPER" );
	}

	private void CreateVenueProps()
	{
		var lightColor = new Color( 0.5f, 0.78f, 0.9f, 1f );
		RegisterAmbientVenueObject( CreateFallbackModelObject( "Venue Asset Ceiling Light Left", new Vector3( -120f, -900f, 520f ), new Vector3( 1.4f, 0.35f, 0.08f ), "models/dev/box.vmdl", lightColor, false, false ), AmbientVenueRole.Light, 0.2f, lightColor );
		RegisterAmbientVenueObject( CreateFallbackModelObject( "Venue Asset Ceiling Light Center", new Vector3( -120f, 0f, 540f ), new Vector3( 1.4f, 0.35f, 0.08f ), "models/dev/box.vmdl", lightColor, false, false ), AmbientVenueRole.Light, 1.6f, lightColor );
		RegisterAmbientVenueObject( CreateFallbackModelObject( "Venue Asset Ceiling Light Right", new Vector3( -120f, 900f, 520f ), new Vector3( 1.4f, 0.35f, 0.08f ), "models/dev/box.vmdl", lightColor, false, false ), AmbientVenueRole.Light, 3.1f, lightColor );

		for ( var side = -1; side <= 1; side += 2 )
		{
			var y = side * 1510f;
			var sign = side < 0 ? "Left" : "Right";

			CreateFallbackModelObject( $"Venue Speaker Stack {side} Base", new Vector3( 210f, y, 86f ), new Vector3( 0.85f, 0.55f, 1.15f ), "models/dev/box.vmdl", VenuePropColor, false, false );
			CreateFallbackModelObject( $"Venue Speaker Stack {side} Top", new Vector3( 210f, y, 204f ), new Vector3( 0.72f, 0.48f, 0.9f ), "models/dev/box.vmdl", VenuePropColor, false, false );
			RegisterAmbientVenueObject( CreateFallbackModelObject( $"Venue Light Rig {sign}", new Vector3( -140f, y, 430f ), new Vector3( 0.34f, 0.34f, 0.34f ), "models/dev/sphere.vmdl", new Color( 0.1f, 0.32f, 0.4f, 1f ), false, false ), AmbientVenueRole.Celebration, side < 0 ? 0.75f : 2.25f, new Color( 0.1f, 0.32f, 0.4f, 1f ) );
			CreateFallbackModelObject( $"Venue Asset {sign} CCTV", new Vector3( 420f, y * 0.86f, 310f ), new Vector3( 0.28f, 0.2f, 0.2f ), "models/dev/box.vmdl", new Color( 0.035f, 0.04f, 0.05f, 1f ), false, false );
			CreateFallbackModelObject( $"Venue Asset {sign} Fire Extinguisher", new Vector3( 410f, y * 0.72f, 30f ), new Vector3( 0.12f, 0.12f, 0.42f ), "models/dev/box.vmdl", new Color( 0.7f, 0.05f, 0.04f, 1f ), false, false );
			CreateFallbackModelObject( $"Venue Asset {sign} Ash Bin", new Vector3( 345f, y * 0.62f, 18f ), new Vector3( 0.34f, 0.34f, 0.36f ), "models/dev/box.vmdl", VenuePropColor, false, false );
		}
	}

	private void CreateVenueSpectators()
	{
		for ( var i = 0; i < 14; i++ )
		{
			var y = -1260f + i * 194f;
			var height = 0.58f + (i % 3) * 0.08f;
			RegisterAmbientVenueObject( CreateFallbackModelObject( $"Venue Crowd Silhouette {i:00}", new Vector3( 455f, y, 108f + height * 30f ), new Vector3( 0.18f, 0.32f, height ), "models/dev/box.vmdl", new Color( 0.025f, 0.028f, 0.036f, 1f ), false, false ), AmbientVenueRole.Crowd, i * 0.47f, new Color( 0.025f, 0.028f, 0.036f, 1f ) );
		}
	}

	private void CreateVenuePodium()
	{
		var stage = GetVenueStageOrigin();
		if ( PodiumPrefab.IsValid() )
		{
			var podium = FindOrCreate( "Venue Podium Prefab Instance" );
			if ( podium.Children.Count == 0 )
				PodiumPrefab.Clone( stage + new Vector3( 420f, 0f, 42f ) ).SetParent( podium, true );
			return;
		}

		CreateFallbackModelObject( "Venue Podium Base", stage + new Vector3( 430f, 0f, 28f ), new Vector3( 2.2f, 3.6f, 0.42f ), "models/dev/box.vmdl", new Color( 0.12f, 0.14f, 0.18f, 1f ), false, false );
		CreateFallbackModelObject( "Venue Podium Winner Step", stage + new Vector3( 410f, 0f, 68f ), new Vector3( 1.3f, 1.1f, 0.52f ), "models/dev/box.vmdl", new Color( 0.22f, 0.18f, 0.08f, 1f ), false, false );
		CreateFallbackModelObject( "Venue Podium Left Step", stage + new Vector3( 425f, -170f, 50f ), new Vector3( 1.0f, 0.9f, 0.34f ), "models/dev/box.vmdl", new Color( 0.11f, 0.13f, 0.16f, 1f ), false, false );
		CreateFallbackModelObject( "Venue Podium Right Step", stage + new Vector3( 425f, 170f, 50f ), new Vector3( 1.0f, 0.9f, 0.34f ), "models/dev/box.vmdl", new Color( 0.11f, 0.13f, 0.16f, 1f ), false, false );
		RegisterAmbientVenueObject( CreateFallbackModelObject( "Venue Podium Winner Lane", stage + new Vector3( 328f, 0f, 15f ), new Vector3( 2.6f, 0.42f, 0.08f ), "models/dev/box.vmdl", WinnerStationColor, false, false ), AmbientVenueRole.Celebration, 1.25f, WinnerStationColor );
	}

	private void RegisterAmbientVenueObject( GameObject gameObject, AmbientVenueRole role, float phase, Color baseColor )
	{
		if ( !gameObject.IsValid() )
			return;

		AmbientVenueObjects.Add( new AmbientVenueObject
		{
			GameObject = gameObject,
			Renderer = gameObject.GetComponent<ModelRenderer>(),
			BasePosition = gameObject.LocalPosition,
			BaseScale = gameObject.LocalScale,
			BaseColor = baseColor,
			Role = role,
			Phase = phase
		} );
	}

	private GameObject[] CreateSparkObjects( int stationIndex, Vector3 origin )
	{
		var sparks = new GameObject[10];
		for ( var i = 0; i < sparks.Length; i++ )
		{
			var spark = CreateModelObject( $"Station {stationIndex} Speed Spark {i:00}", origin + new Vector3( 0f, 0f, 120f ), Vector3.One * 0.08f, "models/dev/sphere.vmdl", new Color( 0.2f, 0.75f, 1f, 0.9f ), false, false );
			spark.Enabled = false;
			sparks[i] = spark;
		}
		return sparks;
	}

	private GameObject EnsureVenueFallbackRoot()
	{
		var root = FindOrCreate( "Venue Generated Fallback Root" );
		root.LocalPosition = Vector3.Zero;
		root.LocalRotation = Rotation.Identity;
		root.LocalScale = Vector3.One;
		root.Enabled = true;
		return root;
	}

	private GameObject CreateFallbackModelObject( string name, Vector3 position, Vector3 scale, string modelPath, Color tint, bool planeCollider, bool boxCollider )
	{
		var gameObject = CreateModelObject( name, position, scale, modelPath, tint, planeCollider, boxCollider );
		gameObject.SetParent( EnsureVenueFallbackRoot(), true );
		return gameObject;
	}

	private GameObject CreateModelObject( string name, Vector3 position, Vector3 scale, string modelPath, Color tint, bool planeCollider, bool boxCollider )
	{
		var gameObject = FindOrCreate( name );
		gameObject.LocalPosition = position;
		gameObject.LocalScale = scale;

		var renderer = gameObject.Components.GetOrCreate<ModelRenderer>();
		renderer.Model = Model.Load( modelPath );
		renderer.Tint = tint;

		if ( planeCollider )
		{
			var collider = gameObject.Components.GetOrCreate<PlaneCollider>();
			collider.Scale = new Vector2( 100f, 100f );
			collider.Static = true;
		}

		if ( boxCollider )
		{
			var collider = gameObject.Components.GetOrCreate<BoxCollider>();
			collider.Scale = new Vector3( 50f, 50f, 50f );
			collider.Static = true;
		}

		return gameObject;
	}

	private TextRenderer CreateTextObject( string name, Vector3 position, float scale )
	{
		var gameObject = FindOrCreate( name );
		gameObject.LocalPosition = position;
		gameObject.LocalRotation = Rotation.FromYaw( 35f );
		gameObject.LocalScale = Vector3.One;

		var textRenderer = gameObject.Components.GetOrCreate<TextRenderer>();
		textRenderer.Scale = scale;
		textRenderer.Color = Color.White;
		return textRenderer;
	}
}
