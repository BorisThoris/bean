using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public sealed partial class PhysicalFastestTapperGame
{
	private const string QuaterniusModelRoot = "models/quaternius/modular_sci_fi/";
	private const string ArenaFloorModel = QuaterniusModelRoot + "platform_metal.vmdl";
	private const string StationPedestalModel = QuaterniusModelRoot + "platform_simple.vmdl";
	private const string TapperButtonModel = QuaterniusModelRoot + "prop_barrel_large.vmdl";
	private const string StationBarTrackModel = QuaterniusModelRoot + "decal_line_straight.vmdl";
	private const string StationBarFillModel = QuaterniusModelRoot + "decal_line_straight.vmdl";
	private const string WallPanelModel = QuaterniusModelRoot + "wallband_straight.vmdl";
	private const float DevLayoutUnitSize = 50f;
	private const float ArcadeTileSize = 260f;
	private const float ArcadeWallBaySize = 300f;

	private readonly Dictionary<string, ModelMetrics> ModelMetricsByPath = new();
	private RuntimeRoomLayout CurrentRoomLayout = RuntimeRoomLayoutMath.Build( 4 );
	private int CurrentGeneratedStationCount = -1;

	private enum ModelPlacementAnchor
	{
		Center,
		Floor,
		Ceiling
	}

	private readonly struct ModelMetrics
	{
		public readonly Model Model;
		public readonly Vector3 Size;
		public readonly Vector3 Center;

		public ModelMetrics( Model model, Vector3 size, Vector3 center )
		{
			Model = model;
			Size = size;
			Center = center;
		}
	}

	private void EnsureArena()
	{
		RebuildArenaForStationCapacity( GetDesiredStationCapacity() );
	}

	private void RebuildArenaForStationCapacity( int stationCapacity )
	{
		Stations.Clear();

		var stage = GetVenueStageOrigin();
		var layout = BuildArenaLayout( stationCapacity );
		CurrentGeneratedStationCount = layout.StationCount;
		ClearGeneratedArcadeDressing();
		LogConstructDiagnostics( "OnStart.EnsureArena", VenueMapLoaded, HasVenueSceneMap() );
		Log.Info( $"[TapperConstruct] phase=Stage.Placement origin='{stage}' stations={layout.StationCount} stationSpanY={layout.StationSpanY} floor='{layout.FloorWidth}x{layout.FloorDepth}' suppressGeneratedAmbient={VenueMapLoaded}" );
		CreateArcadeFloor( stage, layout );
		EnsureVenueWorld();
		CreateArenaWallScreen( stage, layout );

		for ( var i = 0; i < layout.StationCount; i++ )
		{
			var origin = stage + new Vector3( 0f, layout.StationY( i ), 0f );
			Stations.Add( CreateStation( i, origin ) );
		}

		DestroyUnusedStations( layout.StationCount );
	}

	private void ClearGeneratedArcadeDressing()
	{
		foreach ( var prefix in new[] { "Arena Floor", "Arena Lane Strip", "Arena Key Glow", "Arena Wall Screen", "Arena Wall Fallback", "Arena Title Text", "Arena Timer Text", "Arena Mode Text", "Arena Leaderboard Text", "Arena Wall Station Row", "Leaderboard Tower", "Venue Wall Bay", "Venue Ceiling Bay", "Venue Arcade Cabinet", "Venue Accent Column", "Venue Corner Glow", "Venue Office", "Venue Backdrop", "Venue High Sky", "Venue Asset", "Venue Speaker Stack", "Venue Light Rig", "Venue Header", "Venue Overhead", "Venue Left Back Wall", "Venue Right Back Wall", "Venue Podium", "Construct Tapper", "Arcade Key", "Arcade Warm", "Arcade Board", "Arcade Station", "Station Arcade", "Station 0 Speed Spark", "Station 1 Speed Spark", "Station 2 Speed Spark", "Station 3 Speed Spark", "Station 4 Speed Spark", "Station 5 Speed Spark", "Station 6 Speed Spark", "Station 7 Speed Spark" } )
		{
			foreach ( var gameObject in Scene.GetAllObjects( true ).Where( x => x.IsValid() && x.Name.StartsWith( prefix ) ).ToArray() )
				gameObject.Destroy();
		}
	}

	private void CreateArcadeFloor( Vector3 stage, RuntimeRoomLayout layout )
	{
		var xCount = Math.Max( 5, (int)MathF.Ceiling( layout.FloorWidth / ArcadeTileSize ) );
		var yCount = Math.Max( 5, (int)MathF.Ceiling( layout.FloorDepth / ArcadeTileSize ) );
		var startX = -layout.FloorWidth * 0.5f + ArcadeTileSize * 0.5f;
		var startY = -layout.FloorDepth * 0.5f + ArcadeTileSize * 0.5f;

		for ( var x = 0; x < xCount; x++ )
		{
			for ( var y = 0; y < yCount; y++ )
			{
				CreateModelObjectWorld( $"Arena Floor Tile {x:00}-{y:00}", stage + new Vector3( startX + x * ArcadeTileSize, startY + y * ArcadeTileSize, 0f ), new Vector3( ArcadeTileSize - 10f, ArcadeTileSize - 10f, layout.FloorThickness ), ArenaFloorModel, new Color( 0.16f, 0.175f, 0.19f, 1f ), false, true, ModelPlacementAnchor.Floor );
			}
		}

	}

	private void CreateArcadeWallBays( RuntimeRoomLayout layout )
	{
		var wallCenterZ = layout.WallHeight * 0.5f;
		var roomCenterX = (layout.RearWallX - layout.FloorWidth * 0.32f) * 0.5f;
		var rearCount = Math.Max( 6, (int)MathF.Ceiling( layout.FloorDepth / ArcadeWallBaySize ) );
		var rearStep = layout.FloorDepth / rearCount;

		for ( var i = 0; i < rearCount; i++ )
		{
			var y = layout.LeftWallY + rearStep * (i + 0.5f);
			CreateFallbackModelObjectWorld( $"Venue Wall Bay Rear {i:00}", new Vector3( layout.RearWallX, y, wallCenterZ ), new Vector3( 42f, rearStep - 18f, layout.WallHeight * 0.92f ), WallPanelModel, VenueWallColor, false, true );
		}

		var sideCount = Math.Max( 5, (int)MathF.Ceiling( layout.FloorWidth * 0.82f / ArcadeWallBaySize ) );
		var sideStartX = roomCenterX - layout.FloorWidth * 0.41f;
		var sideStep = layout.FloorWidth * 0.82f / sideCount;
		for ( var i = 0; i < sideCount; i++ )
		{
			var x = sideStartX + sideStep * (i + 0.5f);
			CreateFallbackModelObjectWorld( $"Venue Wall Bay Left {i:00}", new Vector3( x, layout.LeftWallY, wallCenterZ ), new Vector3( sideStep - 18f, 42f, layout.WallHeight * 0.9f ), WallPanelModel, VenueWallColor, false, true );
			CreateFallbackModelObjectWorld( $"Venue Wall Bay Right {i:00}", new Vector3( x, layout.RightWallY, wallCenterZ ), new Vector3( sideStep - 18f, 42f, layout.WallHeight * 0.9f ), WallPanelModel, VenueWallColor, false, true );
		}
	}

	private RuntimeRoomLayout BuildArenaLayout( int stationCapacity )
	{
		CurrentRoomLayout = RuntimeRoomLayoutMath.Build( stationCapacity );
		return CurrentRoomLayout;
	}

	private int GetDesiredStationCapacity()
	{
		return RuntimeRoomLayoutMath.ResolveStationCapacity( StationCount, Players.Count );
	}

	private void EnsureStationCapacityForLobby()
	{
		if ( State is RoundState.Countdown or RoundState.Playing )
			return;

		var desired = GetDesiredStationCapacity();
		if ( desired == CurrentGeneratedStationCount )
			return;

		RebuildArenaForStationCapacity( desired );
	}

	private void DestroyUnusedStations( int activeStationCount )
	{
		for ( var stationIndex = activeStationCount; stationIndex < 8; stationIndex++ )
		{
			foreach ( var gameObject in Scene.Directory.FindByName( $"Station {stationIndex} Root" ).ToArray() )
			{
				if ( gameObject.IsValid() )
					gameObject.Destroy();
			}

			var prefix = $"Station {stationIndex} ";
			foreach ( var gameObject in Scene.GetAllObjects( true ).Where( x => x.IsValid() && x.Name.StartsWith( prefix ) ).ToArray() )
				gameObject.Destroy();
		}
	}

	private Vector3 GetVenueStageOrigin()
	{
		return Vector3.Zero;
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

		DestroyLegacyStationAvatarObjects( index );
		CreateModelObjectWorld( $"Station {index} Pedestal", origin + new Vector3( 0f, 0f, 0f ), new Vector3( 360f, 280f, 8f ), StationPedestalModel, new Color( 0.22f, 0.24f, 0.27f, 1f ), false, true, ModelPlacementAnchor.Floor );
		CreateClaimFrame( station );

		station.Button = CreateModelObjectWorld( $"Station {index} Physical Tap Button", origin + new Vector3( 0f, -24f, 8f ), new Vector3( 86f, 86f, 48f ), TapperButtonModel, IdleButtonColor, false, true, ModelPlacementAnchor.Floor );
		var tapButton = station.Button.Components.GetOrCreate<PhysicalTapButton>();
		tapButton.StationIndex = index;
		station.ButtonRenderer = station.Button.GetComponent<ModelRenderer>();

		station.ButtonHitbox = CreateButtonHitbox( index, origin + new Vector3( 0f, -24f, 56f ) );

		CreateModelObject( $"Station {index} Progress Track", origin + new Vector3( 34f, 112f, 118f ), new Vector3( 4.9f, 0.18f, 0.11f ), StationBarTrackModel, new Color( 0.2f, 0.23f, 0.29f, 1f ), false, false );
		station.ProgressFill = CreateModelObject( $"Station {index} Progress Fill", origin + new Vector3( 34f, 112f, 122f ), new Vector3( 4.9f, 0.22f, 0.12f ), StationBarFillModel, new Color( 0.2f, 0.82f, 1f, 1f ), false, false );
		CreateModelObject( $"Station {index} Heat Track", origin + new Vector3( 34f, 112f, 98f ), new Vector3( 4.9f, 0.18f, 0.11f ), StationBarTrackModel, new Color( 0.2f, 0.23f, 0.29f, 1f ), false, false );
		station.HeatFill = CreateModelObject( $"Station {index} Heat Fill", origin + new Vector3( 34f, 112f, 102f ), new Vector3( 4.9f, 0.22f, 0.12f ), StationBarFillModel, new Color( 0.15f, 0.65f, 1f, 1f ), false, false );
		station.HeatFillRenderer = station.HeatFill.GetComponent<ModelRenderer>();

		station.ButtonBaseScale = station.Button.LocalScale;
		station.ProgressBaseScale = station.ProgressFill.LocalScale;
		station.ProgressBasePosition = station.ProgressFill.LocalPosition;
		station.HeatBaseScale = station.HeatFill.LocalScale;
		station.HeatBasePosition = station.HeatFill.LocalPosition;
		station.BarModelHalfExtentX = GetModelMetrics( StationBarFillModel ).Size.x * 0.5f;
		return station;
	}

	private void CreateClaimFrame( TapperStation station )
	{
		var origin = station.Origin;
		var z = 13f;
		var halfX = 196f;
		var halfY = 156f;
		var longSize = new Vector3( 392f, 10f, 6f );
		var shortSize = new Vector3( 312f, 10f, 6f );

		station.ClaimFrame = new[]
		{
			CreateModelObjectWorld( $"Station {station.Index} Claim Frame Front", origin + new Vector3( 0f, -halfY, z ), longSize, StationBarFillModel, ClaimFrameIdleColor, false, false ),
			CreateModelObjectWorld( $"Station {station.Index} Claim Frame Back", origin + new Vector3( 0f, halfY, z ), longSize, StationBarFillModel, ClaimFrameIdleColor, false, false ),
			CreateModelObjectWorld( $"Station {station.Index} Claim Frame Left", origin + new Vector3( -halfX, 0f, z ), shortSize, StationBarFillModel, ClaimFrameIdleColor, false, false, ModelPlacementAnchor.Center, Rotation.FromYaw( 90f ) ),
			CreateModelObjectWorld( $"Station {station.Index} Claim Frame Right", origin + new Vector3( halfX, 0f, z ), shortSize, StationBarFillModel, ClaimFrameIdleColor, false, false, ModelPlacementAnchor.Center, Rotation.FromYaw( 90f ) )
		};
		station.ClaimFrameRenderers = station.ClaimFrame.Select( x => x.GetComponent<ModelRenderer>() ).ToArray();
		station.ClaimFrameBaseScales = station.ClaimFrame.Select( x => x.LocalScale ).ToArray();
	}

	private void DestroyLegacyStationAvatarObjects( int stationIndex )
	{
		foreach ( var name in new[] { $"Station {stationIndex} Avatar Panel", $"Station {stationIndex} Avatar Head", $"Station {stationIndex} Winner Crown", $"Station {stationIndex} Number Text", $"Station {stationIndex} Name Text", $"Station {stationIndex} Status Text" } )
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
		var fallbackRoot = EnsureVenueFallbackRoot();
		fallbackRoot.Enabled = true;

		CreateVenueBackdrop();
	}

	private void CreateVenueBackdrop()
	{
		CreateOfficeShell();
	}

	private void CreateOfficeShell()
	{
		var layout = CurrentRoomLayout;
		CreateArcadeWallBays( layout );
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

	private GameObject CreateFallbackModelObjectWorld( string name, Vector3 position, Vector3 worldSize, string modelPath, Color tint, bool planeCollider, bool boxCollider, ModelPlacementAnchor anchor = ModelPlacementAnchor.Center )
	{
		var gameObject = CreateModelObjectWorld( name, position, worldSize, modelPath, tint, planeCollider, boxCollider, anchor );
		gameObject.SetParent( EnsureVenueFallbackRoot(), true );
		return gameObject;
	}

	private GameObject CreateModelObject( string name, Vector3 position, Vector3 layoutSize, string modelPath, Color tint, bool planeCollider, bool boxCollider )
	{
		return CreateModelObjectWorld( name, position, LayoutUnitsToWorldSize( layoutSize ), modelPath, tint, planeCollider, boxCollider );
	}

	private GameObject CreateModelObjectWorld( string name, Vector3 position, Vector3 desiredWorldSize, string modelPath, Color tint, bool planeCollider, bool boxCollider, ModelPlacementAnchor anchor = ModelPlacementAnchor.Center )
	{
		return CreateModelObjectWorld( name, position, desiredWorldSize, modelPath, tint, planeCollider, boxCollider, anchor, Rotation.Identity );
	}

	private GameObject CreateModelObjectWorld( string name, Vector3 position, Vector3 desiredWorldSize, string modelPath, Color tint, bool planeCollider, bool boxCollider, ModelPlacementAnchor anchor, Rotation rotation )
	{
		var metrics = GetModelMetrics( modelPath );
		var scale = ScaleForDesiredWorldSize( metrics.Size, desiredWorldSize );
		var anchorLocalPosition = GetModelAnchorLocalPosition( metrics, anchor );
		var anchorOffset = ComponentMultiply( anchorLocalPosition, scale );
		var gameObject = FindOrCreate( name );
		gameObject.LocalRotation = rotation;
		gameObject.LocalPosition = position - rotation * anchorOffset;
		gameObject.LocalScale = scale;

		var renderer = gameObject.Components.GetOrCreate<ModelRenderer>();
		renderer.Model = metrics.Model;
		renderer.Tint = tint;

		if ( planeCollider )
		{
			var collider = gameObject.Components.GetOrCreate<PlaneCollider>();
			collider.Scale = new Vector2( metrics.Size.x, metrics.Size.y );
			collider.Static = true;
		}

		if ( boxCollider )
		{
			var collider = gameObject.Components.GetOrCreate<BoxCollider>();
			collider.Scale = metrics.Size;
			collider.Static = true;
		}

		return gameObject;
	}

	private void CreateArenaWallScreen( Vector3 stage, RuntimeRoomLayout layout )
	{
		var screenLayout = ArenaWallScreenLayoutMath.Build( layout, stage.x, stage.y, stage.z );
		var screenCenter = new Vector3( screenLayout.ScreenX, screenLayout.ScreenY, screenLayout.ScreenZ );
		var facing = new Vector3( screenLayout.FacingX, screenLayout.FacingY, screenLayout.FacingZ );
		var rotation = Rotation.LookAt( facing, Vector3.Up );
		var displayRotation = Rotation.LookAt( facing, Vector3.Up );
		var screen = CreateModelObjectWorld( "Arena Wall Screen", screenCenter, new Vector3( ArenaWallScreenLayoutMath.ScreenModelThickness, screenLayout.ScreenWidth, screenLayout.ScreenHeight ), WallPanelModel, new Color( 0.025f, 0.032f, 0.045f, 1f ), false, false, ModelPlacementAnchor.Center, rotation );

		var uiObject = FindOrCreate( "Arena Wall Screen UI" );
		uiObject.LocalPosition = new Vector3( screenLayout.UiX, screenLayout.UiY, screenLayout.UiZ );
		uiObject.LocalRotation = displayRotation;
		uiObject.LocalScale = Vector3.One * screenLayout.UiScale;

		var worldPanel = uiObject.Components.GetOrCreate<WorldPanel>();
		worldPanel.PanelSize = new Vector2( screenLayout.CssWidth, screenLayout.CssHeight );
		worldPanel.RenderScale = 1f;
		worldPanel.InteractionRange = 0f;

		WallScreen = uiObject.Components.GetOrCreate<Sandbox.ui.ArenaWallScreen>();
		WallScreen.Game = this;
		CreateArenaWallFallbackText( screenLayout, displayRotation );
		SetWallFallbackVisible( ArenaWallScreenLayoutMath.ShouldShowFallback( WallScreen.IsValid() ) );
		var scaleRatio = uiObject.LocalScale.x / 100f;
		Log.Info( $"[TapperWallScreen] panelSize='{worldPanel.PanelSize}' renderScale={worldPanel.RenderScale:0.###} localScale='{uiObject.LocalScale}' scaleRatio={scaleRatio:0.###} displayForward='{displayRotation.Forward}' screen='{screenLayout.ScreenWidth:0.#}x{screenLayout.ScreenHeight:0.#}' fallback={ArenaWallScreenLayoutMath.ShouldShowFallback( WallScreen.IsValid() )}" );
	}

	private void CreateArenaWallFallbackText( ArenaWallScreenLayout screenLayout, Rotation rotation )
	{
		var basePosition = new Vector3( screenLayout.UiX + screenLayout.FacingX * 18f, screenLayout.UiY + screenLayout.FacingY * 18f, screenLayout.UiZ + screenLayout.FacingZ * 18f );
		var right = rotation.Right;
		var up = rotation.Up;
		var halfWidth = screenLayout.ScreenWidth * 0.5f;
		var halfHeight = screenLayout.ScreenHeight * 0.5f;
		var scale = screenLayout.ScreenHeight / 520f;

		WallFallbackText = new ArenaWallFallbackText
		{
			Title = CreateWallFallbackTextObject( "Arena Wall Fallback Title", basePosition - right * (halfWidth * 0.42f) + up * (halfHeight * 0.34f), rotation, 0.92f * scale, ReadyStationColor ),
			Debug = CreateWallFallbackTextObject( "Arena Wall Fallback Debug", basePosition + right * (halfWidth * 0.38f) + up * (halfHeight * 0.36f), rotation, 0.26f * scale, new Color( 1f, 0.86f, 0.2f, 1f ) ),
			Headline = CreateWallFallbackTextObject( "Arena Wall Fallback Headline", basePosition - right * (halfWidth * 0.42f) + up * (halfHeight * 0.08f), rotation, 1.28f * scale, WinnerStationColor ),
			Mode = CreateWallFallbackTextObject( "Arena Wall Fallback Mode", basePosition - right * (halfWidth * 0.42f) - up * (halfHeight * 0.26f), rotation, 0.52f * scale, Color.White ),
			Leaderboard = CreateWallFallbackTextObject( "Arena Wall Fallback Leaderboard", basePosition + right * (halfWidth * 0.08f) + up * (halfHeight * 0.12f), rotation, 0.48f * scale, Color.White ),
			Stations = CreateWallFallbackTextObject( "Arena Wall Fallback Stations", basePosition + right * (halfWidth * 0.08f) - up * (halfHeight * 0.28f), rotation, 0.42f * scale, Color.White )
		};
	}

	private TextRenderer CreateWallFallbackTextObject( string name, Vector3 position, Rotation rotation, float scale, Color color )
	{
		var gameObject = FindOrCreate( name );
		gameObject.LocalPosition = position;
		gameObject.LocalRotation = rotation;
		gameObject.LocalScale = Vector3.One;

		var renderer = gameObject.Components.GetOrCreate<TextRenderer>();
		renderer.Scale = scale;
		renderer.Color = color;
		return renderer;
	}

	private void SetWallFallbackVisible( bool visible )
	{
		SetTextRendererVisible( WallFallbackText?.Title, visible );
		SetTextRendererVisible( WallFallbackText?.Debug, visible );
		SetTextRendererVisible( WallFallbackText?.Headline, visible );
		SetTextRendererVisible( WallFallbackText?.Mode, visible );
		SetTextRendererVisible( WallFallbackText?.Leaderboard, visible );
		SetTextRendererVisible( WallFallbackText?.Stations, visible );
	}

	private static void SetTextRendererVisible( TextRenderer renderer, bool visible )
	{
		if ( !renderer.IsValid() || !renderer.GameObject.IsValid() )
			return;

		renderer.GameObject.Enabled = visible;
	}

	private static Vector3 GetModelAnchorLocalPosition( ModelMetrics metrics, ModelPlacementAnchor anchor )
	{
		return anchor switch
		{
			ModelPlacementAnchor.Floor => metrics.Center - new Vector3( 0f, 0f, metrics.Size.z * 0.5f ),
			ModelPlacementAnchor.Ceiling => metrics.Center + new Vector3( 0f, 0f, metrics.Size.z * 0.5f ),
			_ => metrics.Center
		};
	}

	private ModelMetrics GetModelMetrics( string modelPath )
	{
		if ( ModelMetricsByPath.TryGetValue( modelPath, out var cached ) )
			return cached;

		var model = Model.Load( modelPath );
		var bounds = model.Bounds;
		var size = new Vector3(
			SafeModelAxisSize( bounds.Size.x ),
			SafeModelAxisSize( bounds.Size.y ),
			SafeModelAxisSize( bounds.Size.z ) );
		var metrics = new ModelMetrics( model, size, bounds.Center );
		ModelMetricsByPath[modelPath] = metrics;
		return metrics;
	}

	private static float SafeModelAxisSize( float value )
	{
		return MathF.Abs( value ) > 0.001f ? MathF.Abs( value ) : DevLayoutUnitSize;
	}

	private static Vector3 LayoutUnitsToWorldSize( Vector3 layoutSize )
	{
		return new Vector3(
			MathF.Abs( layoutSize.x ) * DevLayoutUnitSize,
			MathF.Abs( layoutSize.y ) * DevLayoutUnitSize,
			MathF.Abs( layoutSize.z ) * DevLayoutUnitSize );
	}

	private static Vector3 ScaleForDesiredWorldSize( Vector3 modelSize, Vector3 desiredWorldSize )
	{
		return new Vector3(
			desiredWorldSize.x / SafeModelAxisSize( modelSize.x ),
			desiredWorldSize.y / SafeModelAxisSize( modelSize.y ),
			desiredWorldSize.z / SafeModelAxisSize( modelSize.z ) );
	}

	private static Vector3 ComponentMultiply( Vector3 left, Vector3 right )
	{
		return new Vector3( left.x * right.x, left.y * right.y, left.z * right.z );
	}

}
