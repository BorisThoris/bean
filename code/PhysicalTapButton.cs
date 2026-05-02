using Sandbox;

[Category( "Gameplay" ), Icon( "ads_click" )]
public sealed class PhysicalTapButton : Component
{
	[Property] public int StationIndex { get; set; }

	private PhysicalFastestTapperGame Game;

	protected override void OnStart()
	{
		FindGameController();
	}

	protected override void OnUpdate()
	{
		if ( !Game.IsValid() )
			FindGameController();

		if ( !Input.Keyboard.Pressed( "MOUSE1" ) )
			return;

		var camera = Scene.Camera;
		if ( !camera.IsValid() )
			return;

		var ray = camera.ScreenPixelToRay( Mouse.Position );
		var trace = Scene.Trace
			.Ray( ray, 10000.0f )
			.HitTriggers()
			.Run();

		if ( !trace.Hit )
			return;

		var hitObject = trace.Collider?.GameObject ?? trace.GameObject;
		if ( !PhysicalFastestTapperGame.TryGetStationIndexFromButtonObject( hitObject, out var stationIndex ) || stationIndex != StationIndex )
			return;

		if ( Game is null || !Game.CanInteractWithStation( StationIndex ) )
			return;

		Game?.PressPhysicalButton( StationIndex );
	}

	private void FindGameController()
	{
		Game = Scene.Directory.FindByName( "Physical Fastest Tapper Game" )
			.FirstOrDefault()
			?.GetComponent<PhysicalFastestTapperGame>();
	}
}
