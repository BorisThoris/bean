using Sandbox;
using Sandbox.Citizen;
using System;
using System.Linq;

public sealed partial class PhysicalFastestTapperGame
{
	private const float BeanCapsuleRadius = 16f;
	private const float BeanCapsuleStartZ = 8f;
	private const float BeanCapsuleEndZ = 76f;
	private const float BeanMinimumSpawnZ = 84f;
	private const float BeanVisualFloorClearance = 46f;
	private const float AuthoredSpawnLockSeconds = 0.35f;
	private GameObject[] CachedAuthoredPlayerSpawnPoints = Array.Empty<GameObject>();

	private void CacheAuthoredPlayerSpawnPoints()
	{
		var root = Scene.Directory.FindByName( "Player Spawn Points" )
			.FirstOrDefault( x => x.IsValid() && x.Enabled );

		if ( !root.IsValid() )
		{
			CachedAuthoredPlayerSpawnPoints = Array.Empty<GameObject>();
			Log.Warning( "[TapperBeanSpawn] mode='spawn-cache-missing-root' root='Player Spawn Points'" );
			return;
		}

		CachedAuthoredPlayerSpawnPoints = root.Children
			.Where( x => x.IsValid() && x.Enabled )
			.OrderBy( x => x.Name )
			.ToArray();

		foreach ( var renderer in CachedAuthoredPlayerSpawnPoints.SelectMany( x => x.Components.GetAll<ModelRenderer>() ) )
			renderer.Enabled = false;

		Log.Info( $"[TapperBeanSpawn] mode='spawn-cache' root='{root.Name}' count={CachedAuthoredPlayerSpawnPoints.Length} points='{string.Join( ";", CachedAuthoredPlayerSpawnPoints.Select( x => $"{x.Name}@{x.WorldPosition}" ) )}'" );
	}

	private void EnsurePlayerBeans()
	{
		for ( var i = 0; i < Players.Count; i++ )
		{
			var player = Players[i];
			EnsurePlayerBean( player );
			if ( !UseAuthoredScene )
				EnsureBeanAboveFloor( player );
			UpdatePlayerBeanVisuals( player, i );
			if ( UseAuthoredScene && IsAuthoritativeInstance() )
				EnforceAuthoredSpawnLock( player );
		}

		PurgeUntrackedPlayerBeanObjects();
	}

