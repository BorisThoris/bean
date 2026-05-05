using Sandbox;
using System;
using System.Linq;

public sealed partial class PhysicalFastestTapperGame
{
	private const float ThirdPersonCameraDistanceDefault = 330f;
	private const float ThirdPersonCameraDistanceMin = 0f;
	private const float ThirdPersonCameraDistanceMax = 520f;
	private const float ThirdPersonCameraZoomStep = 48f;
	private const float FirstPersonCameraDistanceThreshold = 32f;
	private const float ThirdPersonCameraHeight = 92f;

	private void ApplyCursorState()
	{
		Mouse.Visibility = IsCameraLookActive() ? MouseVisibility.Hidden : MouseVisibility.Visible;
		Mouse.CursorType = "pointer";
	}

	private void ConfigureCamera()
	{
		var camera = EnsureGameplayCamera();
		if ( !camera.IsValid() )
			return;

		var mode = GetCameraMode();
		UpdateBeanCameraInput( mode == CameraMode.Bean );
		if ( mode == CameraMode.Bean )
		{
			ConfigureBeanCamera( camera );
			return;
		}

		var localStation = Stations.FirstOrDefault( x => x.Index == GetLocalStationIndex() );
		if ( mode == CameraMode.Station && localStation is not null )
		{
			var target = localStation.Origin + new Vector3( 12f, 18f, 170f );
			camera.WorldPosition = ClampCameraInsideRoomBounds( localStation.Origin + new Vector3( -520f, -156f, 286f ) );
			AimCameraAt( camera, target );
			camera.FieldOfView = 64f;
			return;
		}

		var overviewStage = GetVenueStageOrigin();
		camera.WorldPosition = ClampCameraInsideRoomBounds( overviewStage + new Vector3( -CurrentRoomLayout.FloorWidth * 0.38f, 0f, 470f ) );
		AimCameraAt( camera, overviewStage + new Vector3( 120f, 0f, 180f ) );
		camera.FieldOfView = 74f;
	}

	private CameraMode GetCameraMode()
	{
		var local = GetLocalPlayer();

		if ( local is not null && local.Bean.IsValid() && !local.Spectating )
			return CameraMode.Bean;

		return CameraMode.Spectator;
	}

	private void ConfigureBeanCamera( CameraComponent camera )
	{
		if ( IsFirstPersonCameraActive() )
		{
			ConfigureFirstPersonCamera( camera );
			return;
		}

		ConfigureThirdPersonCamera( camera );
	}

	private void ConfigureThirdPersonCamera( CameraComponent camera )
	{
		var local = GetLocalPlayer();
		var beanPosition = local?.Bean?.WorldPosition ?? GetVenueStageOrigin();
		var target = beanPosition + new Vector3( 0f, 0f, ThirdPersonCameraHeight );
		var rotation = Rotation.From( ThirdPersonCameraPitch, ThirdPersonCameraYaw, 0f );
		var offset = rotation.Backward * BeanCameraDistance + Vector3.Up * 12f;

		camera.WorldPosition = ClampCameraInsideRoomBounds( target + offset );
		AimCameraAt( camera, target + rotation.Forward * 45f );
		camera.FieldOfView = 72f;
		LogBeanCameraDebug( "thirdperson", local, camera );
	}

	private void ConfigureFirstPersonCamera( CameraComponent camera )
	{
		var local = GetLocalPlayer();
		var beanPosition = local?.Bean?.WorldPosition ?? GetVenueStageOrigin();
		var eyePosition = beanPosition + new Vector3( 0f, 0f, ThirdPersonCameraHeight );
		var rotation = Rotation.From( ThirdPersonCameraPitch, ThirdPersonCameraYaw, 0f );

		camera.WorldPosition = ClampCameraInsideRoomBounds( eyePosition );
		camera.WorldRotation = rotation;
		camera.FieldOfView = 78f;
		LogBeanCameraDebug( "firstperson", local, camera );
	}

