using Sandbox;

namespace Foliage;

public class FoliageObjectSettings
{
	[Property]
	[Order( 1 )]
	[Range( 0.0f, 10.0f, false ), Step( 0.1f )]
	[Description( @"
	How many to paint per stroke, relative to the strokes quantity.<br /><br />
	Example: If we're painting 10 foliage objects per stroke, and this is set to 2, we'll paint 20 foliage objects per stroke.<br /><br />
	The random value is calculated per stroke." )]
	[DefaultValue( 1.0f )]
	public RangedFloat CountMultiplier { get; set; } = new RangedFloat( 1.0f );

	[Property]
	[Order( 1 )]
	[Description( @"
	Maximum count per stroke, a value less than 0 means no maximum.<br /><br />
	The random value is calculated per stroke." )]
	[DefaultValue( 0.0f )]
	public RangedFloat MaxPerStroke { get; set; } = new RangedFloat( 0.0f );

	[Property]
	[Order( 1 )]
	[Range( 0.0f, 2, false ), Step( 0.1f )]
	[Description( @"
	How much to scale the foliage by, equal in all directions<br /><br />
	The random value is calculated per foliage object.
	" )]
	[DefaultValue( 1.0f )]
	public RangedFloat Scale { get; set; } = new RangedFloat( 0.8f, 1.2f );

	[Property]
	[Order( 1 )]
	[Description( @"
	Tint color applied to the foliage's <span style=""color: #4EC9B0; font-weight: bold;"">ModelRenderer</span>.<br /><br />
	The random value is calculated per foliage object.
	" )]
	public Gradient Tint { get; set; } = new Gradient();
}