	private void EnsurePlayerBean( PlayerScore player )
	{
		if ( player is null )
			return;

		player.ConnectionKey ??= ConnectionKey( player.Connection );

		if ( Networking.IsActive && !IsAuthoritativeInstance() )
		{
			if ( TryBindRuntimeBean( player ) )
				player.WaitingForSpawn = false;
			else if ( !player.WaitingForSpawn )
			{
				player.WaitingForSpawn = true;
				Log.Info( $"[TapperBeanSpawn] mode='waiting-for-network-bean' player='{player.Name}' key='{player.ConnectionKey}'" );
			}

			return;
		}

		var existingBean = player.Bean.IsValid() && player.BeanController.IsValid();
		if ( UseAuthoredScene && existingBean )
		{
			if ( !TryAssignAuthoredSpawnPoint( player, Players.IndexOf( player ), out var existingSpawnPoint ) )
			{
				if ( !player.WaitingForSpawn )
					Log.Warning( $"[TapperBeanSpawn] mode='authored-queued' player='{player.Name}' key='{player.ConnectionKey}' spawnPoints={GetAuthoredSpawnPoints().Length}" );

				player.WaitingForSpawn = true;
				return;
			}

			player.WaitingForSpawn = false;
			var spawnChanged = !string.Equals( player.SpawnPointName, existingSpawnPoint.Name, StringComparison.Ordinal );
			var spawnTransformChanged = player.AuthoredSpawnPosition.Distance( GetAuthoredBeanSpawnPosition( existingSpawnPoint ) ) > 2f;
			if ( !player.HasAppliedSpawn || spawnChanged || spawnTransformChanged )
				ApplyBeanSpawnTransform( player, player.Bean, existingSpawnPoint, true );
			else
				EnforceAuthoredSpawnLock( player );

			return;
		}

		if ( existingBean )
			return;

		if ( !TryGetBeanSpawnTransform( player, Players.IndexOf( player ), out var spawnPosition, out var spawnRotation, out var spawnPointName ) )
		{
			if ( !player.WaitingForSpawn )
				Log.Warning( $"[TapperBeanSpawn] mode='authored-queued' player='{player.Name}' key='{player.ConnectionKey}' spawnPoints={GetAuthoredSpawnPoints().Length}" );

			player.WaitingForSpawn = true;
			return;
		}

		if ( player.WaitingForSpawn )
			Log.Info( $"[TapperBeanSpawn] mode='authored-queue-resumed' player='{player.Name}' key='{player.ConnectionKey}' position='{spawnPosition}'" );

		player.WaitingForSpawn = false;
		player.SpawnPointName = spawnPointName;
		DestroyStalePlayerBeanObjects( player.ConnectionKey );

		var bean = new GameObject( $"Tapper Bean {player.ConnectionKey}" );
		bean.LocalScale = Vector3.One;
		TeleportBeanToSpawn( bean, spawnPosition, spawnRotation );

		var clothing = ClothingContainer.CreateFromLocalUser();
		var renderer = bean.Components.GetOrCreate<SkinnedModelRenderer>();
		renderer.Model = Model.Load( clothing.PrefersHuman ? "models/citizen_human/citizen_human_female.vmdl" : "models/citizen/citizen.vmdl" );
		renderer.UseAnimGraph = true;
		renderer.Tint = Color.White;

		var dresser = bean.Components.GetOrCreate<Dresser>();
		dresser.Source = Dresser.ClothingSource.LocalUser;
		dresser.BodyTarget = renderer;
		dresser.ApplyHeightScale = true;
		dresser.RemoveUnownedItems = true;
		clothing.Apply( renderer );

		var animation = bean.Components.GetOrCreate<CitizenAnimationHelper>();
		animation.Target = renderer;

		var body = bean.Components.GetOrCreate<Rigidbody>();
		body.Gravity = true;
		body.MotionEnabled = true;
		body.StartAsleep = false;
		body.MassOverride = 500f;
		body.OverrideMassCenter = true;
		body.MassCenterOverride = Vector3.Up * 36f;
		body.LinearDamping = 0.1f;
		body.AngularDamping = 1f;
		body.Locking = new PhysicsLock
		{
			Pitch = true,
			Yaw = true,
			Roll = true
		};

		var collider = bean.Components.GetOrCreate<CapsuleCollider>();
		collider.Radius = BeanCapsuleRadius;
		collider.Start = Vector3.Up * BeanCapsuleStartZ;
		collider.End = Vector3.Up * BeanCapsuleEndZ;
		collider.Static = false;
		collider.IsTrigger = false;

		var controller = bean.Components.GetOrCreate<TapperPlayerBean>();
		controller.Configure( IsLocalPlayer( player ), renderer, animation );

		TeleportBeanToSpawn( bean, spawnPosition, spawnRotation );

		var labelObject = new GameObject( false, $"Tapper Bean {player.ConnectionKey} Name" );
		labelObject.SetParent( bean, true );
		labelObject.LocalPosition = new Vector3( 0f, 0f, 92f );
		labelObject.LocalRotation = Rotation.FromYaw( 35f );
		labelObject.LocalScale = Vector3.One;
		var label = labelObject.Components.GetOrCreate<TextRenderer>();
		label.Scale = 0.24f;
		label.Color = Color.White;
		labelObject.Enabled = true;

		player.Bean = bean;
		player.BeanController = controller;
		player.BeanNameText = label;
		player.HasAppliedSpawn = UseAuthoredScene;
		if ( UseAuthoredScene )
		{
			player.AuthoredSpawnPosition = spawnPosition;
			player.AuthoredSpawnRotation = spawnRotation;
			player.SpawnLockUntilTime = RealTime.Now + AuthoredSpawnLockSeconds;
		}

		SpawnRuntimeBeanForNetwork( player, bean );
		TeleportBeanToSpawn( bean, spawnPosition, spawnRotation );

		Log.Info( $"[TapperBeanSpawn] mode='created-runtime-bean' player='{player.Name}' key='{player.ConnectionKey}' spawnPoint='{spawnPointName}' finalPosition='{bean.WorldPosition}' networked={bean.Network.Active} owner='{bean.Network.OwnerId}' {GetNearestStationDebug( bean.WorldPosition )}" );

		if ( IsLocalPlayer( player ) )
			ConfigureCamera();
	}

