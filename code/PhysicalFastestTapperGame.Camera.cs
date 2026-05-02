using Sandbox;
using System.Linq;

public sealed partial class PhysicalFastestTapperGame
{
	private void ApplyCursorState()
	{
		Mouse.Visibility = MouseVisibility.Visible;
		Mouse.CursorType = "pointer";
	}

	private void ConfigureCamera()
	{
		var camera = Scene.Camera;
		if ( !camera.IsValid() )
			return;

		var mode = GetCameraMode();
		if ( mode == CameraMode.Bean )
		{
			var local = GetLocalPlayer();
			var target = local?.Bean?.WorldPosition ?? GetVenueStageOrigin();
			camera.WorldPosition = target + new Vector3( -240f, -250f, 190f );
			AimCameraAt( camera, target + new Vector3( 65f, 60f, 70f ) );
			camera.FieldOfView = 72f;
			return;
		}

		if ( mode == CameraMode.Results )
		{
			var targetY = Stations.FirstOrDefault( x => x.Index == LastWinnerStation )?.Origin.y ?? 0f;
			var stage = GetVenueStageOrigin();
			var target = stage + new Vector3( 310f, (targetY - stage.y) * 0.1f, 150f );
			camera.WorldPosition = stage + new Vector3( -620f, 310f + (targetY - stage.y) * 0.2f, 330f );
			AimCameraAt( camera, target );
			camera.FieldOfView = VenueMapLoaded ? 72f : 68f;
			return;
		}

		var localStation = Stations.FirstOrDefault( x => x.Index == GetLocalStationIndex() );
		if ( mode == CameraMode.Station && localStation is not null )
		{
			var target = localStation.Origin + new Vector3( 12f, 18f, 170f );
			camera.WorldPosition = localStation.Origin + (VenueMapLoaded ? new Vector3( -430f, -205f, 248f ) : new Vector3( -520f, -156f, 286f ));
			AimCameraAt( camera, target );
			camera.FieldOfView = VenueMapLoaded ? 68f : 64f;
			return;
		}

		var overviewStage = GetVenueStageOrigin();
		camera.WorldPosition = overviewStage + (VenueMapLoaded ? new Vector3( -760f, 520f, 380f ) : new Vector3( -980f, 0f, 470f ));
		AimCameraAt( camera, overviewStage + new Vector3( 120f, 0f, 180f ) );
		camera.FieldOfView = VenueMapLoaded ? 78f : 74f;
	}

	private CameraMode GetCameraMode()
	{
		var localStationIndex = GetLocalStationIndex();
		var local = GetLocalPlayer();
		if ( local is not null && local.StationIndex < 0 && !local.Spectating )
			return CameraMode.Bean;

		if ( State is RoundState.Results or RoundState.Intermission && LastWinnerStation >= 0 )
			return CameraMode.Results;

		if ( Stations.Any( x => x.Index == localStationIndex ) )
			return CameraMode.Station;

		return CameraMode.Spectator;
	}

	private static void AimCameraAt( CameraComponent camera, Vector3 target )
	{
		var direction = target - camera.WorldPosition;
		if ( direction.LengthSquared < 1f )
			return;

		camera.WorldRotation = Rotation.LookAt( direction.Normal );
	}
}
