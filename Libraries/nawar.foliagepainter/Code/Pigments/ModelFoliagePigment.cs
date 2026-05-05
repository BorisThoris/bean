
using System;
using Sandbox;

namespace Foliage;

/// <summary>
/// A pigment that is just a model.
/// It will create a new game object with a model renderer component.
/// </summary>
public class ModelFoliagePigment : FoliagePigment
{
	[Property]
	[Order( 0 )]
	[ResourceType( "model" )]
	public Model? Model { get; set; }

	public override GameObject? Paint( Scene scene )
	{
		if ( Model is null ) return null;
		var foliageObject = scene.CreateObject( false );
		foliageObject.Name = $"Foliage-{Model.ResourceName}";
		var modelRenderer = foliageObject.GetOrAddComponent<ModelRenderer>();
		modelRenderer.Model = Model;
		return foliageObject;
	}

	public override float Radius()
	{
		if ( Model is null ) return 0f;
		var bounds = Model.Bounds;
		return MathF.Max( bounds.Size.x, bounds.Size.y ) / 2f;
	}

}