	private void ApplyBeanSpawnTransform( PlayerScore player, GameObject bean, GameObject spawnPoint, bool existingBean )
	{
		if ( player is null || !bean.IsValid() || !spawnPoint.IsValid() )
			return;

		var spawnPosition = GetAuthoredBeanSpawnPosition( spawnPoint );
		var positionDelta = bean.WorldPosition.Distance( spawnPosition );
		var spawnChanged = !string.Equals( player.SpawnPointName, spawnPoint.Name, StringComparison.Ordinal );
		if ( !spawnChanged && positionDelta <= 2f )
			return;

		var oldPosition = bean.WorldPosition;
		TeleportBeanToSpawn( bean, spawnPosition, spawnPoint.WorldRotation );
		player.SpawnPointName = spawnPoint.Name;
		player.HasAppliedSpawn = true;
		player.AuthoredSpawnPosition = spawnPosition;
		player.AuthoredSpawnRotation = spawnPoint.WorldRotation;
		player.SpawnLockUntilTime = RealTime.Now + AuthoredSpawnLockSeconds;

		Log.Info( $"[TapperBeanSpawn] mode='{(existingBean ? "authored-reposition-existing" : "authored-assigned")}' player='{player.Name}' spawnPoint='{spawnPoint.Name}' from='{oldPosition}' markerPosition='{spawnPoint.WorldPosition}' finalPosition='{bean.WorldPosition}' {GetNearestStationDebug( bean.WorldPosition )}" );
	}

	private void EnforceAuthoredSpawnLock( PlayerScore player )
	{
		if ( player is null || !UseAuthoredScene || !player.Bean.IsValid() )
			return;

		if ( RealTime.Now > player.SpawnLockUntilTime )
			return;

		if ( player.Bean.WorldPosition.Distance( player.AuthoredSpawnPosition ) <= 4f )
			return;

		var oldPosition = player.Bean.WorldPosition;
		TeleportBeanToSpawn( player.Bean, player.AuthoredSpawnPosition, player.AuthoredSpawnRotation );

		Log.Info( $"[TapperBeanSpawn] mode='authored-lock-corrected' player='{player.Name}' from='{oldPosition}' finalPosition='{player.Bean.WorldPosition}' lockRemaining={player.SpawnLockUntilTime - RealTime.Now:0.##}" );
	}

	private static void TeleportBeanToSpawn( GameObject bean, Vector3 position, Rotation rotation )
	{
		if ( !bean.IsValid() )
			return;

		bean.WorldPosition = position;
		bean.WorldRotation = rotation;
		bean.Transform.ClearInterpolation();

		var body = bean.Components.Get<Rigidbody>();
		if ( !body.IsValid() )
			return;

		body.Velocity = Vector3.Zero;
		body.AngularVelocity = Vector3.Zero;
		body.Sleeping = false;

		var physicsBody = body.PhysicsBody;
		if ( physicsBody is null )
			return;

		physicsBody.Position = position;
		physicsBody.Rotation = rotation;
		physicsBody.Velocity = Vector3.Zero;
		physicsBody.AngularVelocity = Vector3.Zero;
		physicsBody.Sleeping = false;
	}

	private void SpawnRuntimeBeanForNetwork( PlayerScore player, GameObject bean )
	{
		if ( player is null || !bean.IsValid() || !Networking.IsActive || !IsAuthoritativeInstance() || bean.Network.Active )
			return;

		var options = new NetworkSpawnOptions
		{
			StartEnabled = true,
			AlwaysTransmit = true,
			Owner = player.Connection,
			OwnerTransfer = OwnerTransfer.Fixed
		};

		if ( !bean.NetworkSpawn( options ) )
			Log.Warning( $"[TapperBeanSpawn] mode='network-spawn-failed' player='{player.Name}' key='{player.ConnectionKey}' position='{bean.WorldPosition}'" );
	}

	private void DestroyStalePlayerBeanObjects( string connectionKey )
	{
		if ( string.IsNullOrWhiteSpace( connectionKey ) )
			return;

		var beanName = $"Tapper Bean {connectionKey}";
		var labelName = $"Tapper Bean {connectionKey} Name";
		foreach ( var gameObject in Scene.Directory.FindByName( beanName ).Concat( Scene.Directory.FindByName( labelName ) ).ToArray() )
		{
			if ( gameObject.IsValid() )
				gameObject.Destroy();
		}
	}

