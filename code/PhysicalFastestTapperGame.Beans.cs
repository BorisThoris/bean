using Sandbox;
using Sandbox.Citizen;
using Sandbox.Movement;
using System;

public sealed partial class PhysicalFastestTapperGame
{
	private void EnsurePlayerBeans()
	{
		for ( var i = 0; i < Players.Count; i++ )
		{
			var player = Players[i];
			EnsurePlayerBean( player );
			UpdatePlayerBeanVisuals( player, i );
		}
	}

	private void EnsurePlayerBean( PlayerScore player )
	{
		if ( player is null )
			return;

		if ( player.Bean.IsValid() && player.BeanController.IsValid() )
			return;

		player.ConnectionKey ??= ConnectionKey( player.Connection );
		var bean = FindOrCreate( $"Tapper Bean {player.ConnectionKey}" );
		bean.LocalPosition = GetBeanSpawnPosition( Players.IndexOf( player ) );
		bean.LocalRotation = Rotation.FromYaw( 0f );
		bean.LocalScale = Vector3.One;

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
		collider.Radius = 16f;
		collider.Start = Vector3.Up * 8f;
		collider.End = Vector3.Up * 76f;
		collider.Static = false;
		collider.IsTrigger = false;

		var playerController = bean.Components.GetOrCreate<PlayerController>();
		playerController.Body = body;
		playerController.Renderer = renderer;
		playerController.BodyHeight = 72f;
		playerController.BodyRadius = 16f;
		playerController.BodyMass = 500f;
		playerController.WalkSpeed = 185f;
		playerController.RunSpeed = 300f;
		playerController.RunByDefault = false;
		playerController.AltMoveButton = "run";
		playerController.UseInputControls = false;
		playerController.UseLookControls = false;
		playerController.UseCameraControls = false;
		playerController.UseAnimatorControls = false;
		playerController.ThirdPerson = false;
		playerController.CameraOffset = Vector3.Zero;

		var walkMode = bean.Components.GetOrCreate<MoveModeWalk>();
		walkMode.StepUpHeight = 18f;
		walkMode.StepDownHeight = 18f;
		walkMode.GroundAngle = 45f;

		var controller = bean.Components.GetOrCreate<TapperPlayerBean>();
		controller.Configure( IsLocalPlayer( player ), renderer, animation );

		var labelObject = FindOrCreate( $"Tapper Bean {player.ConnectionKey} Name" );
		labelObject.SetParent( bean, true );
		labelObject.LocalPosition = new Vector3( 0f, 0f, 92f );
		labelObject.LocalRotation = Rotation.FromYaw( 35f );
		labelObject.LocalScale = Vector3.One;
		var label = labelObject.Components.GetOrCreate<TextRenderer>();
		label.Scale = 0.24f;
		label.Color = Color.White;

		player.Bean = bean;
		player.BeanController = controller;
		player.BeanNameText = label;
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
		return stage + new Vector3( -CurrentRoomLayout.FloorWidth * 0.18f, y, 84f );
	}

	private bool IsPlayerCloseEnoughToClaim( PlayerScore player, TapperStation station )
	{
		if ( player?.BeanController is null || !player.BeanController.IsValid() )
			return true;

		return player.BeanController.IsWithinClaimRange( station.Origin );
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
	}
}
