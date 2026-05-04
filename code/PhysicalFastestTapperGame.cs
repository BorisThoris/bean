using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[Category( "Gameplay" ), Icon( "touch_app" )]
public sealed partial class PhysicalFastestTapperGame : Component, Component.INetworkListener
{
	[Property] public bool AutoCreateLobby { get; set; } = true;
	[Property, Range( 1, 8 )] public int StationCount { get; set; } = 4;
	[Property] public float GameDurationSeconds { get; set; } = 10f;
	[Property] public float CountdownSeconds { get; set; } = 3f;
	[Property] public float IntermissionSeconds { get; set; } = 5f;
	[Property] public float CooldownSeconds { get; set; } = 0.5f;
	[Property] public TapperGameMode GameMode { get; set; } = TapperGameMode.Classic10;
	[Property] public bool AutoRotateModes { get; set; } = true;
	[Property] public bool TournamentMode { get; set; } = true;
	[Property, Range( 1, 9 )] public int TournamentRounds { get; set; } = 5;
	[Property] public bool UseFinalTieBreaker { get; set; } = true;
	[Property] public bool DebugVisuals { get; set; } = false;
	[Property] public bool EnableFocusWindows { get; set; } = true;
	[Property] public bool UseConstructWorld { get; set; } = false;
	[Property] public string ConstructMapName { get; set; } = "facepunch.construct";
	[Property] public bool UseLiquidGlassFloor { get; set; } = true;
	[Property] public string LiquidGlassFloorMaterialPath { get; set; } = "materials/floor/liquid_glass_floor.vmat";
	[Property] public string LiquidGlassFloorDiagnosticMaterialPath { get; set; } = "materials/projection/endless_projection_tile.vmat";
	[Property] public bool ForceLiquidGlassFloorDiagnosticMaterial { get; set; } = false;
	[Property] public float LiquidGlassFloorHeightAboveFloor { get; set; } = 5f;
	[Property] public Color LiquidGlassFloorTint { get; set; } = Color.White;
	[Property] public bool UseProjectionSphere { get; set; } = true;
	[Property] public float ProjectionSphereRadiusPadding { get; set; } = 900f;
	[Property] public float ProjectionSphereURepeat { get; set; } = 1f;
	[Property] public float ProjectionSphereVRepeat { get; set; } = 1f;
	[Property] public Color ProjectionSphereTint { get; set; } = Color.White;
	[Property] public string ProjectionSkyMaterialPath { get; set; } = "materials/projection/endless_projection_tile.vmat";
	[Property] public float ProjectionSphereRotationSpeed { get; set; } = 18f;
	[Property] public float ProjectionSpherePitchTilt { get; set; } = 8f;
	[Property] public Color ProjectionTopLightColor { get; set; } = new( 0.78f, 0.9f, 1f, 1f );
	[Property] public float ProjectionTopLightRadius { get; set; } = 5200f;
	[Property] public float ProjectionTopLightIntensity { get; set; } = 2.8f;
	[Property] public bool EnableSphereDayNightCycle { get; set; } = false;
	[Property] public bool StartProjectionCycleAtNight { get; set; } = true;
	[Property] public float NightSkyMinimumVisibility { get; set; } = 0.72f;
	[Property] public float SphereDayLengthSeconds { get; set; } = 45f;
	[Property] public float SphereNightLengthSeconds { get; set; } = 30f;
	[Property] public bool EnablePixelClothesProjection { get; set; } = false;
	[Property, Range( 0, 24 )] public int PixelClothesCount { get; set; } = 16;
	[Property] public float PixelClothesOrbitSpeed { get; set; } = 0.32f;
	[Property] public bool EnableSphereStars { get; set; } = false;
	[Property, Range( 0, 240 )] public int SphereStarCount { get; set; } = 180;
	[Property] public float StarVisualScale { get; set; } = 2.2f;
	[Property] public float StarGlowScale { get; set; } = 4.4f;
	[Property] public float StarTwinkleSpeed { get; set; } = 3.2f;
	[Property] public float SunOrbitRadiusScale { get; set; } = 0.72f;
	[Property] public float SunLightRadius { get; set; } = 2600f;
	[Property] public Color SunDayColor { get; set; } = new( 1f, 0.72f, 0.2f, 1f );
	[Property] public bool EnableProjectionMoon { get; set; } = false;
	[Property] public float MoonOrbitRadiusScale { get; set; } = 0.68f;
	[Property] public float MoonLightRadius { get; set; } = 3400f;
	[Property] public float MoonLightIntensityScale { get; set; } = 2.2f;
	[Property] public float MoonGlowScale { get; set; } = 3.2f;
	[Property] public Color MoonNightColor { get; set; } = new( 0.92f, 0.96f, 1f, 1f );
	[Property] public bool EnableShootingStars { get; set; } = false;
	[Property, Range( 0, 12 )] public int ShootingStarCount { get; set; } = 5;
	[Property] public float ShootingStarIntervalSeconds { get; set; } = 1.35f;
	[Property] public float ShootingStarSpeed { get; set; } = 0.78f;
	[Property] public float ShootingStarTrailLength { get; set; } = 5.2f;
	[Property] public float ShootingStarLightRadius { get; set; } = 1150f;
	[Property] public Color ShootingStarColor { get; set; } = new( 0.86f, 0.95f, 1f, 1f );