	private void PurgeUntrackedPlayerBeanObjects()
	{
		var trackedObjects = Players
			.SelectMany( x => new[] { x.Bean, x.BeanNameText?.GameObject } )
			.Where( x => x.IsValid() )
			.ToHashSet();

		var expectedNames = Players
			.SelectMany( x =>
			{
				var key = x.ConnectionKey ?? ConnectionKey( x.Connection );
				return new[] { $"Tapper Bean {key}", $"Tapper Bean {key} Name" };
			} )
			.ToHashSet( StringComparer.Ordinal );

		foreach ( var gameObject in Scene.GetAllObjects( true ).Where( IsTapperBeanObject ).ToArray() )
		{
			if ( trackedObjects.Contains( gameObject ) )
				continue;

			var duplicateTrackedName = expectedNames.Contains( gameObject.Name );
			var unexpectedName = !expectedNames.Contains( gameObject.Name );
			if ( !duplicateTrackedName && !unexpectedName )
				continue;

			Log.Info( $"[TapperBeanSpawn] mode='destroy-untracked-bean' object='{gameObject.Name}' position='{gameObject.WorldPosition}' duplicateName={duplicateTrackedName} unexpectedName={unexpectedName} {GetNearestStationDebug( gameObject.WorldPosition )}" );
			gameObject.Destroy();
		}

		foreach ( var controller in Scene.GetAllComponents<TapperPlayerBean>().Where( x => x.IsValid() && x.GameObject.IsValid() ).ToArray() )
		{
			if ( trackedObjects.Contains( controller.GameObject ) )
				continue;

			Log.Info( $"[TapperBeanSpawn] mode='destroy-orphan-controller' object='{controller.GameObject.Name}' position='{controller.GameObject.WorldPosition}' {GetNearestStationDebug( controller.GameObject.WorldPosition )}" );
			controller.GameObject.Destroy();
		}
	}

	private static bool IsTapperBeanObject( GameObject gameObject )
	{
		return gameObject.IsValid()
			&& gameObject.Name.StartsWith( "Tapper Bean ", StringComparison.Ordinal );
	}

	private bool TryBindRuntimeBean( PlayerScore player )
	{
		if ( player is null )
			return false;

		if ( player.Bean.IsValid() && player.BeanController.IsValid() )
			return true;

		var bean = Scene.Directory.FindByName( $"Tapper Bean {player.ConnectionKey}" )
			.FirstOrDefault( x => x.IsValid() && x.Network.Active );
		if ( !bean.IsValid() )
			return false;

		var controller = bean.Components.Get<TapperPlayerBean>();
		if ( !controller.IsValid() )
			return false;

		var label = Scene.Directory.FindByName( $"Tapper Bean {player.ConnectionKey} Name" )
			.FirstOrDefault( x => x.IsValid() && x.Parent == bean )
			?.Components.Get<TextRenderer>();

		player.Bean = bean;
		player.BeanController = controller;
		player.BeanNameText = label;
		player.HasAppliedSpawn = true;

		Log.Info( $"[TapperBeanSpawn] mode='bound-network-bean' player='{player.Name}' key='{player.ConnectionKey}' position='{bean.WorldPosition}' owner='{bean.Network.OwnerId}'" );
		return true;
	}

	private void UpdatePlayerBeanVisuals( PlayerScore player, int slot )
	{
		if ( player is null )
			return;

		if ( !player.Bean.IsValid() )
			return;

		if ( player.BeanController.IsValid() )
		{
			var isLocalPlayer = IsLocalPlayer( player );
			player.BeanController.IsLocalPlayer = isLocalPlayer;
			player.BeanController.CameraYaw = ThirdPersonCameraYaw;
			player.BeanController.CameraPitch = ThirdPersonCameraPitch;
			player.BeanController.LookTarget = ResolveBeanLookTarget( player, isLocalPlayer );
			player.BeanController.IsFirstPersonView = isLocalPlayer && IsFirstPersonCameraActive();
			player.BeanController.Happiness = player.Heat;
		}

		if ( player.BeanNameText.IsValid() )
		{
			var localFirstPerson = IsLocalPlayer( player ) && IsFirstPersonCameraActive();
			player.BeanNameText.GameObject.Enabled = !localFirstPerson;
			var stationText = player.StationIndex >= 0 ? $"S{player.StationIndex + 1}" : "UNCLAIMED";
			SetText( player.BeanNameText, $"{player.Name}\n{stationText}" );
			player.BeanNameText.Color = player.StationIndex >= 0 ? ReadyStationColor : Color.White;
		}

	}

