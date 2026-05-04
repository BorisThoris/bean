using Sandbox;

public sealed class FastestClickerStartup : GameObjectSystem<FastestClickerStartup>, ISceneStartup
{
	public FastestClickerStartup( Scene scene ) : base( scene )
	{
	}

	void ISceneStartup.OnHostInitialize()
	{
		if ( Scene.Get<PhysicalFastestTapperGame>().IsValid() )
			return;

		var load = new SceneLoadOptions();
		load.IsAdditive = true;
		load.SetScene( "scenes/minimal.scene" );
		Scene.Load( load );
	}
}
