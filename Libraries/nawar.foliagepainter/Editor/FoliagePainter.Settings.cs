using System.Text.Json.Serialization;
using Sandbox;

namespace Foliage;

public enum BrushMode
{
	Circle,
	Texture
}

/// <summary>
/// Settings for the foliage painter tool.
/// You might even call this a paint brush.
/// </summary>
// [JsonSerializable( typeof( FoliagePainterSettings ) )]
public partial class FoliagePainterSettings
{

	public FoliagePainterSettings()
	{
		Log.Info( "FoliagePainterSettings - constructor" );
	}

	[Property]
	[Description( "The brush distribution mode. Circle uses uniform circular distribution, Texture samples from the brush texture based on grayscale values." )]
	public BrushMode BrushMode { get; set; } = BrushMode.Circle;

	[Property]
	[Description( "The object that the foliage will be parented to." )]
	public GameObject? ContainerObject { get; set; }

	[Property]
	[Range( 32, 2048 ), Step( 32 )]
	[Description( "The radius of the paint brush." )]
	public int Size { get; set; } = 50;

	[Property( Title = "Paint Speed" )]
	[Range( 1, 100 ), Step( 1 )]
	[Description( "How many objects to paint per second." )]
	public float ObjectsPaintedPerSecond { get; set; } = 5;

	[Property( Title = "Erase Speed" )]
	[Range( 1, 100 ), Step( 1 )]
	[Description( "How many objects to erase per second." )]
	public float EraseSpeed { get; set; } = 5;

	[Property, ResourceType( ".folb" )]
	public FoliagePalette? Palette { get; set; }

	public BrushGlue.Brush? Brush { get; set; }

	[Property]
	public bool EraseOnlyPalette { get; set; } = true;

	[Property( Title = "Objects Per Stroke" )]
	[Range( 1, 100 ), Step( 0.5f )]
	[Description( @"
	How many of each type of foliage object to paint per stroke.<br /><br />
	<i>Example:</i> If you have a multiplier of 2.0 for a foliage object, and this is set to 5, you will paint 10 of that foliage object per stroke.
	" )]
	public float ObjectsPaintedPerStroke { get; set; } = 5;

	[Property]
	[Title( "Stoke After Finish" )]
	[Description( @"
	If enabled, a new stroke will be started after the current stroke is finished." )]
	public bool KeepStrokingAfterFinish { get; set; } = true;

	[Property]
	[Range( 0f, 1f ), Step( 0.05f )]
	[Description( @"
	How long to wait after the current stroke is finished before starting a new stroke." )]
	public float StrokeDelay { get; set; } = 0.1f;

	[Property]
	[Title( "Existing Affects Spacing" )]
	[Description( @"
	Whether to include existing foliage objects when calculating spacing.<br /><br />
	If enabled, spacing calculations will consider existing foliage objects with FoliageInfo components, preventing overlap with previously painted foliage." )]
	public bool IncludeExistingFoliageForSpacing { get; set; } = false;

	[Property]
	[Range( 1, 100 ), Step( 1 )]
	[Description( @"
	Radius to search for foliage objects when erasing.<br /><br />
	When erasing, the tool will search for objects within this distance of the randomly chosen erase position." )]
	public float EraseSearchRadius { get; set; } = 50f;

	[Property]
	[Description( @"
	If enabled, the texture brush will be rotated by a random amount each time you paint or erase.<br /><br />
	Only applies when Brush Mode is set to Texture." )]
	public bool RotateBrushOnPaint { get; set; } = false;

	[Property]
	[Range( 0.0f, 360.0f, false ), Step( 1.0f )]
	[Description( @"
	The base rotation amount for the texture brush when RotateBrush is enabled.<br /><br />
	The brush will be rotated by this amount plus/minus the RotationAmountVariance each time you paint or erase.<br />
	Only applies when Brush Mode is set to Texture and RotateBrush is enabled." )]
	public float RotationAmount { get; set; } = 0f;

	[Property]
	[Range( 0.0f, 360.0f, false ), Step( 1.0f )]
	[Description( @"
	The variance to add/subtract from RotationAmount when RotateBrush is enabled.<br /><br />
	The final rotation will be: RotationAmount ± RotationAmountVariance (randomly chosen).<br />
	Only applies when Brush Mode is set to Texture and RotateBrush is enabled." )]
	public float RotationAmountVariance { get; set; } = 0f;

	[Property]
	[Range( 0.0f, 360.0f, false ), Step( 1.0f )]
	[Description( @"
	The current rotation of the texture brush.<br /><br />
	This value is automatically updated when RotateBrush is enabled and you paint or erase.<br />
	Only applies when Brush Mode is set to Texture." )]
	public float CurrentBrushRotation { get; set; } = 0f;

	public void CopyTo( FoliagePainterSettings target )
	{
		target.Size = Size;
		target.ObjectsPaintedPerSecond = ObjectsPaintedPerSecond;
		target.EraseSpeed = EraseSpeed;
		target.Palette = Palette;
		target.EraseOnlyPalette = EraseOnlyPalette;
		target.ObjectsPaintedPerStroke = ObjectsPaintedPerStroke;
		target.ContainerObject = ContainerObject;
		target.StrokeDelay = StrokeDelay;
		target.BrushMode = BrushMode;
		target.IncludeExistingFoliageForSpacing = IncludeExistingFoliageForSpacing;
		target.EraseSearchRadius = EraseSearchRadius;
		target.RotateBrushOnPaint = RotateBrushOnPaint;
		target.RotationAmount = RotationAmount;
		target.RotationAmountVariance = RotationAmountVariance;
		target.CurrentBrushRotation = CurrentBrushRotation;
	}
}