	private bool IsLocalPlayer( PlayerScore player )
	{
		return player is not null && (player.ConnectionKey ?? ConnectionKey( player.Connection )) == ConnectionKey( Connection.Local );
	}

	private Vector3 ResolveBeanLookTarget( PlayerScore player, bool isLocalPlayer )
	{
		if ( player is null || !player.Bean.IsValid() )
			return GetVenueStageOrigin();

		var eyePosition = GetBeanEyePosition( player );
		if ( !isLocalPlayer )
			return eyePosition + NormalizeLookDirection( player.LookDirection, Vector3.Forward ) * 1000f;

		var camera = Scene.Camera;
		if ( !camera.IsValid() )
		{
			var fallbackDirection = NormalizeLookDirection( Rotation.From( ThirdPersonCameraPitch, ThirdPersonCameraYaw, 0f ).Forward, player.LookDirection );
			player.LookDirection = fallbackDirection;
			TryPublishLocalLookDirection( player, fallbackDirection );
			return eyePosition + fallbackDirection * 1000f;
		}

		Vector3 target;

		if ( IsCameraLookActive() )
		{
			target = camera.WorldPosition + camera.WorldRotation.Forward * 1000f;
		}
		else
		{
			var ray = camera.ScreenPixelToRay( Mouse.Position );
			var trace = Scene.Trace
				.Ray( ray, 10000f )
				.IgnoreGameObjectHierarchy( player.Bean )
				.WithoutTags( "trigger" )
				.Run();

			target = trace.Hit ? trace.EndPosition : ray.Position + ray.Forward * 10000f;
		}

		var direction = NormalizeLookDirection( target - eyePosition, player.LookDirection );
		player.LookDirection = direction;
		TryPublishLocalLookDirection( player, direction );
		return eyePosition + direction * MathF.Max( 1000f, eyePosition.Distance( target ) );
	}

	private void TryPublishLocalLookDirection( PlayerScore player, Vector3 direction )
	{
		if ( player is null )
			return;

		var now = RealTime.Now;
		var changed = direction.Distance( player.LastSentLookDirection ) > 0.025f;
		if ( !changed && now - player.LastLookPublishTime < 0.08f )
			return;

		player.LastSentLookDirection = direction;
		player.LastLookPublishTime = now;

		if ( Networking.IsActive && !Networking.IsHost )
			RequestPlayerLookDirection( direction.x, direction.y, direction.z );
	}

	private static Vector3 GetBeanEyePosition( PlayerScore player )
	{
		return player.Bean.WorldPosition + Vector3.Up * 72f;
	}

	private Vector3 GetBeanSpawnPosition( int slot )
	{
		var lane = slot < 0 ? 0 : slot;
		var stage = GetVenueStageOrigin();
		var firstY = MathF.Max( CurrentRoomLayout.LeftWallY + 360f, -560f );
		var y = MathF.Min( firstY + lane * 110f, CurrentRoomLayout.RightWallY - 360f );
		var floorCenterZ = CurrentRoomLayout.FloorThickness + PixelGrassFloorHeightAboveFloor;
		var floorTopZ = GetPixelGrassFloorTopZ();
		var spawnZ = GetMinimumBeanSpawnZ();
		var position = stage + new Vector3( -CurrentRoomLayout.FloorWidth * 0.18f, y, spawnZ );

		Log.Info( $"[TapperBeanSpawn] mode='spawn' slot={slot} position='{position}' floorCenterZ={floorCenterZ:0.##} floorTopZ={floorTopZ:0.##} spawnZ={spawnZ:0.##} capsuleRadius={BeanCapsuleRadius:0.##} capsuleStartZ={BeanCapsuleStartZ:0.##} capsuleEndZ={BeanCapsuleEndZ:0.##} floorHeightAboveFloor={PixelGrassFloorHeightAboveFloor:0.##}" );
		return position;
	}

