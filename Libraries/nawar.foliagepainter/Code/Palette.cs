using System;
using System.Collections.Generic;
using Sandbox;

namespace Foliage;

[AssetType( Name = "Foliage Palette", Extension = "folp", Category = "Foliage" )]
[Description( "A foliage palette, describes what foliage to paint and how to paint it" )]
public class FoliagePalette : GameResource
{
	public enum FoliageMode
	{
		Prefab,
		Model
	}
	public static HashSet<FoliagePalette> All { get; set; } = new();

	[FeatureEnabled( "Global Placement",
		Icon = "place",
		Tint = EditorTint.Yellow,
		Description = @"
	Global placement settings for all foliage objects.<br /><br />
	If enabled, the unique placement settings for each foliage object will be ignored.
	" )]
	[Order( 1 )]
	public bool UseGlobalPlacement { get; set; } = false;

	[Feature( "Global Placement" )]
	[Property, InlineEditor( Label = false )]
	public FoliagePlacementSettings GlobalPlacementOverride { get; set; } = new();

	[FeatureEnabled( "Global Settings",
		Icon = "satellite",
		Tint = EditorTint.Yellow,
		Description = @"
	Global foliage object settings for all foliage objects.<br /><br />
	If enabled, the unique object settings for each foliage object will be ignored.
	" )]
	[Order( 2 )]
	public bool UseGlobalObjectSettings { get; set; } = false;

	[Feature( "Global Settings" )]
	[Property, InlineEditor( Label = false )]
	public FoliageObjectSettings GlobalObjectOverride { get; set; } = new();

	[Feature( "Foliage Objects", Icon = "grass" )]
	[Property]
	[Order( -10 )]
	[Description( @"
	Whether to use Prefab <br /><br />
	<strong><u>Prefab Mode:</u></strong><br />
	Foliage objects will be created from the prefab you specify.<br />
	Tint will be applied to all <span style=""color: #4EC9B0; font-weight: bold;"">ModelRenderer</span> components that are a child of the prefab.<br /><br />
	<strong><u>Model Mode:</u></strong><br />
	Foliage objects will be a game object with a <span style=""color: #4EC9B0; font-weight: bold;"">ModelRenderer</span> component.<br />
	" )]
	public FoliageMode Mode { get; set; } = FoliageMode.Prefab;

	[Feature( "Foliage Objects" )]
	[Property, InlineEditor, WideMode]
	[ShowIf( nameof( Mode ), FoliageMode.Prefab )]
	public List<PrefabFoliagePigment> FoliagePrefabs { get; set; } = new();

	[Feature( "Foliage Objects" )]
	[Property, InlineEditor, WideMode]
	[ShowIf( nameof( Mode ), FoliageMode.Model )]
	public List<ModelFoliagePigment> FoliageModels { get; set; } = new();



	protected override void PostLoad()
	{
		CalculatePigmentRadii();
		All.Add( this );
	}

	protected override void PostReload()
	{
		Log.Info( "Reloading foliage palette" );
		CalculatePigmentRadii();
		base.PostReload();
	}

	public void CalculatePigmentRadii()
	{
		var pigments = new List<IFoliagePigment>();
		if ( Mode == FoliageMode.Prefab )
		{
			pigments.AddRange( FoliagePrefabs );
		}
		else if ( Mode == FoliageMode.Model )
		{
			pigments.AddRange( FoliageModels );
		}
		foreach ( var pigment in pigments )
		{
			pigment.OnLoad();
		}
	}

	protected override Bitmap CreateAssetTypeIcon( int width, int height )
	{
		return CreateSimpleAssetTypeIcon( "grass", width, height, "#526e2c", "#ccf97a" );
	}


}
