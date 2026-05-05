using Sandbox;

namespace Foliage;

public abstract class FoliagePigment : IFoliagePigment
{
	private const int _placementCategoryOrder = 10;

	[Property, InlineEditor( Label = false )]
	[Group( "Object Settings" )]
	[Order( 1 )]
	public FoliageObjectSettings Settings { get; set; } = new();

	[Property, InlineEditor( Label = false )]
	[Group( "Placement" )]
	[Order( _placementCategoryOrder + 0 )]
	public FoliagePlacementSettings Placement { get; set; } = new();

	public virtual void OnLoad()
	{

	}

	public virtual float Radius()
	{
		return 0f;
	}


	public virtual GameObject? Paint( Scene scene )
	{
		return null;
	}
}