	private bool TryGetBeanSpawnTransform( PlayerScore player, int slot, out Vector3 position, out Rotation rotation, out string spawnPointName )
	{
		spawnPointName = "";

		if ( UseAuthoredScene )
		{
			if ( TryAssignAuthoredSpawnPoint( player, slot, out var spawnPoint ) )
			{
				position = GetAuthoredBeanSpawnPosition( spawnPoint );
				Log.Info( $"[TapperBeanSpawn] mode='authored-assigned' slot={slot} spawnPoint='{spawnPoint.Name}' markerPosition='{spawnPoint.WorldPosition}' finalPosition='{position}' rotation='{spawnPoint.WorldRotation}' count={GetAuthoredSpawnPoints().Length}" );
				rotation = spawnPoint.WorldRotation;
				spawnPointName = spawnPoint.Name;
				return true;
			}

			position = default;
			rotation = Rotation.Identity;
			return false;
		}

		position = GetBeanSpawnPosition( slot );
		rotation = Rotation.FromYaw( 0f );
		return true;
	}

	private bool TryAssignAuthoredSpawnPoint( PlayerScore player, int slot, out GameObject spawnPoint )
	{
		var spawnPoints = GetAuthoredSpawnPoints();
		spawnPoint = default;

		if ( spawnPoints.Length == 0 )
			return false;

		if ( !string.IsNullOrWhiteSpace( player?.SpawnPointName ) )
		{
			var assigned = spawnPoints.FirstOrDefault( x => string.Equals( x.Name, player.SpawnPointName, StringComparison.Ordinal ) );
			if ( assigned.IsValid() && !IsAuthoredSpawnPointReserved( player, assigned.Name ) )
			{
				spawnPoint = assigned;
				return true;
			}
		}

		var index = Math.Clamp( slot < 0 ? 0 : slot, 0, int.MaxValue ) % spawnPoints.Length;
		for ( var offset = 0; offset < spawnPoints.Length; offset++ )
		{
			var candidate = spawnPoints[(index + offset) % spawnPoints.Length];
			if ( IsAuthoredSpawnPointReserved( player, candidate.Name ) )
				continue;

			spawnPoint = candidate;
			return true;
		}

		return false;
	}

	private bool IsAuthoredSpawnPointReserved( PlayerScore player, string spawnPointName )
	{
		return Players.Any( other => other != player
			&& other.Bean.IsValid()
			&& string.Equals( other.SpawnPointName, spawnPointName, StringComparison.Ordinal ) );
	}

	private GameObject[] GetAuthoredSpawnPoints()
	{
		var spawnPoints = CachedAuthoredPlayerSpawnPoints
			.Where( x => x.IsValid() && x.Enabled )
			.OrderBy( x => x.Name )
			.ToArray();

		if ( spawnPoints.Length == 0 && UseAuthoredScene )
		{
			CacheAuthoredPlayerSpawnPoints();
			spawnPoints = CachedAuthoredPlayerSpawnPoints
				.Where( x => x.IsValid() && x.Enabled )
				.OrderBy( x => x.Name )
				.ToArray();
		}

		return spawnPoints;
	}

	private string GetNearestStationDebug( Vector3 position )
	{
		var nearestStation = Stations
			.OrderBy( x => position.Distance( x.Origin ) )
			.FirstOrDefault();

		if ( nearestStation is null )
			return "nearestStation='none'";

		return $"nearestStation={nearestStation.Index} stationOrigin='{nearestStation.Origin}' stationDistance={position.Distance( nearestStation.Origin ):0.##}";
	}

	private Vector3 GetAuthoredBeanSpawnPosition( GameObject spawnPoint )
	{
		if ( !spawnPoint.IsValid() )
			return new Vector3( 0f, 0f, GetMinimumBeanSpawnZ() );

		var marker = spawnPoint.WorldPosition;
		return marker + Vector3.Up * BeanCapsuleStartZ;
	}

	private void EnsureBeanAboveFloor( PlayerScore player )
	{
		if ( player is null || !player.Bean.IsValid() )
			return;

		var minimumZ = GetMinimumBeanSpawnZ();
		if ( player.Bean.WorldPosition.z >= minimumZ )
			return;

		var oldPosition = player.Bean.WorldPosition;
		player.Bean.WorldPosition = oldPosition.WithZ( minimumZ );

		var body = player.Bean.Components.Get<Rigidbody>();
		if ( body.IsValid() && body.Velocity.z < 0f )
			body.Velocity = body.Velocity.WithZ( 0f );

		Log.Info( $"[TapperBeanSpawn] mode='lift-existing' player='{player.Name}' from='{oldPosition}' to='{player.Bean.WorldPosition}' floorCenterZ={CurrentRoomLayout.FloorThickness + PixelGrassFloorHeightAboveFloor:0.##} floorTopZ={GetPixelGrassFloorTopZ():0.##} spawnZ={minimumZ:0.##}" );
	}

