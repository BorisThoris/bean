using Sandbox;
using System.Linq;

public sealed partial class PhysicalFastestTapperGame
{
	private const float ThirdPersonCameraDistance = 330f;
	private const float ThirdPersonCameraHeight = 92f;

	private void ApplyCursorState()
	{
		Mouse.Visibility = IsThirdPersonLookActive() ? MouseVisibility.Hidden : MouseVisibility.Visible;
		Mouse.CursorType = "pointer";
	}

	private void ConfigureCamera()
	{
		var camera = Scene.Camera;
		if ( !camera.IsValid() )
			return;

		UpdateThirdPersonCameraInput();

		var mode = GetCameraMode();
		if ( mode == CameraMode.Bean )
		{
			ConfigureThirdPersonCamera( camera );
			return;
		}

		var localStation = Stations.FirstOrDefault( x => x.Index == GetLocalStationIndex() );
		if ( mode == CameraMode.Station && localStation is not null )
		{
			var target = localStation.Origin + new Vector3( 12f, 18f, 170f );
			camera.WorldPosition = localStation.Origin + new Vector3( -520f, -156f, 286f );
			AimCameraAt( camera, target );
			camera.FieldOfView = 64f;
			return;
		}

		var overviewStage = GetVenueStageOrigin();
		camera.WorldPosition = overviewStage + new Vector3( -CurrentRoomLayout.FloorWidth * 0.38f, 0f, 470f );
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

	private void ConfigureThirdPersonCamera( CameraComponent camera )
	{
		var local = GetLocalPlayer();
		var beanPosition = local?.Bean?.WorldPosition ?? GetVenueStageOrigin();
		var target = beanPosition + new Vector3( 0f, 0f, ThirdPersonCameraHeight );
		var rotation = Rotation.From( ThirdPersonCameraPitch, ThirdPersonCameraYaw, 0f );
		var offset = rotation.Backward * ThirdPersonCameraDistance + Vector3.Up * 12f;

		camera.WorldPosition = target + offset;
		AimCameraAt( camera, target + rotation.Forward * 45f );
		camera.FieldOfView = 72f;
	}

	private void UpdateThirdPersonCameraInput()
	{
		if ( !IsThirdPersonLookActive() )
			return;

		var look = Input.AnalogLook;
		ThirdPersonCameraYaw += look.yaw;
		ThirdPersonCameraPitch = (ThirdPersonCameraPitch + look.pitch).Clamp( -25f, 65f );
	}

	private static bool IsThirdPersonLookActive()
	{
		return Input.Down( "attack2" ) || Input.Down( "MOUSE2" );
	}

	private static void AimCameraAt( CameraComponent camera, Vector3 target )
	{
		var direction = target - camera.WorldPosition;
		if ( direction.LengthSquared < 1f )
			return;

		camera.WorldRotation = Rotation.LookAt( direction.Normal );
	}
}