	private static readonly Color IdleButtonColor = new( 0.92f, 0.08f, 0.07f, 1f );
	private static readonly Color HotButtonColor = new( 1f, 0.76f, 0.05f, 1f );
	private static readonly Color ReadyStationColor = new( 0.2f, 0.82f, 1f, 1f );
	private static readonly Color WinnerStationColor = new( 1f, 0.84f, 0.18f, 1f );
	private static readonly Color ClaimFrameIdleColor = new( 0.18f, 0.72f, 1f, 1f );
	private static readonly Color ClaimFrameActiveColor = new( 0.2f, 1f, 0.42f, 1f );
	private static readonly Color VenueWallColor = new( 0.13f, 0.145f, 0.165f, 1f );

	private readonly List<TapperStation> Stations = new();
	private readonly List<PlayerScore> Players = new();
	private readonly Dictionary<string, PlayerScore> PlayersByConnection = new();

	private RoundState State = RoundState.WaitingForPlayers;
	private float StateTimeLeft;
	private float RoundTimeLeft;
	private float LastLocalPressTime = -1f;
	private int LastWinnerStation = -1;
	private float RoundDuration;
	private int LastCountdownSecond = -1;
	private int TournamentRound = 1;
	private TapperEventPhase EventPhase = TapperEventPhase.Warmup;

	private Sandbox.ui.ArenaWallScreen WallScreen;
	private ArenaWallFallbackText WallFallbackText;
	private float ThirdPersonCameraYaw = 35f;
	private float ThirdPersonCameraPitch = 18f;
	private float BeanCameraDistance = ThirdPersonCameraDistanceDefault;
	private SceneMap VenueSceneMap;
	private bool VenueMapLoaded;
	private string VenueWorldStatus = "GENERATED FALLBACK";
	private Model LiquidGlassFloorModel;
	private float LiquidGlassFloorModelWidth;
	private float LiquidGlassFloorModelDepth;
	private string LiquidGlassFloorModelMaterialPath;
	private GameObject LiquidGlassFloorObject;
	private ModelRenderer LiquidGlassFloorRenderer;
	private Model ProjectionSphereModel;
	private float ProjectionSphereModelRadius;
	private float ProjectionSphereModelURepeat;
	private float ProjectionSphereModelVRepeat;
	private string ProjectionSphereModelMaterialPath;
	private Vector3 ProjectionSphereCenter;
	private float ProjectionSphereRadius;
	private GameObject ProjectionSphereObject;
	private ModelRenderer ProjectionSphereRenderer;
	private Model ProjectionSunModel;
	private Model ProjectionMoonModel;
	private Model ProjectionStarModel;
	private Model ProjectionStarGlowModel;
	private Model ProjectionShootingStarModel;
	private readonly Dictionary<int, Model> ProjectionClothesModels = new();
	private readonly List<ProjectionSphereVisual> ProjectionStars = new();
	private readonly List<ProjectionSphereVisual> ProjectionClothes = new();
	private readonly List<ProjectionShootingStarVisual> ProjectionShootingStars = new();
	private GameObject ProjectionTopLightObject;
	private GameObject ProjectionTopLightMarkerObject;
	private ModelRenderer ProjectionTopLightMarkerRenderer;
	private PointLight ProjectionTopLight;
	private GameObject ProjectionSunObject;
	private ModelRenderer ProjectionSunRenderer;
	private PointLight ProjectionSunLight;
	private GameObject ProjectionMoonObject;
	private GameObject ProjectionMoonGlowObject;
	private ModelRenderer ProjectionMoonRenderer;
	private ModelRenderer ProjectionMoonGlowRenderer;
	private PointLight ProjectionMoonLight;
	private float NextShootingStarTime;
	private float ProjectionCycleStartTime;

