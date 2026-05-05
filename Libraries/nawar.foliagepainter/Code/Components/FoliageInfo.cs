using Sandbox;

namespace Foliage;

public class FoliageInfo : Component
{
	[Property]
	public float Radius { get; set; } = 0f;

	[Property]
	public int PaletteResourceId { get; set; } = 0;

	[Property]
	public int PigmentIndex { get; set; } = 0;

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		if ( !Gizmo.IsSelected && !Gizmo.IsChildSelected ) return;



		Gizmo.Draw.LineCircle( Vector3.Up * 5, Vector3.Up, Radius );
	}
}
