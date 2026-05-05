using Sandbox;

public sealed class FastestClickerStartup : GameObjectSystem<FastestClickerStartup>, ISceneStartup
{
	public FastestClickerStartup( Scene scene ) : base( scene )
	{
	}

	void ISceneStartup.OnHostInitialize()
	{
		Log.Info( "[TapperStartup] mode='direct-scene' scene='test.scene' additiveLoader=False" );
	}
}