	[Sync] private int SyncedRoundState { get; set; }
	[Sync] private float SyncedStateTimeLeft { get; set; }
	[Sync] private float SyncedRoundTimeLeft { get; set; }
	[Sync] private int SyncedWinnerStation { get; set; } = -1;
	[Sync] private int SyncedGameMode { get; set; }
	[Sync] private int SyncedTournamentRound { get; set; } = 1;
	[Sync] private int SyncedEventPhase { get; set; }
	[Sync] private int SyncedStationCapacity { get; set; } = 4;
	[Sync] private string SyncedResultOrder { get; set; } = "";
	[Sync] private NetDictionary<string, int> SyncedScores { get; set; } = new();
	[Sync] private NetDictionary<string, int> SyncedStations { get; set; } = new();
	[Sync] private NetDictionary<string, bool> SyncedReady { get; set; } = new();
	[Sync] private NetDictionary<string, int> SyncedTournamentPoints { get; set; } = new();
	[Sync] private NetDictionary<string, int> SyncedFocusHits { get; set; } = new();
	[Sync] private NetDictionary<string, string> SyncedNames { get; set; } = new();
	[Sync] private NetDictionary<string, float> SyncedHeat { get; set; } = new();
	[Sync] private NetDictionary<string, string> SyncedLookDirections { get; set; } = new();

	protected override async Task OnLoad()
	{
		await LoadVenueMap();

		if ( Scene.IsEditor )
			return;

		if ( AutoCreateLobby && !Networking.IsActive )
		{
			LoadingScreen.Title = "Creating Tapper Lobby";
			await Task.DelayRealtimeSeconds( 0.1f );
			Networking.CreateLobby( new() );
		}
	}

	protected override void OnStart()
	{
		try
		{
			ConfigureCamera();
			EnsureArena();
			EnsureLocalFallbackPlayer();
			EnsurePlayerBeans();
			EnterWaiting();
		}
		catch ( Exception exception )
		{
			VenueMapLoaded = false;
			VenueWorldStatus = ConstructMapLoadDiagnostics.FormatWorldStatus( UseConstructWorld, ConstructMapName, false, false, "", exception.GetType().Name );
			Log.Warning( BuildConstructDiagnostics( "OnStart.Failed", false, false, exception ).ToLogLine() );
			throw;
		}
	}

	protected override void OnUpdate()
	{
		ApplyCursorState();
		if ( IsAuthoritativeInstance() )
		{
			EnsureKnownConnections();
			EnsureStationCapacityForLobby();
			HandleLocalFallbackInput();
			UpdateRoundFlow();
			PublishNetworkState();
		}
		else
		{
			ApplySyncedRoundState();
			HandleLocalFallbackInput();
		}

		EnsurePlayerBeans();
		UpdateVisuals();
		UpdateCentralDisplays();
	}

	public void OnActive( Connection channel )
	{
		EnsurePlayer( channel );
	}

	public void PressPhysicalButton()
	{
		PressPhysicalButton( GetLocalStationIndex() );
	}

	public void PressPhysicalButton( int stationIndex )
	{
		if ( Time.Now == LastLocalPressTime )
			return;

		LastLocalPressTime = Time.Now;

		if ( Networking.IsActive )
		{
			RequestStationPressHost( stationIndex );
			return;
		}

		TryHandleStationPress( stationIndex );
	}

	[Rpc.Host]
	private void RequestStationPressHost( int stationIndex )
	{
		if ( !TryHandleStationPress( stationIndex ) )
			return;

		BroadcastStationPressFeedback( stationIndex );
	}

	[Rpc.Broadcast]
	private void BroadcastStationPressFeedback( int stationIndex )
	{
		var station = Stations.FirstOrDefault( x => x.Index == stationIndex );
		if ( station is null )
			return;

		station.ButtonPunch = Math.Max( station.ButtonPunch, 0.6f );
	}

}
