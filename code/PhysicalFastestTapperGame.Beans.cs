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
			player.BeanController.IsLocalPlayer = IsLocalPlayer( player );
			player.BeanController.CameraYaw = ThirdPersonCameraYaw;
		}

		if ( player.BeanNameText.IsValid() )
		{
			player.BeanNameText.GameObject.Enabled = true;
			var stationText = player.StationIndex >= 0 ? $"S{player.StationIndex + 1}" : "UNCLAIMED";
			SetText( player.BeanNameText, $"{player.Name}\n{stationText}" );
			player.BeanNameText.Color = player.StationIndex >= 0 ? ReadyStationColor : Color.White;
		}

	}

	private bool IsLocalPlayer( PlayerScore player )
	{
		return player is not null && (player.ConnectionKey ?? ConnectionKey( player.Connection )) == ConnectionKey( Connection.Local );
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