	private CameraComponent EnsureGameplayCamera()
	{
		var camera = Scene.Camera;
		if ( camera.IsValid() )
		{
			ActivateGameplayCamera( camera );
			return camera;
		}

		camera = Scene.GetAllComponents<CameraComponent>()
			.FirstOrDefault( x => x.IsValid() && x.IsMainCamera );
		if ( camera.IsValid() )
		{
			ActivateGameplayCamera( camera );
			return camera;
		}

		var cameraObject = FindOrCreate( "Tapper Gameplay Camera" );
		cameraObject.Enabled = true;
		camera = cameraObject.Components.GetOrCreate<CameraComponent>();
		ActivateGameplayCamera( camera );
		Log.Info( "[TapperCamera] created='Tapper Gameplay Camera' reason='missing-scene-camera'" );
		return camera;
	}

	private void ActivateGameplayCamera( CameraComponent camera )
	{
		if ( !camera.IsValid() )
			return;

		foreach ( var other in Scene.GetAllComponents<CameraComponent>().Where( x => x.IsValid() && x != camera ) )
			other.IsMainCamera = false;

		camera.GameObject.Enabled = true;
		camera.Enabled = true;
		camera.IsMainCamera = true;
		camera.Priority = 10;
		camera.ZNear = 4f;
		camera.ZFar = 10000f;
	}

	private void LogBeanCameraDebug( string mode, PlayerScore local, CameraComponent camera )
	{
		if ( local is null || !local.Bean.IsValid() || !camera.IsValid() )
			return;

		if ( RealTime.Now - LastCameraDebugLogTime < 1.5f )
			return;

		LastCameraDebugLogTime = RealTime.Now;
		Log.Info( $"[TapperCamera] mode='Bean' view='{mode}' player='{local.ConnectionKey}' bean='{local.Bean.WorldPosition}' camera='{camera.WorldPosition}' fov={camera.FieldOfView:0.#}" );
	}

	private Vector3 ClampCameraInsideRoomBounds( Vector3 position )
	{
		var layout = CurrentRoomLayout;
		var inset = VenueBoundaryWallThickness * 0.5f;
		var minX = RuntimeRoomLayoutMath.FrontWallX( layout ) + inset;
		var maxX = layout.RearWallX - inset;
		var minY = layout.LeftWallY + inset;
		var maxY = layout.RightWallY - inset;

		if ( minX >= maxX || minY >= maxY )
			return position;

		return new Vector3(
			position.x.Clamp( minX, maxX ),
			position.y.Clamp( minY, maxY ),
			position.z );
	}

	private void UpdateBeanCameraInput( bool isBeanCamera )
	{
		if ( isBeanCamera )
		{
			var wheel = Input.MouseWheel.y;
			if ( MathF.Abs( wheel ) > 0.001f )
				BeanCameraDistance = (BeanCameraDistance - wheel * ThirdPersonCameraZoomStep).Clamp( ThirdPersonCameraDistanceMin, ThirdPersonCameraDistanceMax );
		}

		if ( !IsCameraLookActive() )
			return;

		var look = Input.AnalogLook;
		ThirdPersonCameraYaw += look.yaw;
		ThirdPersonCameraPitch = (ThirdPersonCameraPitch + look.pitch).Clamp( -65f, 65f );
	}

	private bool IsCameraLookActive()
	{
		return IsFirstPersonCameraActive() || Input.Down( "attack2" ) || Input.Down( "MOUSE2" );
	}

	private bool IsFirstPersonCameraActive()
	{
		return GetCameraMode() == CameraMode.Bean && BeanCameraDistance <= FirstPersonCameraDistanceThreshold;
	}

	private static void AimCameraAt( CameraComponent camera, Vector3 target )
	{
		var direction = target - camera.WorldPosition;
		if ( direction.LengthSquared < 1f )
			return;

		camera.WorldRotation = Rotation.LookAt( direction.Normal );
	}
}
