using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public sealed partial class PhysicalFastestTapperGame
{
	private const string QuaterniusModelRoot = "models/quaternius/modular_sci_fi/";
	private const string FrutigerAeroModelRoot = "models/frutiger_aero/";
	private const string TapperButtonModel = FrutigerAeroModelRoot + "apple_computer.vmdl";
	private const string StationBarFillModel = QuaterniusModelRoot + "decal_line_straight.vmdl";
	private const string WallPanelModel = QuaterniusModelRoot + "wallband_straight.vmdl";
	private const string WallDividedPanelModel = QuaterniusModelRoot + "wallastra_straight_divided.vmdl";
	private const string WallMetalPlateModel = QuaterniusModelRoot + "shortwall_metalplates_straight.vmdl";
	private const string WallVentModel = QuaterniusModelRoot + "prop_vent_big.vmdl";
	private const string WallAccessPointModel = QuaterniusModelRoot + "prop_accesspoint.vmdl";
	private const string WallCableModel = QuaterniusModelRoot + "topcables_straight.vmdl";
	private const string WallSupportColumnModel = QuaterniusModelRoot + "column_metalsupport.vmdl";
	private const float DevLayoutUnitSize = 50f;
	private const float ArcadeWallBaySize = 300f;
	private const float ArcadeRoofBaySize = 340f;
	private const float VenueWallThickness = 42f;
	private const float VenueRoofThickness = 34f;
	private const float VenueBoundaryWallThickness = 24f;

	private readonly Dictionary<string, ModelMetrics> ModelMetricsByPath = new();
	private readonly List<VenueDynamicLight> VenueDynamicLights = new();
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
		if ( UseAuthoredScene )
		{
			BindAuthoredArena();
			return;
		}

		RebuildArenaForStationCapacity( GetDesiredStationCapacity() );
	}

	private void BindAuthoredArena()
	{
		Stations.Clear();
		VenueDynamicLights.Clear();
		ProjectionStars.Clear();
		ProjectionClothes.Clear();
		ProjectionShootingStars.Clear();

		var stationCapacity = GetDesiredAuthoredStationCapacity();
		CurrentRoomLayout = RuntimeRoomLayoutMath.Build( Math.Max( 1, stationCapacity ) );
		CurrentGeneratedStationCount = 0;
		EnsureAuthoredPlayStationCapacity( stationCapacity );

		for ( var index = 0; index < 8; index++ )
		{
			var station = BindAuthoredStation( index );
			if ( station is null )
				continue;

			Stations.Add( station );
		}

		CurrentGeneratedStationCount = Stations.Count;
		if ( CurrentGeneratedStationCount > 0 )
			CurrentRoomLayout = RuntimeRoomLayoutMath.Build( CurrentGeneratedStationCount );

		BindAuthoredPixelGrassFloor();
		BindAuthoredWallScreen();
		BindAuthoredProjectionObjects();
		BindAuthoredVenueLights();
		BindAuthoredWallFallbackText();

		var gameCount = Scene.GetAllComponents<PhysicalFastestTapperGame>().Count( x => x.IsValid() );
		var playStationCount = Scene.GetAllObjects( true ).Count( x => x.IsValid() && x.Name.StartsWith( "PlayStation" ) );
		Log.Info( $"[TapperAuthoredScene] games={gameCount} stations={Stations.Count} playStations={playStationCount} spawnPoints={GetAuthoredSpawnPoints().Length} floor={PixelGrassFloorObject.IsValid()} wallScreen={WallScreen.IsValid()} projectionSphere={ProjectionSphereObject.IsValid()} physicalProgress=False generated=False" );
	}

	private TapperStation BindAuthoredStation( int index )
	{
		var root = FindSceneObject( $"PlayStation {index}" );
		if ( !root.IsValid() || !root.Enabled )
			return null;

		var button = FindSceneObject( $"Station {index} Physical Tap Button" );
		var buttonHitbox = FindSceneObject( $"Station {index} Button Hitbox" );

		if ( !button.IsValid() && !buttonHitbox.IsValid() )
			return null;

		var claimBoundsCenterLocal = AuthoredStationClaimBoundsCenterLocal;
		var claimBoundsHalfExtentsLocal = AuthoredStationClaimBoundsHalfExtentsLocal;
		var origin = GetStationLocalPointWorldPosition( root, claimBoundsCenterLocal );

		var station = new TapperStation
		{
			Index = index,
			Origin = origin,
			Root = root,
			Button = button,
			ButtonHitbox = buttonHitbox,
			ButtonRenderer = button.GetComponent<ModelRenderer>(),
			ClaimFrameRenderers = GetAuthoredStationFrameRenderers( index ),
			ButtonBaseScale = button.IsValid() ? button.LocalScale : Vector3.One,
			ClaimBoundsCenterLocal = claimBoundsCenterLocal,
			ClaimBoundsHalfExtentsLocal = claimBoundsHalfExtentsLocal
		};

		if ( station.Button.IsValid() )
			station.Button.Components.GetOrCreate<PhysicalTapButton>().StationIndex = index;

		if ( station.ButtonHitbox.IsValid() )
			station.ButtonHitbox.Components.GetOrCreate<PhysicalTapButton>().StationIndex = index;

		return station;
	}

	private ModelRenderer[] GetAuthoredStationFrameRenderers( int stationIndex )
	{
		return TapperStationObjectNames.ClaimFrameSuffixes
			.Select( suffix => FindSceneObject( $"Station {stationIndex}{suffix}" ) )
			.Where( x => x.IsValid() )
			.Select( x => x.GetComponent<ModelRenderer>() )
			.Where( x => x.IsValid() )
			.ToArray();
	}

	private static Vector3 GetStationLocalPointWorldPosition( GameObject root, Vector3 localPoint )
	{
		if ( !root.IsValid() )
			return localPoint;

		return root.WorldPosition + root.WorldRotation * ComponentMultiply( localPoint, root.LocalScale );
	}

	private void BindAuthoredPixelGrassFloor()
	{
		PixelGrassFloorObject = FindSceneObject( "Arena Pixel Grass Floor" );
		if ( !PixelGrassFloorObject.IsValid() )
		{
			Log.Warning( "[TapperAuthoredFloor] missing='Arena Pixel Grass Floor'" );
			return;
		}

		var layout = CurrentRoomLayout;
		PixelGrassFloorRenderer = PixelGrassFloorObject.GetComponent<ModelRenderer>();
		if ( PixelGrassFloorRenderer.IsValid() )
		{
			PixelGrassFloorRenderer.Enabled = UsePixelGrassFloor;
			PixelGrassFloorRenderer.Tint = PixelGrassFloorTint;
		}

		var collider = PixelGrassFloorObject.Components.GetOrCreate<BoxCollider>();
		collider.Scale = new Vector3( layout.FloorWidth, layout.FloorDepth, MathF.Max( 8f, layout.FloorThickness ) );
		collider.Static = true;
		collider.IsTrigger = false;

		var floorModelValid = PixelGrassFloorRenderer.IsValid() && PixelGrassFloorRenderer.Model.IsValid();
		Log.Info( $"[TapperAuthoredFloor] object=True renderer={PixelGrassFloorRenderer.IsValid()} modelValid={floorModelValid} sceneModelOnly=True position='{PixelGrassFloorObject.LocalPosition}' size='{layout.FloorWidth:0.#}x{layout.FloorDepth:0.#}'" );
	}

	private void BindAuthoredWallScreen()
	{
		var uiObject = FindSceneObject( "Arena Wall Screen UI" );
		if ( !uiObject.IsValid() )
			return;

		var screenLayout = ArenaWallScreenLayoutMath.Build( CurrentRoomLayout );
		ConfigureArenaWallWorldPanel( uiObject, screenLayout );

		var worldPanel = uiObject.Components.Get<WorldPanel>();
		Log.Info( $"[TapperAuthoredScene] wallScreen=True worldPanel={worldPanel.IsValid()} panelSize='{(worldPanel.IsValid() ? worldPanel.PanelSize.ToString() : "")}' wallScreen={WallScreen.IsValid()}" );
	}

	private void BindAuthoredWallFallbackText()
	{
		WallFallbackText = new ArenaWallFallbackText
		{
			Title = GetAuthoredTextRenderer( "Arena Wall Fallback Title" ),
			Debug = GetAuthoredTextRenderer( "Arena Wall Fallback Debug" ),
			Headline = GetAuthoredTextRenderer( "Arena Wall Fallback Headline" ),
			Mode = GetAuthoredTextRenderer( "Arena Wall Fallback Mode" ),
			Leaderboard = GetAuthoredTextRenderer( "Arena Wall Fallback Leaderboard" ),
			Stations = GetAuthoredTextRenderer( "Arena Wall Fallback Stations" )
		};
	}

	private TextRenderer GetAuthoredTextRenderer( string name )
	{
		var gameObject = FindSceneObject( name );
		return gameObject.IsValid()
			? gameObject.GetComponent<TextRenderer>()
			: null;
	}

	private void BindAuthoredProjectionObjects()
	{
		ProjectionSphereObject = FindSceneObject( "Venue Projection Sphere" );
		var layout = CurrentRoomLayout;
		var ceilingHeight = GetVenueCeilingHeight( layout );
		ProjectionSphereCenter = GetProjectionSphereCenter( Vector3.Zero, layout, ceilingHeight );
		var floorDiagonal = MathF.Sqrt( layout.FloorWidth * layout.FloorWidth + layout.FloorDepth * layout.FloorDepth );
		ProjectionSphereRadius = MathF.Max( floorDiagonal * 0.5f + ProjectionSphereRadiusPadding, ceilingHeight + ProjectionSphereRadiusPadding );
		ProjectionCycleStartTime = RealTime.Now;

		if ( ProjectionSphereObject.IsValid() )
		{
			ProjectionSphereCenter = ProjectionSphereObject.LocalPosition;
			ProjectionSphereRenderer = ProjectionSphereObject.GetComponent<ModelRenderer>();
			if ( ProjectionSphereRenderer.IsValid() )
			{
				ProjectionSphereRenderer.Enabled = UseProjectionSphere;
				ProjectionSphereRenderer.Tint = ProjectionSphereTint;
			}
		}
		else
		{
			Log.Warning( "[TapperAuthoredProjectionSphere] missing='Venue Projection Sphere'" );
		}

		ProjectionTopLightObject = FindSceneObject( "Venue Projection Top Light" );
		ProjectionTopLight = ProjectionTopLightObject.IsValid()
			? ProjectionTopLightObject.GetComponent<PointLight>()
			: null;
		if ( ProjectionTopLightObject.IsValid() )
			ProjectionTopLightObject.LocalPosition = ProjectionSphereCenter + Vector3.Up * ProjectionSphereRadius * 0.62f;

		if ( ProjectionTopLight.IsValid() )
		{
			ProjectionTopLight.LightColor = ProjectionTopLightColor * ProjectionTopLightIntensity;
			ProjectionTopLight.Radius = ProjectionTopLightRadius;
			ProjectionTopLight.Attenuation = 0.58f;
			ProjectionTopLight.Shadows = false;
		}

		ProjectionTopLightMarkerObject = FindSceneObject( "Venue Projection Top Light Marker" );
		ProjectionTopLightMarkerRenderer = ProjectionTopLightMarkerObject.IsValid()
			? ProjectionTopLightMarkerObject.GetComponent<ModelRenderer>()
			: null;

		var sphereModelValid = ProjectionSphereRenderer.IsValid() && ProjectionSphereRenderer.Model.IsValid();
		Log.Info( $"[TapperAuthoredProjectionSphere] object={ProjectionSphereObject.IsValid()} renderer={ProjectionSphereRenderer.IsValid()} modelValid={sphereModelValid} sceneModelOnly=True center='{ProjectionSphereCenter}' radius={ProjectionSphereRadius:0.#} material='{ProjectionSkyMaterialPath}'" );
	}

	private void BindAuthoredVenueLights()
	{
		foreach ( var gameObject in Scene.GetAllObjects( true ).Where( x => x.IsValid() && x.Name.StartsWith( "Venue Light Rig" ) ) )
		{
			var point = gameObject.GetComponent<PointLight>();
			var spot = gameObject.GetComponent<SpotLight>();
			if ( !point.IsValid() && !spot.IsValid() )
				continue;

			var role = VenueLightRole.Ambient;
			var stationIndex = -1;

			if ( gameObject.Name.Contains( "Wall Screen" ) )
			{
				role = VenueLightRole.WallScreen;
			}
			else if ( gameObject.Name.Contains( "Station" ) && TryParseTrailingNumber( gameObject.Name, out stationIndex ) )
			{
				role = VenueLightRole.Station;
			}

			VenueDynamicLights.Add( new VenueDynamicLight
			{
				GameObject = gameObject,
				Point = point,
				Spot = spot,
				Role = role,
				StationIndex = stationIndex,
				BaseColor = point.IsValid() ? point.LightColor : spot.LightColor,
				BaseRadius = point.IsValid() ? point.Radius : spot.Radius
			} );
		}
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
		EnsureVenueWorld( stage, layout );
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
		VenueDynamicLights.Clear();
		ProjectionStars.Clear();
		ProjectionClothes.Clear();
		ProjectionShootingStars.Clear();
		ProjectionSphereObject = null;
		ProjectionSphereRenderer = null;
		PixelGrassFloorObject = null;
		PixelGrassFloorRenderer = null;
		ProjectionTopLightObject = null;
		ProjectionTopLightMarkerObject = null;
		ProjectionTopLightMarkerRenderer = null;
		ProjectionTopLight = null;
		ProjectionSunObject = null;
		ProjectionSunRenderer = null;
		ProjectionSunLight = null;
		ProjectionMoonObject = null;
		ProjectionMoonGlowObject = null;
		ProjectionMoonRenderer = null;
		ProjectionMoonGlowRenderer = null;
		ProjectionMoonLight = null;

		foreach ( var prefix in new[] { "Arena Floor", "Arena Pixel Grass Floor", "Arena Liquid Glass Floor", "Arena Lane Strip", "Arena Key Glow", "Arena Wall Screen", "Arena Wall Fallback", "Arena Title Text", "Arena Timer Text", "Arena Mode Text", "Arena Leaderboard Text", "Arena Wall Station Row", "Leaderboard Tower", "Venue Projection Sphere", "Venue Projection Sky", "Venue Projection Visibility Beacon", "Venue Projection Top Light", "Venue Projection Sun", "Venue Projection Moon", "Venue Projection Star", "Venue Projection Shooting Star", "Venue Projection Clothes", "Venue Boundary Blocker", "Venue Boundary Plane", "Venue Boundary Wall", "Venue Wall Bay", "Venue Wall Detail", "Venue Ceiling Bay", "Venue Arcade Cabinet", "Venue Accent Column", "Venue Corner Glow", "Venue Office", "Venue Backdrop", "Venue High Sky", "Venue Asset", "Venue Speaker Stack", "Venue Light Rig", "Venue Header", "Venue Overhead", "Venue Left Back Wall", "Venue Right Back Wall", "Venue Podium", "Construct Tapper", "Arcade Key", "Arcade Warm", "Arcade Board", "Arcade Station", "Station Arcade", "Station 0 Speed Spark", "Station 1 Speed Spark", "Station 2 Speed Spark", "Station 3 Speed Spark", "Station 4 Speed Spark", "Station 5 Speed Spark", "Station 6 Speed Spark", "Station 7 Speed Spark" } )
		{
			foreach ( var gameObject in Scene.GetAllObjects( true ).Where( x => x.IsValid() && x.Name.StartsWith( prefix ) ).ToArray() )
				gameObject.Destroy();
		}
	}

	private void CreateArcadeFloor( Vector3 stage, RuntimeRoomLayout layout )
	{
		CreatePixelGrassFloor( stage, layout );
	}

	private void CreatePixelGrassFloor( Vector3 stage, RuntimeRoomLayout layout )
	{
		if ( !UsePixelGrassFloor )
			return;

		var center = GetPixelGrassFloorCenter( stage, layout );
		var materialPath = PixelGrassFloorMaterialPath;

		PixelGrassFloorObject = FindOrCreate( "Arena Pixel Grass Floor" );
		PixelGrassFloorObject.LocalPosition = center;
		PixelGrassFloorObject.LocalRotation = Rotation.Identity;
		PixelGrassFloorObject.LocalScale = Vector3.One;

		PixelGrassFloorRenderer = PixelGrassFloorObject.Components.GetOrCreate<ModelRenderer>();
		PixelGrassFloorRenderer.Enabled = true;
		PixelGrassFloorRenderer.Model = GetPixelGrassFloorModel( layout.FloorWidth, layout.FloorDepth, materialPath );
		PixelGrassFloorRenderer.Tint = PixelGrassFloorTint;

		var collider = PixelGrassFloorObject.Components.GetOrCreate<BoxCollider>();
		collider.Scale = new Vector3( layout.FloorWidth, layout.FloorDepth, MathF.Max( 8f, layout.FloorThickness ) );
		collider.Static = true;
		collider.IsTrigger = false;

		Log.Info( $"[TapperPixelGrassFloor] material='{materialPath}' shader='shaders/complex.shader' texture='textures/pixel_grass.png' floorObjects=1 mesh='single-textured-slab' collision='same-object-box' objectEnabled={PixelGrassFloorObject.Enabled} rendererEnabled={PixelGrassFloorRenderer.Enabled} modelValid={PixelGrassFloorRenderer.Model.IsValid()} position='{center}' heightAboveFloor={PixelGrassFloorHeightAboveFloor:0.##} size='{layout.FloorWidth:0.#}x{layout.FloorDepth:0.#}' tint='{PixelGrassFloorTint}' winding='slab-12-triangles' bounds='{layout.FloorWidth:0.#}x{layout.FloorDepth:0.#}x8'" );
	}

	private void CreateArcadeRoomShell( Vector3 stage, RuntimeRoomLayout layout )
	{
		var ceilingHeight = GetVenueCeilingHeight( layout );
		if ( UseProjectionSphere )
		{
			CreateProjectionSphere( stage, layout, ceilingHeight );
		}
		else
		{
			CreateArcadeWallBays( stage, layout, ceilingHeight );
			CreateArcadeRoofBays( stage, layout, ceilingHeight );
		}

		CreateVenueBoundaryWalls( stage, layout, ceilingHeight );
		CreateVenueLightRig( stage, layout, ceilingHeight );
	}

	private void CreateProjectionSphere( Vector3 stage, RuntimeRoomLayout layout, float ceilingHeight )
	{
		var center = GetProjectionSphereCenter( stage, layout, ceilingHeight );

		var floorDiagonal = MathF.Sqrt( layout.FloorWidth * layout.FloorWidth + layout.FloorDepth * layout.FloorDepth );
		var radius = MathF.Max( floorDiagonal * 0.5f + ProjectionSphereRadiusPadding, ceilingHeight + ProjectionSphereRadiusPadding );
		ProjectionSphereCenter = center;
		ProjectionSphereRadius = radius;
		ProjectionCycleStartTime = RealTime.Now;

		ProjectionSphereObject = FindOrCreate( "Venue Projection Sphere" );
		ProjectionSphereObject.LocalPosition = center;
		ProjectionSphereObject.LocalRotation = Rotation.Identity;
		ProjectionSphereObject.LocalScale = Vector3.One;
		ProjectionSphereObject.SetParent( EnsureVenueFallbackRoot(), true );

		ProjectionSphereRenderer = ProjectionSphereObject.Components.GetOrCreate<ModelRenderer>();
		ProjectionSphereRenderer.Model = GetProjectionSphereModel( radius );
		ProjectionSphereRenderer.Tint = ProjectionSphereTint;
		CreateProjectionTopLight();

		Log.Info( $"[TapperProjectionSphere] center='{center}' radius={radius:0.#} floor='{layout.FloorWidth:0.#}x{layout.FloorDepth:0.#}' uv='{ProjectionSphereURepeat:0.##}x{ProjectionSphereVRepeat:0.##}' rotationSpeed={ProjectionSphereRotationSpeed:0.##} pitchTilt={ProjectionSpherePitchTilt:0.##}" );
		Log.Info( $"[TapperProjectionSimpleSpin] material='{ProjectionSkyMaterialPath}' shader='shaders/projection_endless.shader' mapping='sphere-space procedural' uv='{ProjectionSphereURepeat:0.##}x{ProjectionSphereVRepeat:0.##}' rotationSpeed={ProjectionSphereRotationSpeed:0.##} topLight=True dayNight=False orbitalEffects=False procedural=True" );
	}

	private Vector3 GetPixelGrassFloorCenter( Vector3 stage, RuntimeRoomLayout layout )
	{
		var frontWallX = RuntimeRoomLayoutMath.FrontWallX( layout );
		return stage + new Vector3(
			frontWallX + layout.FloorWidth * 0.5f,
			layout.LeftWallY + layout.FloorDepth * 0.5f,
			layout.FloorThickness + PixelGrassFloorHeightAboveFloor );
	}

	private static Vector3 GetProjectionSphereCenter( Vector3 stage, RuntimeRoomLayout layout, float ceilingHeight )
	{
		return stage + new Vector3(
			RuntimeRoomLayoutMath.RoomCenterX( layout ),
			RuntimeRoomLayoutMath.RoomCenterY( layout ),
			ceilingHeight * 0.46f );
	}

	private void CreateProjectionSphereEffects()
	{
		if ( !UseProjectionSphere || ProjectionSphereRadius <= 0f )
			return;

		CreateProjectionSun();

		if ( EnableProjectionMoon )
			CreateProjectionMoon();

		if ( EnableSphereStars )
			CreateProjectionStars();

		if ( EnableShootingStars )
			CreateProjectionShootingStars();

		if ( EnablePixelClothesProjection )
			CreateProjectionClothes();

		Log.Info( $"[TapperProjectionSky] stars={ProjectionStars.Count} shootingStars={ProjectionShootingStars.Count} moon={ProjectionMoonObject.IsValid()} startNight={StartProjectionCycleAtNight}" );
	}

	private void CreateProjectionTopLight()
	{
		ProjectionTopLightObject = FindOrCreate( "Venue Projection Top Light" );
		ProjectionTopLightObject.LocalPosition = ProjectionSphereCenter + Vector3.Up * ProjectionSphereRadius * 0.62f;
		ProjectionTopLightObject.LocalRotation = Rotation.Identity;
		ProjectionTopLightObject.LocalScale = Vector3.One;
		ProjectionTopLightObject.SetParent( EnsureVenueFallbackRoot(), true );

		ProjectionTopLight = ProjectionTopLightObject.Components.GetOrCreate<PointLight>();
		ProjectionTopLight.LightColor = ProjectionTopLightColor * ProjectionTopLightIntensity;
		ProjectionTopLight.Radius = ProjectionTopLightRadius;
		ProjectionTopLight.Attenuation = 0.58f;
		ProjectionTopLight.Shadows = false;

		ProjectionTopLightMarkerObject = FindOrCreate( "Venue Projection Top Light Marker" );
		ProjectionTopLightMarkerObject.LocalPosition = ProjectionTopLightObject.LocalPosition;
		ProjectionTopLightMarkerObject.LocalRotation = Rotation.Identity;
		ProjectionTopLightMarkerObject.LocalScale = Vector3.One * MathF.Max( 42f, ProjectionSphereRadius * 0.028f );
		ProjectionTopLightMarkerObject.SetParent( EnsureVenueFallbackRoot(), true );

		ProjectionTopLightMarkerRenderer = ProjectionTopLightMarkerObject.Components.GetOrCreate<ModelRenderer>();
		ProjectionTopLightMarkerRenderer.Model = GetProjectionSunModel();
		ProjectionTopLightMarkerRenderer.Tint = ProjectionTopLightColor * 2.2f;

		Log.Info( $"[TapperProjectionTopLight] position='{ProjectionTopLightObject.LocalPosition}' radius={ProjectionTopLightRadius:0.#} intensity={ProjectionTopLightIntensity:0.##} color='{ProjectionTopLightColor}' marker=True" );
	}

	private void CreateProjectionSun()
	{
		ProjectionSunObject = FindOrCreate( "Venue Projection Sun" );
		ProjectionSunObject.LocalScale = Vector3.One * MathF.Max( 24f, ProjectionSphereRadius * 0.045f );
		ProjectionSunObject.SetParent( EnsureVenueFallbackRoot(), true );

		ProjectionSunRenderer = ProjectionSunObject.Components.GetOrCreate<ModelRenderer>();
		ProjectionSunRenderer.Model = GetProjectionSunModel();
		ProjectionSunRenderer.Tint = SunDayColor;

		ProjectionSunLight = ProjectionSunObject.Components.GetOrCreate<PointLight>();
		ProjectionSunLight.LightColor = SunDayColor;
		ProjectionSunLight.Radius = SunLightRadius;
		ProjectionSunLight.Attenuation = 0.65f;
		ProjectionSunLight.Shadows = false;
	}

	private void CreateProjectionMoon()
	{
		var moonScale = MathF.Max( 82f, ProjectionSphereRadius * 0.072f );
		ProjectionMoonObject = FindOrCreate( "Venue Projection Moon" );
		ProjectionMoonObject.LocalScale = Vector3.One * moonScale;
		ProjectionMoonObject.SetParent( EnsureVenueFallbackRoot(), true );

		ProjectionMoonRenderer = ProjectionMoonObject.Components.GetOrCreate<ModelRenderer>();
		ProjectionMoonRenderer.Model = GetProjectionMoonModel();
		ProjectionMoonRenderer.Tint = MoonNightColor;

		ProjectionMoonGlowObject = FindOrCreate( "Venue Projection Moon Glow" );
		ProjectionMoonGlowObject.LocalScale = Vector3.One * moonScale * MathF.Max( 1f, MoonGlowScale );
		ProjectionMoonGlowObject.SetParent( EnsureVenueFallbackRoot(), true );

		ProjectionMoonGlowRenderer = ProjectionMoonGlowObject.Components.GetOrCreate<ModelRenderer>();
		ProjectionMoonGlowRenderer.Model = GetProjectionStarGlowModel();
		ProjectionMoonGlowRenderer.Tint = new Color( 0.72f, 0.86f, 1f, 1f );

		ProjectionMoonLight = ProjectionMoonObject.Components.GetOrCreate<PointLight>();
		ProjectionMoonLight.LightColor = MoonNightColor;
		ProjectionMoonLight.Radius = MoonLightRadius;
		ProjectionMoonLight.Attenuation = 0.78f;
		ProjectionMoonLight.Shadows = false;
	}

	private void CreateProjectionStars()
	{
		var count = Math.Clamp( SphereStarCount, 0, 240 );
		var litEvery = Math.Max( 1, count / 8 );
		for ( var i = 0; i < count; i++ )
		{
			var largeStar = i % 13 == 0;
			var baseScale = (18f + (i % 5) * 7f) * MathF.Max( 0.25f, StarVisualScale ) * (largeStar ? 1.65f : 1f);
			var visual = new ProjectionSphereVisual
			{
				GameObject = FindOrCreate( $"Venue Projection Star {i:000}" ),
				Longitude = i * 2.399963f,
				Latitude = 0.18f + ((i * 37) % 73) / 72f * 1.02f,
				OrbitSpeed = 0.008f + (i % 5) * 0.002f,
				Phase = i * 0.73f,
				RadiusScale = 0.90f,
				BaseScale = baseScale,
				DayColor = new Color( 0.55f, 0.7f, 1f, 0.28f ),
				NightColor = Color.Lerp( new Color( 0.42f, 0.76f, 1f, 1f ), Color.White, (i % 7) / 6f )
			};

			visual.GameObject.LocalScale = Vector3.One * visual.BaseScale;
			visual.GameObject.SetParent( EnsureVenueFallbackRoot(), true );
			visual.Renderer = visual.GameObject.Components.GetOrCreate<ModelRenderer>();
			visual.Renderer.Model = GetProjectionStarModel();
			visual.Renderer.Tint = visual.NightColor;

			visual.GlowObject = FindOrCreate( $"Venue Projection Star Glow {i:000}" );
			visual.GlowObject.LocalScale = Vector3.One * visual.BaseScale * MathF.Max( 1f, StarGlowScale );
			visual.GlowObject.SetParent( EnsureVenueFallbackRoot(), true );
			visual.GlowRenderer = visual.GlowObject.Components.GetOrCreate<ModelRenderer>();
			visual.GlowRenderer.Model = GetProjectionStarGlowModel();
			visual.GlowRenderer.Tint = new Color( 0.36f, 0.68f, 1f, 1f );

			if ( i % litEvery == 0 )
			{
				visual.Light = visual.GameObject.Components.GetOrCreate<PointLight>();
				visual.Light.LightColor = visual.NightColor;
				visual.Light.Radius = 520f + (i % 3) * 140f;
				visual.Light.Attenuation = 0.95f;
				visual.Light.Shadows = false;
			}

			ProjectionStars.Add( visual );
		}
	}

	private void CreateProjectionShootingStars()
	{
		var count = Math.Clamp( ShootingStarCount, 0, 12 );
		for ( var i = 0; i < count; i++ )
		{
			var visual = new ProjectionShootingStarVisual
			{
				GameObject = FindOrCreate( $"Venue Projection Shooting Star {i:000}" ),
				Phase = i * 1.91f,
				BaseScale = MathF.Max( 72f, ProjectionSphereRadius * 0.032f ),
				Duration = 1.15f + (i % 3) * 0.18f
			};

			visual.GameObject.LocalScale = new Vector3( 1f, ShootingStarTrailLength, 0.45f ) * visual.BaseScale;
			visual.GameObject.SetParent( EnsureVenueFallbackRoot(), true );
			visual.GameObject.Enabled = false;
			visual.Renderer = visual.GameObject.Components.GetOrCreate<ModelRenderer>();
			visual.Renderer.Model = GetProjectionShootingStarModel();
			visual.Renderer.Tint = ShootingStarColor;
			visual.Light = visual.GameObject.Components.GetOrCreate<PointLight>();
			visual.Light.LightColor = ShootingStarColor;
			visual.Light.Radius = ShootingStarLightRadius;
			visual.Light.Attenuation = 0.85f;
			visual.Light.Shadows = false;
			ProjectionShootingStars.Add( visual );
		}

		NextShootingStarTime = RealTime.Now + 0.35f;
	}

	private void CreateProjectionClothes()
	{
		var count = Math.Clamp( PixelClothesCount, 0, 24 );
		for ( var i = 0; i < count; i++ )
		{
			var visual = new ProjectionSphereVisual
			{
				GameObject = FindOrCreate( $"Venue Projection Clothes {i:000}" ),
				Longitude = i * 2.13f,
				Latitude = -0.42f + ((i * 17) % 37) / 36f * 1.05f,
				OrbitSpeed = PixelClothesOrbitSpeed * (0.62f + (i % 5) * 0.16f),
				Phase = i * 1.17f,
				RadiusScale = 0.91f,
				BaseScale = 46f + (i % 3) * 12f,
				DayColor = Color.White,
				NightColor = new Color( 0.42f, 0.72f, 1f, 1f )
			};

			visual.GameObject.LocalScale = Vector3.One * visual.BaseScale;
			visual.GameObject.SetParent( EnsureVenueFallbackRoot(), true );
			visual.Renderer = visual.GameObject.Components.GetOrCreate<ModelRenderer>();
			visual.Renderer.Model = GetProjectionClothesModel( i % 4 );
			visual.Renderer.Tint = Color.White;
			ProjectionClothes.Add( visual );
		}
	}

	private Model GetProjectionSphereModel( float radius )
	{
		var roundedRadius = MathF.Round( radius );
		if ( ProjectionSphereModel.IsValid()
			&& Math.Abs( ProjectionSphereModelRadius - roundedRadius ) < 0.5f
			&& Math.Abs( ProjectionSphereModelURepeat - ProjectionSphereURepeat ) < 0.001f
			&& Math.Abs( ProjectionSphereModelVRepeat - ProjectionSphereVRepeat ) < 0.001f
			&& string.Equals( ProjectionSphereModelMaterialPath, ProjectionSkyMaterialPath, StringComparison.OrdinalIgnoreCase ) )
		{
			return ProjectionSphereModel;
		}

		ProjectionSphereModelRadius = roundedRadius;
		ProjectionSphereModelURepeat = ProjectionSphereURepeat;
		ProjectionSphereModelVRepeat = ProjectionSphereVRepeat;
		ProjectionSphereModelMaterialPath = ProjectionSkyMaterialPath;
		ProjectionSphereModel = Model.Builder
			.AddMesh( CreateProjectionSphereMesh( 64, 32, ProjectionSphereURepeat, ProjectionSphereVRepeat, roundedRadius, ProjectionSkyMaterialPath ) )
			.Create();

		return ProjectionSphereModel;
	}

	private Model GetPixelGrassFloorModel( float width, float depth, string materialPath )
	{
		var roundedWidth = MathF.Round( width );
		var roundedDepth = MathF.Round( depth );
		if ( PixelGrassFloorModel.IsValid()
			&& Math.Abs( PixelGrassFloorModelWidth - roundedWidth ) < 0.5f
			&& Math.Abs( PixelGrassFloorModelDepth - roundedDepth ) < 0.5f
			&& string.Equals( PixelGrassFloorModelMaterialPath, materialPath, StringComparison.OrdinalIgnoreCase ) )
		{
			return PixelGrassFloorModel;
		}

		PixelGrassFloorModelWidth = roundedWidth;
		PixelGrassFloorModelDepth = roundedDepth;
		PixelGrassFloorModelMaterialPath = materialPath;
		PixelGrassFloorModel = Model.Builder
			.AddMesh( CreatePixelGrassFloorMesh( roundedWidth, roundedDepth, materialPath ) )
			.Create();

		return PixelGrassFloorModel;
	}

	private static Mesh CreatePixelGrassFloorMesh( float width, float depth, string materialPath )
	{
		var requestedMaterialPath = string.IsNullOrWhiteSpace( materialPath ) ? "materials/core/shader_editor.vmat" : materialPath;
		var material = Material.Load( requestedMaterialPath );
		var materialValid = material.IsValid();
		if ( !materialValid )
			material = Material.Load( "materials/core/shader_editor.vmat" );

		Log.Info( $"[TapperPixelGrassFloorMaterial] path='{requestedMaterialPath}' valid={materialValid} fallback='{(!materialValid ? "materials/core/shader_editor.vmat" : "")}'" );

		var halfWidth = width * 0.5f;
		var halfDepth = depth * 0.5f;
		const float halfThickness = 4f;
		var mesh = new Mesh( material );
		mesh.CreateVertexBuffer<Vertex>( 24 );
		mesh.CreateIndexBuffer( 36 );
		mesh.Bounds = BBox.FromPositionAndSize( Vector3.Zero, new Vector3( width, depth, 8f ) );

		mesh.LockVertexBuffer<Vertex>( vertices =>
		{
			WritePixelGrassFloorFace( vertices, 0, new Vector3( -halfWidth, -halfDepth, halfThickness ), new Vector3( halfWidth, -halfDepth, halfThickness ), new Vector3( -halfWidth, halfDepth, halfThickness ), new Vector3( halfWidth, halfDepth, halfThickness ), Vector3.Up );
			WritePixelGrassFloorFace( vertices, 4, new Vector3( -halfWidth, halfDepth, -halfThickness ), new Vector3( halfWidth, halfDepth, -halfThickness ), new Vector3( -halfWidth, -halfDepth, -halfThickness ), new Vector3( halfWidth, -halfDepth, -halfThickness ), Vector3.Down );
			WritePixelGrassFloorFace( vertices, 8, new Vector3( -halfWidth, halfDepth, halfThickness ), new Vector3( halfWidth, halfDepth, halfThickness ), new Vector3( -halfWidth, halfDepth, -halfThickness ), new Vector3( halfWidth, halfDepth, -halfThickness ), Vector3.Forward );
			WritePixelGrassFloorFace( vertices, 12, new Vector3( halfWidth, -halfDepth, halfThickness ), new Vector3( -halfWidth, -halfDepth, halfThickness ), new Vector3( halfWidth, -halfDepth, -halfThickness ), new Vector3( -halfWidth, -halfDepth, -halfThickness ), Vector3.Backward );
			WritePixelGrassFloorFace( vertices, 16, new Vector3( halfWidth, halfDepth, halfThickness ), new Vector3( halfWidth, -halfDepth, halfThickness ), new Vector3( halfWidth, halfDepth, -halfThickness ), new Vector3( halfWidth, -halfDepth, -halfThickness ), Vector3.Right );
			WritePixelGrassFloorFace( vertices, 20, new Vector3( -halfWidth, -halfDepth, halfThickness ), new Vector3( -halfWidth, halfDepth, halfThickness ), new Vector3( -halfWidth, -halfDepth, -halfThickness ), new Vector3( -halfWidth, halfDepth, -halfThickness ), Vector3.Left );
		} );

		mesh.LockIndexBuffer( indices =>
		{
			for ( var face = 0; face < 6; face++ )
			{
				var vertex = face * 4;
				var index = face * 6;
				indices[index + 0] = vertex + 0;
				indices[index + 1] = vertex + 1;
				indices[index + 2] = vertex + 2;
				indices[index + 3] = vertex + 1;
				indices[index + 4] = vertex + 3;
				indices[index + 5] = vertex + 2;
			}
		} );

		return mesh;
	}

	private static void WritePixelGrassFloorFace( Span<Vertex> vertices, int start, Vector3 bottomLeft, Vector3 bottomRight, Vector3 topLeft, Vector3 topRight, Vector3 normal )
	{
		vertices[start + 0] = CreatePixelGrassFloorVertex( bottomLeft, new Vector2( 0f, 0f ), normal );
		vertices[start + 1] = CreatePixelGrassFloorVertex( bottomRight, new Vector2( 1f, 0f ), normal );
		vertices[start + 2] = CreatePixelGrassFloorVertex( topLeft, new Vector2( 0f, 1f ), normal );
		vertices[start + 3] = CreatePixelGrassFloorVertex( topRight, new Vector2( 1f, 1f ), normal );
	}

	private static Vertex CreatePixelGrassFloorVertex( Vector3 position, Vector2 uv, Vector3 normal )
	{
		return new Vertex
		{
			Position = position,
			Normal = normal,
			Tangent = new Vector4( Vector3.Right, 1f ),
			TexCoord0 = uv,
			TexCoord1 = uv,
			Color = Color.White
		};
	}

	private static Mesh CreateProjectionSphereMesh( int horizontalFacets, int verticalFacets, float uRepeat, float vRepeat, float radius, string materialPath )
	{
		var requestedMaterialPath = string.IsNullOrWhiteSpace( materialPath ) ? "materials/core/shader_editor.vmat" : materialPath;
		var material = Material.Load( requestedMaterialPath );
		var materialValid = material.IsValid();
		if ( !materialValid )
			material = Material.Load( "materials/core/shader_editor.vmat" );

		Log.Info( $"[TapperProjectionMaterial] path='{requestedMaterialPath}' valid={materialValid} fallback='{(!materialValid ? "materials/core/shader_editor.vmat" : "")}'" );
		var mesh = new Mesh( material );
		mesh.CreateVertexBuffer<Vertex>( (horizontalFacets + 1) * (verticalFacets + 1) );
		mesh.CreateIndexBuffer( horizontalFacets * verticalFacets * 12 );
		mesh.Bounds = BBox.FromPositionAndSize( Vector3.Zero, radius * 2f );

		mesh.LockVertexBuffer<Vertex>( vertices =>
		{
			var index = 0;
			for ( var v = 0; v <= verticalFacets; v++ )
			{
				var vertical = v / (float)verticalFacets;
				var theta = vertical * MathF.PI;
				var sinTheta = MathF.Sin( theta );
				var cosTheta = MathF.Cos( theta );

				for ( var u = 0; u <= horizontalFacets; u++ )
				{
					var horizontal = u / (float)horizontalFacets;
					var phi = horizontal * MathF.PI * 2f;
					var sinPhi = MathF.Sin( phi );
					var cosPhi = MathF.Cos( phi );
					var outward = new Vector3( sinTheta * cosPhi, sinTheta * sinPhi, cosTheta ).Normal;
					var horizon = 1f - MathF.Abs( vertical - 0.5f ) * 2f;
					var aurora = MathF.Max( 0f, MathF.Sin( horizontal * MathF.PI * 5.5f + vertical * MathF.PI * 2f ) ) * horizon;
					var starBand = MathF.Pow( MathF.Max( 0f, MathF.Sin( horizontal * MathF.PI * 73f ) * MathF.Sin( vertical * MathF.PI * 41f ) ), 18f );
					var fallbackSky = Color.Lerp( new Color( 0.01f, 0.015f, 0.055f, 1f ), new Color( 0.08f, 0.2f, 0.58f, 1f ), horizon * 0.68f );
					fallbackSky = Color.Lerp( fallbackSky, new Color( 0.02f, 0.9f, 1f, 1f ), aurora * 0.42f );
					fallbackSky = Color.Lerp( fallbackSky, Color.White, starBand );

					vertices[index++] = new Vertex
					{
						Position = outward * radius,
						Normal = -outward,
						Tangent = new Vector4( new Vector3( -sinPhi, cosPhi, 0f ).Normal, -1f ),
						TexCoord0 = new Vector2( horizontal * uRepeat, vertical * vRepeat ),
						TexCoord1 = new Vector2( horizontal * uRepeat, vertical * vRepeat ) * -1f,
						Color = fallbackSky
					};
				}
			}
		} );

		mesh.LockIndexBuffer( indices =>
		{
			var index = 0;
			for ( var v = 0; v < verticalFacets; v++ )
			{
				for ( var u = 0; u < horizontalFacets; u++ )
				{
					var a = v * (horizontalFacets + 1) + u;
					var b = v * (horizontalFacets + 1) + u + 1;
					var c = (v + 1) * (horizontalFacets + 1) + u;
					var d = (v + 1) * (horizontalFacets + 1) + u + 1;

					indices[index++] = a;
					indices[index++] = c;
					indices[index++] = b;
					indices[index++] = b;
					indices[index++] = c;
					indices[index++] = d;
					indices[index++] = a;
					indices[index++] = b;
					indices[index++] = c;
					indices[index++] = b;
					indices[index++] = d;
					indices[index++] = c;
				}
			}
		} );

		return mesh;
	}

	private Model GetProjectionSunModel()
	{
		if ( ProjectionSunModel.IsValid() )
			return ProjectionSunModel;

		ProjectionSunModel = Model.Builder
			.AddMesh( CreateProjectionSunMesh( 18, 12, 1f ) )
			.Create();
		return ProjectionSunModel;
	}

	private Model GetProjectionMoonModel()
	{
		if ( ProjectionMoonModel.IsValid() )
			return ProjectionMoonModel;

		ProjectionMoonModel = Model.Builder
			.AddMesh( CreateProjectionOrbMesh( 18, 12, 1f, new Color( 0.72f, 0.82f, 1f, 1f ), Color.White ) )
			.Create();
		return ProjectionMoonModel;
	}

	private Model GetProjectionStarModel()
	{
		if ( ProjectionStarModel.IsValid() )
			return ProjectionStarModel;

		ProjectionStarModel = Model.Builder
			.AddMesh( CreateProjectionStarMesh( 1f, Color.White ) )
			.Create();
		return ProjectionStarModel;
	}

	private Model GetProjectionStarGlowModel()
	{
		if ( ProjectionStarGlowModel.IsValid() )
			return ProjectionStarGlowModel;

		ProjectionStarGlowModel = Model.Builder
			.AddMesh( CreateProjectionStarGlowMesh( 1f, new Color( 0.48f, 0.76f, 1f, 1f ) ) )
			.Create();
		return ProjectionStarGlowModel;
	}

	private Model GetProjectionShootingStarModel()
	{
		if ( ProjectionShootingStarModel.IsValid() )
			return ProjectionShootingStarModel;

		ProjectionShootingStarModel = Model.Builder
			.AddMesh( CreateProjectionShootingStarMesh( 1f, ShootingStarColor ) )
			.Create();
		return ProjectionShootingStarModel;
	}

	private Model GetProjectionClothesModel( int variant )
	{
		if ( ProjectionClothesModels.TryGetValue( variant, out var model ) && model.IsValid() )
			return model;

		model = Model.Builder
			.AddMesh( CreateProjectionClothesMesh( variant ) )
			.Create();
		ProjectionClothesModels[variant] = model;
		return model;
	}

	private static Mesh CreateProjectionSunMesh( int horizontalFacets, int verticalFacets, float radius )
	{
		return CreateProjectionOrbMesh( horizontalFacets, verticalFacets, radius, new Color( 1f, 0.34f, 0.04f, 1f ), new Color( 1f, 0.94f, 0.38f, 1f ) );
	}

	private static Mesh CreateProjectionOrbMesh( int horizontalFacets, int verticalFacets, float radius, Color lowColor, Color highColor )
	{
		var material = Material.Load( "materials/core/shader_editor.vmat" );
		var mesh = new Mesh( material );
		mesh.CreateVertexBuffer<Vertex>( (horizontalFacets + 1) * (verticalFacets + 1) );
		mesh.CreateIndexBuffer( horizontalFacets * verticalFacets * 6 );
		mesh.Bounds = BBox.FromPositionAndSize( Vector3.Zero, radius * 2f );

		mesh.LockVertexBuffer<Vertex>( vertices =>
		{
			var index = 0;
			for ( var v = 0; v <= verticalFacets; v++ )
			{
				var vertical = v / (float)verticalFacets;
				var theta = vertical * MathF.PI;
				var sinTheta = MathF.Sin( theta );
				var cosTheta = MathF.Cos( theta );

				for ( var u = 0; u <= horizontalFacets; u++ )
				{
					var horizontal = u / (float)horizontalFacets;
					var phi = horizontal * MathF.PI * 2f;
					var sinPhi = MathF.Sin( phi );
					var cosPhi = MathF.Cos( phi );
					var normal = new Vector3( sinTheta * cosPhi, sinTheta * sinPhi, cosTheta ).Normal;

					vertices[index++] = new Vertex
					{
						Position = normal * radius,
						Normal = normal,
						Tangent = new Vector4( new Vector3( -sinPhi, cosPhi, 0f ).Normal, 1f ),
						TexCoord0 = new Vector2( horizontal, vertical ),
						Color = Color.Lerp( lowColor, highColor, vertical )
					};
				}
			}
		} );

		mesh.LockIndexBuffer( indices =>
		{
			var index = 0;
			for ( var v = 0; v < verticalFacets; v++ )
			{
				for ( var u = 0; u < horizontalFacets; u++ )
				{
					var a = v * (horizontalFacets + 1) + u;
					var b = v * (horizontalFacets + 1) + u + 1;
					var c = (v + 1) * (horizontalFacets + 1) + u;
					var d = (v + 1) * (horizontalFacets + 1) + u + 1;

					indices[index++] = a;
					indices[index++] = b;
					indices[index++] = c;
					indices[index++] = b;
					indices[index++] = d;
					indices[index++] = c;
				}
			}
		} );

		return mesh;
	}

	private static Mesh CreateProjectionStarMesh( float size, Color color )
	{
		var half = size * 0.5f;
		var arm = size * 0.18f;
		var vertices = new List<Vertex>();
		var indices = new List<int>();
		AddProjectionPixelRect( vertices, indices, -half, -arm * 0.5f, size, arm, color );
		AddProjectionPixelRect( vertices, indices, -arm * 0.5f, -half, arm, size, color );
		AddProjectionPixelRect( vertices, indices, -arm, -arm, arm * 2f, arm * 2f, Color.White );
		return CreateProjectionFlatMesh( vertices, indices, size );
	}

	private static Mesh CreateProjectionStarGlowMesh( float size, Color color )
	{
		var half = size * 0.5f;
		var thin = size * 0.1f;
		var vertices = new List<Vertex>();
		var indices = new List<int>();
		AddProjectionPixelRect( vertices, indices, -half, -thin * 0.5f, size, thin, color );
		AddProjectionPixelRect( vertices, indices, -thin * 0.5f, -half, thin, size, color );
		AddProjectionPixelRect( vertices, indices, -half * 0.58f, -half * 0.58f, size * 0.18f, size * 0.18f, color );
		AddProjectionPixelRect( vertices, indices, half * 0.4f, half * 0.4f, size * 0.18f, size * 0.18f, color );
		return CreateProjectionFlatMesh( vertices, indices, size );
	}

	private static Mesh CreateProjectionShootingStarMesh( float trailLength, Color color )
	{
		var length = MathF.Max( 1f, trailLength );
		var vertices = new List<Vertex>();
		var indices = new List<int>();
		AddProjectionPixelRect( vertices, indices, -length, -0.08f, length, 0.16f, color );
		AddProjectionPixelRect( vertices, indices, -0.18f, -0.18f, 0.36f, 0.36f, Color.White );
		AddProjectionPixelRect( vertices, indices, -length * 0.62f, -0.18f, length * 0.5f, 0.08f, new Color( 0.46f, 0.76f, 1f, 1f ) );
		AddProjectionPixelRect( vertices, indices, -length * 0.62f, 0.1f, length * 0.5f, 0.08f, new Color( 0.46f, 0.76f, 1f, 1f ) );
		return CreateProjectionFlatMesh( vertices, indices, length + 1f );
	}

	private static Mesh CreateProjectionBillboardMesh( float size, Color color )
	{
		var half = size * 0.5f;
		var vertices = new List<Vertex>();
		var indices = new List<int>();
		AddProjectionPixelRect( vertices, indices, -half, -half, size, size, color );
		return CreateProjectionFlatMesh( vertices, indices, size );
	}

	private static Mesh CreateProjectionClothesMesh( int variant )
	{
		var vertices = new List<Vertex>();
		var indices = new List<int>();

		switch ( variant )
		{
			case 0:
				AddProjectionPixelRect( vertices, indices, -0.42f, 0.2f, 0.84f, 0.34f, new Color( 0.9f, 0.2f, 0.36f, 1f ) );
				AddProjectionPixelRect( vertices, indices, -0.24f, -0.28f, 0.48f, 0.58f, new Color( 0.98f, 0.36f, 0.52f, 1f ) );
				AddProjectionPixelRect( vertices, indices, -0.64f, -0.08f, 0.22f, 0.34f, new Color( 0.72f, 0.12f, 0.28f, 1f ) );
				AddProjectionPixelRect( vertices, indices, 0.42f, -0.08f, 0.22f, 0.34f, new Color( 0.72f, 0.12f, 0.28f, 1f ) );
				break;
			case 1:
				AddProjectionPixelRect( vertices, indices, -0.36f, -0.48f, 0.28f, 0.92f, new Color( 0.18f, 0.48f, 1f, 1f ) );
				AddProjectionPixelRect( vertices, indices, 0.08f, -0.48f, 0.28f, 0.92f, new Color( 0.12f, 0.34f, 0.86f, 1f ) );
				AddProjectionPixelRect( vertices, indices, -0.36f, 0.28f, 0.72f, 0.18f, new Color( 0.08f, 0.22f, 0.64f, 1f ) );
				break;
			case 2:
				AddProjectionPixelRect( vertices, indices, -0.34f, -0.28f, 0.68f, 0.64f, new Color( 0.16f, 0.86f, 0.58f, 1f ) );
				AddProjectionPixelRect( vertices, indices, -0.24f, 0.28f, 0.48f, 0.26f, new Color( 0.08f, 0.48f, 0.38f, 1f ) );
				AddProjectionPixelRect( vertices, indices, -0.56f, -0.14f, 0.22f, 0.44f, new Color( 0.1f, 0.64f, 0.48f, 1f ) );
				AddProjectionPixelRect( vertices, indices, 0.34f, -0.14f, 0.22f, 0.44f, new Color( 0.1f, 0.64f, 0.48f, 1f ) );
				break;
			default:
				AddProjectionPixelRect( vertices, indices, -0.54f, 0.18f, 0.34f, 0.18f, new Color( 1f, 0.88f, 0.18f, 1f ) );
				AddProjectionPixelRect( vertices, indices, 0.2f, 0.18f, 0.34f, 0.18f, new Color( 1f, 0.88f, 0.18f, 1f ) );
				AddProjectionPixelRect( vertices, indices, -0.42f, -0.34f, 0.32f, 0.5f, new Color( 1f, 0.68f, 0.08f, 1f ) );
				AddProjectionPixelRect( vertices, indices, 0.1f, -0.34f, 0.32f, 0.5f, new Color( 1f, 0.68f, 0.08f, 1f ) );
				break;
		}

		return CreateProjectionFlatMesh( vertices, indices, 1.4f );
	}

	private static void AddProjectionPixelRect( List<Vertex> vertices, List<int> indices, float y, float z, float width, float height, Color color )
	{
		var start = vertices.Count;
		var left = y;
		var right = y + width;
		var bottom = z;
		var top = z + height;
		var normal = Vector3.Forward;
		var tangent = new Vector4( Vector3.Left, 1f );

		vertices.Add( new Vertex( new Vector3( 0f, left, bottom ), normal, tangent, new Vector2( 0f, 1f ) ) { Color = color } );
		vertices.Add( new Vertex( new Vector3( 0f, left, top ), normal, tangent, new Vector2( 0f, 0f ) ) { Color = color } );
		vertices.Add( new Vertex( new Vector3( 0f, right, top ), normal, tangent, new Vector2( 1f, 0f ) ) { Color = color } );
		vertices.Add( new Vertex( new Vector3( 0f, right, bottom ), normal, tangent, new Vector2( 1f, 1f ) ) { Color = color } );

		indices.Add( start );
		indices.Add( start + 1 );
		indices.Add( start + 2 );
		indices.Add( start + 2 );
		indices.Add( start + 3 );
		indices.Add( start );
	}

	private static Mesh CreateProjectionFlatMesh( List<Vertex> vertices, List<int> indices, float boundsSize )
	{
		var material = Material.Load( "materials/core/shader_editor.vmat" );
		var mesh = new Mesh( material );
		mesh.CreateVertexBuffer<Vertex>( vertices.Count );
		mesh.CreateIndexBuffer( indices.Count );
		mesh.Bounds = BBox.FromPositionAndSize( Vector3.Zero, boundsSize );

		mesh.LockVertexBuffer<Vertex>( target =>
		{
			for ( var i = 0; i < vertices.Count; i++ )
				target[i] = vertices[i];
		} );

		mesh.LockIndexBuffer( target =>
		{
			for ( var i = 0; i < indices.Count; i++ )
				target[i] = indices[i];
		} );

		return mesh;
	}

	private static float GetVenueCeilingHeight( RuntimeRoomLayout layout )
	{
		var enlargedScreenHeight = layout.WallHeight * ArenaWallScreenLayoutMath.ScreenSizeMultiplier;
		var screenTopClearance = layout.WallHeight * 0.5f + enlargedScreenHeight * 0.5f + 80f;
		return RuntimeRoomLayoutMath.EffectiveCeilingHeight( layout, screenTopClearance );
	}

	private void CreateArcadeWallBays( Vector3 stage, RuntimeRoomLayout layout, float ceilingHeight )
	{
		var wallCenterZ = ceilingHeight * 0.5f;
		var frontWallX = RuntimeRoomLayoutMath.FrontWallX( layout );
		var rearCount = Math.Max( 6, (int)MathF.Ceiling( layout.FloorDepth / ArcadeWallBaySize ) );
		var rearStep = layout.FloorDepth / rearCount;

		for ( var i = 0; i < rearCount; i++ )
		{
			var y = layout.LeftWallY + rearStep * (i + 0.5f);
			CreateFallbackModelObjectWorld( $"Venue Wall Bay Rear {i:00}", stage + new Vector3( layout.RearWallX, y, wallCenterZ ), new Vector3( VenueWallThickness, rearStep - 18f, ceilingHeight ), GetRearWallPanelModel( i ), GetWallPanelTint( i, VenueWallColor ), false, false );
			CreateFallbackModelObjectWorld( $"Venue Wall Bay Front {i:00}", stage + new Vector3( frontWallX, y, wallCenterZ ), new Vector3( VenueWallThickness, rearStep - 18f, ceilingHeight ), GetRearWallPanelModel( i + 1 ), GetWallPanelTint( i + 1, VenueWallColor * 0.72f ), false, false );
		}

		var sideCount = Math.Max( 5, (int)MathF.Ceiling( layout.FloorWidth / ArcadeWallBaySize ) );
		var sideStep = layout.FloorWidth / sideCount;
		for ( var i = 0; i < sideCount; i++ )
		{
			var x = frontWallX + sideStep * (i + 0.5f);
			CreateFallbackModelObjectWorld( $"Venue Wall Bay Left {i:00}", stage + new Vector3( x, layout.LeftWallY, wallCenterZ ), new Vector3( sideStep - 18f, VenueWallThickness, ceilingHeight ), GetSideWallPanelModel( i ), GetWallPanelTint( i, VenueWallColor * 0.86f ), false, false );
			CreateFallbackModelObjectWorld( $"Venue Wall Bay Right {i:00}", stage + new Vector3( x, layout.RightWallY, wallCenterZ ), new Vector3( sideStep - 18f, VenueWallThickness, ceilingHeight ), GetSideWallPanelModel( i + 1 ), GetWallPanelTint( i + 1, VenueWallColor * 0.86f ), false, false );
		}

		CreateArcadeWallTextureDetails( stage, layout, ceilingHeight, rearCount, rearStep, sideCount, sideStep );
	}

	private void CreateVenueBoundaryWalls( Vector3 stage, RuntimeRoomLayout layout, float ceilingHeight )
	{
		var frontWallX = RuntimeRoomLayoutMath.FrontWallX( layout );
		var roomCenterX = RuntimeRoomLayoutMath.RoomCenterX( layout );
		var roomCenterY = RuntimeRoomLayoutMath.RoomCenterY( layout );
		var centerZ = ceilingHeight * 0.5f;
		var endOverlap = VenueBoundaryWallThickness * 2f;

		CreateVenueBoundaryWall( "Venue Boundary Wall Rear", stage + new Vector3( layout.RearWallX, roomCenterY, centerZ ), new Vector3( VenueBoundaryWallThickness, layout.FloorDepth + endOverlap, ceilingHeight ) );
		CreateVenueBoundaryWall( "Venue Boundary Wall Front", stage + new Vector3( frontWallX, roomCenterY, centerZ ), new Vector3( VenueBoundaryWallThickness, layout.FloorDepth + endOverlap, ceilingHeight ) );
		CreateVenueBoundaryWall( "Venue Boundary Wall Left", stage + new Vector3( roomCenterX, layout.LeftWallY, centerZ ), new Vector3( layout.FloorWidth + endOverlap, VenueBoundaryWallThickness, ceilingHeight ) );
		CreateVenueBoundaryWall( "Venue Boundary Wall Right", stage + new Vector3( roomCenterX, layout.RightWallY, centerZ ), new Vector3( layout.FloorWidth + endOverlap, VenueBoundaryWallThickness, ceilingHeight ) );
	}

	private GameObject CreateVenueBoundaryWall( string name, Vector3 position, Vector3 size )
	{
		var gameObject = FindOrCreate( name );
		gameObject.LocalPosition = position;
		gameObject.LocalRotation = Rotation.Identity;
		gameObject.LocalScale = Vector3.One;
		gameObject.SetParent( EnsureVenueFallbackRoot(), true );

		var collider = gameObject.Components.GetOrCreate<BoxCollider>();
		collider.Scale = size;
		collider.Static = true;
		collider.IsTrigger = false;
		return gameObject;
	}

	private static string GetRearWallPanelModel( int index )
	{
		return index % 3 == 0 ? WallMetalPlateModel : WallDividedPanelModel;
	}

	private static string GetSideWallPanelModel( int index )
	{
		return index % 2 == 0 ? WallMetalPlateModel : WallDividedPanelModel;
	}

	private static Color GetWallPanelTint( int index, Color baseColor )
	{
		var lift = index % 2 == 0 ? 1.08f : 0.94f;
		return baseColor * lift;
	}

	private void CreateArcadeWallTextureDetails( Vector3 stage, RuntimeRoomLayout layout, float ceilingHeight, int rearCount, float rearStep, int sideCount, float sideStep )
	{
		var frontWallX = RuntimeRoomLayoutMath.FrontWallX( layout );
		var detailColor = new Color( 0.19f, 0.22f, 0.26f, 1f );
		var cableColor = new Color( 0.1f, 0.18f, 0.23f, 1f );
		var supportColor = new Color( 0.28f, 0.31f, 0.36f, 1f );
		var rearDetailX = layout.RearWallX - VenueWallThickness * 0.58f;
		var frontDetailX = frontWallX + VenueWallThickness * 0.58f;
		var leftDetailY = layout.LeftWallY + VenueWallThickness * 0.58f;
		var rightDetailY = layout.RightWallY - VenueWallThickness * 0.58f;
		var cableZ = ceilingHeight - 95f;
		var lowerDetailZ = MathF.Min( ceilingHeight - 210f, 290f );

		for ( var i = 0; i < rearCount; i++ )
		{
			var y = layout.LeftWallY + rearStep * (i + 0.5f);
			CreateFallbackModelObjectWorld( $"Venue Wall Detail Rear Cable {i:00}", stage + new Vector3( rearDetailX, y, cableZ ), new Vector3( 18f, rearStep * 0.72f, 58f ), WallCableModel, cableColor, false, false );
			CreateFallbackModelObjectWorld( $"Venue Wall Detail Front Cable {i:00}", stage + new Vector3( frontDetailX, y, cableZ ), new Vector3( 18f, rearStep * 0.72f, 58f ), WallCableModel, cableColor * 0.82f, false, false );

			if ( i % 2 == 0 )
				CreateFallbackModelObjectWorld( $"Venue Wall Detail Rear Vent {i:00}", stage + new Vector3( rearDetailX - 4f, y, lowerDetailZ ), new Vector3( 20f, rearStep * 0.42f, 132f ), WallVentModel, detailColor, false, false );

			if ( i % 3 == 1 )
				CreateFallbackModelObjectWorld( $"Venue Wall Detail Front Access {i:00}", stage + new Vector3( frontDetailX + 4f, y, lowerDetailZ + 22f ), new Vector3( 20f, rearStep * 0.32f, 128f ), WallAccessPointModel, detailColor * 1.12f, false, false );

			if ( i % 3 == 0 )
			{
				var edgeY = layout.LeftWallY + rearStep * i;
				CreateFallbackModelObjectWorld( $"Venue Wall Detail Rear Support {i:00}", stage + new Vector3( rearDetailX - 6f, edgeY, ceilingHeight * 0.5f ), new Vector3( 26f, 56f, ceilingHeight * 0.94f ), WallSupportColumnModel, supportColor, false, false );
				CreateFallbackModelObjectWorld( $"Venue Wall Detail Front Support {i:00}", stage + new Vector3( frontDetailX + 6f, edgeY, ceilingHeight * 0.5f ), new Vector3( 26f, 56f, ceilingHeight * 0.94f ), WallSupportColumnModel, supportColor * 0.82f, false, false );
			}
		}

		for ( var i = 0; i < sideCount; i++ )
		{
			var x = frontWallX + sideStep * (i + 0.5f);
			CreateFallbackModelObjectWorld( $"Venue Wall Detail Left Cable {i:00}", stage + new Vector3( x, leftDetailY, cableZ ), new Vector3( sideStep * 0.72f, 18f, 58f ), WallCableModel, cableColor * 0.92f, false, false, ModelPlacementAnchor.Center, Rotation.FromYaw( 90f ) );
			CreateFallbackModelObjectWorld( $"Venue Wall Detail Right Cable {i:00}", stage + new Vector3( x, rightDetailY, cableZ ), new Vector3( sideStep * 0.72f, 18f, 58f ), WallCableModel, cableColor * 0.92f, false, false, ModelPlacementAnchor.Center, Rotation.FromYaw( 90f ) );

			if ( i % 3 == 1 )
			{
				CreateFallbackModelObjectWorld( $"Venue Wall Detail Left Vent {i:00}", stage + new Vector3( x, leftDetailY + 4f, lowerDetailZ ), new Vector3( sideStep * 0.34f, 20f, 124f ), WallVentModel, detailColor * 0.96f, false, false, ModelPlacementAnchor.Center, Rotation.FromYaw( 90f ) );
				CreateFallbackModelObjectWorld( $"Venue Wall Detail Right Access {i:00}", stage + new Vector3( x, rightDetailY - 4f, lowerDetailZ + 18f ), new Vector3( sideStep * 0.3f, 20f, 124f ), WallAccessPointModel, detailColor * 1.08f, false, false, ModelPlacementAnchor.Center, Rotation.FromYaw( 90f ) );
			}
		}
	}

	private void CreateArcadeRoofBays( Vector3 stage, RuntimeRoomLayout layout, float ceilingHeight )
	{
		var frontWallX = RuntimeRoomLayoutMath.FrontWallX( layout );
		var xCount = Math.Max( 5, (int)MathF.Ceiling( layout.FloorWidth / ArcadeRoofBaySize ) );
		var yCount = Math.Max( 5, (int)MathF.Ceiling( layout.FloorDepth / ArcadeRoofBaySize ) );
		var xStep = layout.FloorWidth / xCount;
		var yStep = layout.FloorDepth / yCount;

		for ( var x = 0; x < xCount; x++ )
		{
			for ( var y = 0; y < yCount; y++ )
			{
				var tileX = frontWallX + xStep * (x + 0.5f);
				var tileY = layout.LeftWallY + yStep * (y + 0.5f);
				CreateFallbackModelObjectWorld( $"Venue Ceiling Bay {x:00}-{y:00}", stage + new Vector3( tileX, tileY, ceilingHeight ), new Vector3( xStep - 12f, yStep - 12f, VenueRoofThickness ), WallPanelModel, new Color( 0.08f, 0.09f, 0.105f, 1f ), false, false, ModelPlacementAnchor.Ceiling );
			}
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

	private int GetDesiredAuthoredStationCapacity()
	{
		return TapperStationInteractionRules.ResolveDynamicStationCapacity( Players.Count, Players.Select( x => x.StationIndex ) );
	}

	private void EnsureStationCapacityForLobby()
	{
		if ( UseAuthoredScene )
		{
			if ( State is RoundState.Countdown or RoundState.Playing )
				return;

			var desiredAuthored = GetDesiredAuthoredStationCapacity();
			if ( desiredAuthored == CurrentGeneratedStationCount )
				return;

			EnsureAuthoredPlayStationCapacity( desiredAuthored );
			BindAuthoredArena();
			DropInvalidStationClaims();
			return;
		}

		if ( State is RoundState.Countdown or RoundState.Playing )
			return;

		var desired = GetDesiredStationCapacity();
		if ( desired == CurrentGeneratedStationCount )
			return;

		RebuildArenaForStationCapacity( desired );
		DropInvalidStationClaims();
	}

	private void DropInvalidStationClaims()
	{
		var activeStationIndexes = Stations
			.Select( x => x.Index )
			.ToHashSet();

		foreach ( var player in Players )
		{
			if ( !TapperStationInteractionRules.ShouldDropStationClaim( player.StationIndex, activeStationIndexes ) )
				continue;

			Log.Info( $"[TapperStations] mode='drop-invalid-claim' player='{player.Name}' station={player.StationIndex} active='{string.Join( ",", activeStationIndexes.OrderBy( x => x ) )}'" );
			player.StationIndex = -1;
			player.Ready = false;
			player.Spectating = false;
		}
	}

	private void EnsureAuthoredPlayStationCapacity( int stationCapacity )
	{
		var desired = Math.Clamp( stationCapacity, 0, 8 );
		var layout = RuntimeRoomLayoutMath.Build( Math.Max( 1, desired ) );

		for ( var index = 0; index < 8; index++ )
		{
			var root = FindSceneObject( $"PlayStation {index}" );
			if ( index < desired )
			{
				if ( !root.IsValid() )
				{
					root = CreateAuthoredPlayStationFromPrefabShape( index );
					Log.Info( $"[TapperAuthoredStations] created='PlayStation {index}' source='Assets/Prefabs/playstatio.prefab'" );
				}

				root.Enabled = true;
				root.LocalPosition = new Vector3( 196f, layout.StationY( index ), GetAuthoredPlayStationRootZ() );
				ConfigurePlayStationTree( root, index );
			}
			else if ( root.IsValid() )
			{
				root.Enabled = false;
			}
		}
	}

	private float GetAuthoredPlayStationRootZ()
	{
		var floor = PixelGrassFloorObject.IsValid()
			? PixelGrassFloorObject
			: FindSceneObject( "Arena Pixel Grass Floor" );
		var collider = floor.GetComponent<BoxCollider>();

		if ( floor.IsValid() && collider.IsValid() )
			return floor.WorldPosition.z + MathF.Abs( collider.Scale.z ) * 0.5f + 1f;

		return CurrentRoomLayout.FloorThickness + PixelGrassFloorHeightAboveFloor + CurrentRoomLayout.FloorThickness * 0.5f + 1f;
	}

	private GameObject CreateAuthoredPlayStationFromPrefabShape( int stationIndex )
	{
		var root = FindOrCreate( $"PlayStation {stationIndex}" );
		root.LocalRotation = Rotation.FromYaw( 90f );
		root.LocalScale = new Vector3( 0.791165411f, 0.704383969f, 0.121717758f );

		CreateAuthoredStationFrame( root, stationIndex, "Claim Frame Right", Vector3.Zero, Rotation.Identity, Vector3.One );
		CreateAuthoredStationFrame( root, stationIndex, "Claim Frame Front", new Vector3( -197.177521f, 278.257355f, 0f ), Rotation.FromYaw( -90f ), new Vector3( 1.25641036f, 1f, 1f ) );
		CreateAuthoredStationFrame( root, stationIndex, "Claim Frame Back", new Vector3( 197.177521f, 278.257355f, 0f ), Rotation.FromYaw( -90f ), new Vector3( 1.25641036f, 1f, 1f ) );
		CreateAuthoredStationFrame( root, stationIndex, "Claim Frame Left", new Vector3( 0f, 556.514709f, 0f ), Rotation.Identity, Vector3.One );
		CreateAuthoredStationButton( root, stationIndex );
		CreateAuthoredStationHitbox( root, stationIndex );

		return root;
	}

	private void CreateAuthoredStationFrame( GameObject root, int stationIndex, string suffix, Vector3 localPosition, Rotation localRotation, Vector3 localScale )
	{
		var gameObject = FindOrCreate( $"Station {stationIndex} {suffix}" );
		gameObject.SetParent( root, false );
		gameObject.LocalPosition = localPosition;
		gameObject.LocalRotation = localRotation;
		gameObject.LocalScale = localScale;
		gameObject.Enabled = true;

		var renderer = gameObject.Components.GetOrCreate<ModelRenderer>();
		renderer.Model = Model.Load( StationBarFillModel );
		renderer.Tint = ReadyStationColor;
	}

	private void CreateAuthoredStationButton( GameObject root, int stationIndex )
	{
		var gameObject = FindOrCreate( $"Station {stationIndex} Physical Tap Button" );
		gameObject.SetParent( root, false );
		gameObject.LocalPosition = new Vector3( -30.3350067f, 278.257355f, 353.276367f );
		gameObject.LocalRotation = Rotation.Identity;
		gameObject.LocalScale = new Vector3( 1.263958f, 1.41968f, 8.215728f );
		gameObject.Enabled = true;

		var renderer = gameObject.Components.GetOrCreate<ModelRenderer>();
		renderer.Model = Model.Load( TapperButtonModel );
		renderer.Tint = Color.White;

		var collider = gameObject.Components.GetOrCreate<BoxCollider>();
		collider.Scale = new Vector3( 118f, 104f, 72f );
		collider.Static = true;
		collider.IsTrigger = false;

		gameObject.Components.GetOrCreate<PhysicalTapButton>().StationIndex = stationIndex;
	}

	private void CreateAuthoredStationHitbox( GameObject root, int stationIndex )
	{
		var gameObject = FindOrCreate( $"Station {stationIndex} Button Hitbox" );
		gameObject.SetParent( root, false );
		gameObject.LocalPosition = new Vector3( -30.3349972f, 278.257355f, 353.276367f );
		gameObject.LocalRotation = Rotation.FromYaw( -90f );
		gameObject.LocalScale = new Vector3( 1.26395822f, 1.41968024f, 8.21572781f );
		gameObject.Enabled = true;

		var collider = gameObject.Components.GetOrCreate<BoxCollider>();
		collider.Scale = new Vector3( 190f, 190f, 110f );
		collider.Static = true;
		collider.IsTrigger = true;

		gameObject.Components.GetOrCreate<PhysicalTapButton>().StationIndex = stationIndex;
	}

	private void RenamePlayStationTree( GameObject root, int fromIndex, int toIndex )
	{
		if ( !root.IsValid() )
			return;

		var fromStation = $"Station {fromIndex} ";
		var toStation = $"Station {toIndex} ";

		foreach ( var gameObject in Scene.GetAllObjects( true ).Where( x => IsInGameObjectTree( x, root ) ).ToArray() )
		{
			if ( gameObject == root )
			{
				gameObject.Name = $"PlayStation {toIndex}";
				continue;
			}

			if ( gameObject.Name.Contains( fromStation ) )
				gameObject.Name = gameObject.Name.Replace( fromStation, toStation );
		}
	}

	private void ConfigurePlayStationTree( GameObject root, int stationIndex )
	{
		if ( !root.IsValid() )
			return;

		foreach ( var gameObject in Scene.GetAllObjects( true ).Where( x => IsInGameObjectTree( x, root ) ) )
		{
			if ( gameObject.Name.Contains( $"Station {stationIndex} " ) )
				continue;

			if ( TapperStationObjectNames.TryParseStationIndex( gameObject.Name, out _ ) )
				gameObject.Name = gameObject.Name.Replace( "Station 0 ", $"Station {stationIndex} " );
		}

		foreach ( var tapButton in Scene.GetAllComponents<PhysicalTapButton>().Where( x => x.IsValid() && x.GameObject.IsValid() && IsInGameObjectTree( x.GameObject, root ) ) )
			tapButton.StationIndex = stationIndex;
	}

	private static bool IsInGameObjectTree( GameObject gameObject, GameObject root )
	{
		var current = gameObject;
		while ( current.IsValid() )
		{
			if ( current == root )
				return true;

			current = current.Parent;
		}

		return false;
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

		station.Button = CreateModelObjectWorld( $"Station {index} Physical Tap Button", origin + new Vector3( 0f, -24f, 8f ), new Vector3( 118f, 104f, 72f ), TapperButtonModel, Color.White, false, true, ModelPlacementAnchor.Floor );
		var tapButton = station.Button.Components.GetOrCreate<PhysicalTapButton>();
		tapButton.StationIndex = index;
		station.ButtonRenderer = station.Button.GetComponent<ModelRenderer>();

		station.ButtonHitbox = CreateButtonHitbox( index, origin + new Vector3( 0f, -24f, 56f ) );

		station.ButtonBaseScale = station.Button.LocalScale;
		return station;
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

	private void EnsureVenueWorld( Vector3 stage, RuntimeRoomLayout layout )
	{
		var fallbackRoot = EnsureVenueFallbackRoot();
		fallbackRoot.Enabled = true;

		CreateVenueBackdrop( stage, layout );
	}

	private void CreateVenueBackdrop( Vector3 stage, RuntimeRoomLayout layout )
	{
		CreateOfficeShell( stage, layout );
	}

	private void CreateOfficeShell( Vector3 stage, RuntimeRoomLayout layout )
	{
		CreateArcadeRoomShell( stage, layout );
	}

	private void CreateVenueLightRig( Vector3 stage, RuntimeRoomLayout layout, float ceilingHeight )
	{
		VenueDynamicLights.Clear();

		var centerX = RuntimeRoomLayoutMath.RoomCenterX( layout );
		var yCount = Math.Max( 3, (int)MathF.Ceiling( layout.FloorDepth / 900f ) );
		var yStep = layout.FloorDepth / yCount;
		for ( var i = 0; i < yCount; i++ )
		{
			var y = layout.LeftWallY + yStep * (i + 0.5f);
			CreateVenuePointLight( $"Venue Light Rig Ambient {i:00}", stage + new Vector3( centerX, y, ceilingHeight - 115f ), new Color( 0.32f, 0.58f, 1f, 1f ), 760f, false, VenueLightRole.Ambient );
		}

		for ( var i = 0; i < layout.StationCount; i++ )
		{
			var stationY = layout.StationY( i );
			var source = stage + new Vector3( centerX - 120f, stationY, ceilingHeight - 145f );
			var target = stage + new Vector3( 0f, stationY, 44f );
			CreateVenueSpotLight( $"Venue Light Rig Station {i:00}", source, target, ReadyStationColor, 980f, VenueLightRole.Station, i );
		}

		CreateVenuePointLight( "Venue Light Rig Wall Screen", stage + new Vector3( layout.RearWallX - 140f, 0f, Math.Min( ceilingHeight - 120f, layout.WallHeight + 210f ) ), new Color( 0.22f, 0.82f, 1f, 1f ), 940f, false, VenueLightRole.WallScreen );
	}

	private VenueDynamicLight CreateVenuePointLight( string name, Vector3 position, Color color, float radius, bool shadows, VenueLightRole role, int stationIndex = -1 )
	{
		var gameObject = FindOrCreate( name );
		gameObject.LocalPosition = position;
		gameObject.LocalRotation = Rotation.Identity;
		gameObject.LocalScale = Vector3.One;
		gameObject.SetParent( EnsureVenueFallbackRoot(), true );

		var light = gameObject.Components.GetOrCreate<PointLight>();
		light.LightColor = color;
		light.Radius = radius;
		light.Attenuation = 1f;
		light.Shadows = shadows;

		var entry = new VenueDynamicLight
		{
			GameObject = gameObject,
			Point = light,
			Role = role,
			StationIndex = stationIndex,
			BaseColor = color,
			BaseRadius = radius
		};
		VenueDynamicLights.Add( entry );
		return entry;
	}

	private VenueDynamicLight CreateVenueSpotLight( string name, Vector3 position, Vector3 target, Color color, float radius, VenueLightRole role, int stationIndex )
	{
		var gameObject = FindOrCreate( name );
		gameObject.LocalPosition = position;
		gameObject.LocalScale = Vector3.One;
		gameObject.LocalRotation = Rotation.LookAt( (target - position).Normal, Vector3.Up );
		gameObject.SetParent( EnsureVenueFallbackRoot(), true );

		var light = gameObject.Components.GetOrCreate<SpotLight>();
		light.LightColor = color;
		light.Radius = radius;
		light.Attenuation = 1f;
		light.ConeInner = 28f;
		light.ConeOuter = 62f;
		light.Shadows = false;

		var entry = new VenueDynamicLight
		{
			GameObject = gameObject,
			Spot = light,
			Role = role,
			StationIndex = stationIndex,
			BaseColor = color,
			BaseRadius = radius
		};
		VenueDynamicLights.Add( entry );
		return entry;
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

	private GameObject CreateFallbackModelObjectWorld( string name, Vector3 position, Vector3 worldSize, string modelPath, Color tint, bool planeCollider, bool boxCollider, ModelPlacementAnchor anchor, Rotation rotation )
	{
		var gameObject = CreateModelObjectWorld( name, position, worldSize, modelPath, tint, planeCollider, boxCollider, anchor, rotation );
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
			collider.IsTrigger = false;
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

		ConfigureArenaWallWorldPanel( uiObject, screenLayout );
		CreateArenaWallFallbackText( screenLayout, displayRotation );
		SetWallFallbackVisible( ArenaWallScreenLayoutMath.ShouldShowFallback( IsPrimaryWallScreenValid() ) );
		var scaleRatio = uiObject.LocalScale.x / 100f;
		Log.Info( $"[TapperWallScreen] mode='razor' panelSize='{screenLayout.CssWidth}x{screenLayout.CssHeight}' localScale='{uiObject.LocalScale}' scaleRatio={scaleRatio:0.###} displayForward='{displayRotation.Forward}' screen='{screenLayout.ScreenWidth:0.#}x{screenLayout.ScreenHeight:0.#}' fallback={ArenaWallScreenLayoutMath.ShouldShowFallback( IsPrimaryWallScreenValid() )}" );
	}

	private void ConfigureArenaWallWorldPanel( GameObject uiObject, ArenaWallScreenLayout screenLayout )
	{
		var worldPanel = uiObject.Components.GetOrCreate<WorldPanel>();
		worldPanel.PanelSize = new Vector2( screenLayout.CssWidth, screenLayout.CssHeight );
		worldPanel.RenderScale = 1f;
		worldPanel.InteractionRange = 0f;

		WallScreen = uiObject.Components.GetOrCreate<Sandbox.ui.ArenaWallScreen>();
		WallScreen.Game = this;
		WallScreen.Enabled = true;

		Log.Info( $"[TapperWallScreen] nativeRazor={WallScreen.IsValid()} panelSize='{worldPanel.PanelSize}' renderScale={worldPanel.RenderScale:0.###} interactionRange={worldPanel.InteractionRange:0.#} fallback={ArenaWallScreenLayoutMath.ShouldShowFallback( IsPrimaryWallScreenValid() )}" );
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
