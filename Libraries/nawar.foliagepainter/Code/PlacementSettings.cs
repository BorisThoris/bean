using Sandbox;

namespace Foliage;

public class FoliagePlacementSettings
{
	[Property]
	[Range( 0.0f, 1.0f, true ), Step( 0.05f )]
	[Description( @"
	Maximum surface normal <strong>[0,1]</strong> allowed to paint on when compared to the <strong>Up Vector</strong>.<br /><br />
	<strong>0</strong> = Only completely flat surfaces are allowed.<br />
	<strong>1</strong> = Any surface is allowed.
	" )]
	public float MaxNormal { get; set; } = 1;

	[Property]
	[Description( @"
	Whether to align the foliage to the surface normal.<br /><br />
	If disabled, the foliage will be aligned to the world up vector.
	" )]
	public bool AlignToNormal { get; set; } = true;


	[Property]
	[HideIf( nameof( AlignToNormal ), true )]
	[Range( -1.0f, 1.0f, true ), Step( 0.05f )]
	[Description( @"
	Range of pitch <strong>[-1, 1]</strong> to randomize the foliage to, when not aligning to the surface normal.<br /><br />

	<strong>0</strong> = Foliage aligned to the surface normal.<br />
	<strong>1</strong> = Foliage aligned to the world up vector.<br />
	<strong>-1</strong> = Foliage aligned to the world sideways vector.<br />
	<br/>
	Calculated per foliage object.
	" )]
	public RangedFloat PitchAlignment { get; set; } = new RangedFloat( 0f, 1f );

	[Description( @"
	Whether to randomize the rotation (yaw) of the foliage.<br /><br />
	If this is false then all the foliage will be facing the same direction.
	" )]
	[Property]
	public bool RandomYaw { get; set; } = true;

	[Description( @"
	Amount to embed the foliage into the surface via the normal of the surface.<br /><br />
	<strong>0</strong> = The foliage will be placed on the surface.<br />
	<strong>n</strong> = The foliage will be placed <strong>n</strong> units into the surface.<br />
	<br/>
	Calculated per foliage object.<br/>
	<br />
	<i>Essentially a z-offset if we were placing on a flat surface.</i>
	" )]
	[Property]
	public RangedFloat EmbedAmount { get; set; } = new RangedFloat( 0f );

	[Description( @"
	Minimum spacing between foliage objects in the same stroke.<br /><br />
	When painting, objects will not be placed within this distance of other objects painted in the current stroke.<br />
	<strong>0</strong> = No spacing restriction.<br />
	<br/>
	Calculated per foliage object.
	" )]
	[Property]
	[Range( 0.0f, 1000.0f, false ), Step( 0.1f )]
	public RangedFloat SpacingAmount { get; set; } = new RangedFloat( 0f );

	[Description( @"
	Whether to include the bounds (radius) of foliage objects when calculating spacing.<br /><br />
	If enabled, the spacing calculation will include the radius of both the new object and existing objects, ensuring they don't overlap.<br />
	If disabled, spacing is calculated from center to center.
	" )]
	[Property]
	public bool IncludeBoundsInSpacing { get; set; } = false;
}