	private float GetMinimumBeanSpawnZ()
	{
		return MathF.Max( BeanMinimumSpawnZ, GetPixelGrassFloorTopZ() + BeanVisualFloorClearance );
	}

	private float GetPixelGrassFloorTopZ()
	{
		var floorCenterZ = CurrentRoomLayout.FloorThickness + PixelGrassFloorHeightAboveFloor;
		var floorColliderHalfZ = MathF.Max( 8f, CurrentRoomLayout.FloorThickness ) * 0.5f;
		return floorCenterZ + floorColliderHalfZ;
	}

	private bool IsPlayerCloseEnoughToClaim( PlayerScore player, TapperStation station )
	{
		if ( player?.BeanController is null || !player.BeanController.IsValid() )
			return true;

		var playerPosition = player.Bean.IsValid() ? player.Bean.WorldPosition : player.BeanController.WorldPosition;
		if ( HasStationBounds( station ) )
			return IsPlayerInsideStationBounds( playerPosition, station );

		return TapperStationInteractionRules.IsWithinClaimRange2D(
			playerPosition.x - station.Origin.x,
			playerPosition.y - station.Origin.y,
			player.BeanController.ClaimRange );
	}

	private bool IsPlayerInsideStationBounds( PlayerScore player, TapperStation station )
	{
		if ( player?.BeanController is null || !player.BeanController.IsValid() )
			return true;

		var playerPosition = player.Bean.IsValid() ? player.Bean.WorldPosition : player.BeanController.WorldPosition;
		return IsPlayerInsideStationBounds( playerPosition, station );
	}

	private static bool IsPlayerInsideStationBounds( Vector3 playerPosition, TapperStation station )
	{
		if ( !HasStationBounds( station ) )
			return false;

		var halfExtents = station.ClaimBoundsHalfExtentsLocal;
		var center = GetStationLocalPointWorldPosition( station.Root, station.ClaimBoundsCenterLocal );
		var relative = playerPosition - center;
		var scale = station.Root.LocalScale;
		var localX = Dot( relative, station.Root.WorldRotation.Forward ) / SafeScaleAxis( scale.x );
		var localY = Dot( relative, station.Root.WorldRotation.Left ) / SafeScaleAxis( scale.y );

		return TapperStationInteractionRules.IsWithinStationBounds2D( localX, localY, halfExtents.x, halfExtents.y );
	}

	private static bool HasStationBounds( TapperStation station )
	{
		return station?.Root is not null
			&& station.Root.IsValid()
			&& station.ClaimBoundsHalfExtentsLocal.x > 0f
			&& station.ClaimBoundsHalfExtentsLocal.y > 0f;
	}

	private static float Dot( Vector3 left, Vector3 right )
	{
		return left.x * right.x + left.y * right.y + left.z * right.z;
	}

	private static float SafeScaleAxis( float value )
	{
		var magnitude = MathF.Abs( value );
		return magnitude <= 0.0001f ? 1f : magnitude;
	}

	private PlayerScore GetLocalPlayer()
	{
		var key = ConnectionKey( Connection.Local );
		return PlayersByConnection.TryGetValue( key, out var player ) ? player : null;
	}

	private void DeletePlayerBean( PlayerScore player )
	{
		if ( player?.BeanNameText is not null && player.BeanNameText.GameObject.IsValid() )
			player.BeanNameText.GameObject.Destroy();

		if ( player?.Bean is not null && player.Bean.IsValid() )
			player.Bean.Destroy();

		if ( player is null )
			return;

		player.Bean = null;
		player.BeanController = null;
		player.BeanNameText = null;
		player.SpawnPointName = "";
		player.HasAppliedSpawn = false;
		player.AuthoredSpawnPosition = default;
		player.AuthoredSpawnRotation = Rotation.Identity;
		player.SpawnLockUntilTime = 0f;
	}
}
