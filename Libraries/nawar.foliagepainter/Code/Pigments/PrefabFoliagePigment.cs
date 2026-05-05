using System;
using Sandbox;

namespace Foliage;

/// <summary>
/// A pigment that is a prefab.
/// It will create a clone of the prefab.
/// </summary>
public class PrefabFoliagePigment : FoliagePigment
{
	[Property]
	[ResourceType( "prefab" )]
	public PrefabFile? Prefab { get; set; }

	public float _CalculatedRadius = 0f;

	public override void OnLoad()
	{
		base.OnLoad();

		if ( Prefab is null ) return;

		var tempObject = GameObject.GetPrefab( Prefab.ResourcePath );
		if ( tempObject is null ) return;
		var bounds = tempObject.GetBounds();
		_CalculatedRadius = MathF.Max( bounds.Size.x, bounds.Size.y ) / 2f;
	}

	public override float Radius()
	{
		return _CalculatedRadius;
	}

	public override GameObject? Paint( Scene scene )
	{
		if ( Prefab is null ) return null;
		return GameObject.Clone( Prefab );
	}
}
