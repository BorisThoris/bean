using Editor;
using Sandbox;

namespace Foliage;

public partial class FoliagePainter
{
	protected void CreateOverlayWidgets()
	{
		AddOverlay( new FoliagePainterWidget( SceneOverlay, this ), TextFlag.RightBottom, 10 );
	}

}

public class FoliagePainterWidget : WidgetWindow
{
	private FoliagePainter Painter { get; set; }

	public FoliagePainterWidget( Widget parent, FoliagePainter painter ) : base( parent, "Foliage Painter" )
	{

		Layout = Layout.Column();
		Layout.Margin = 8;

		var openPainterSettingsButton = new Button( "Open Painter Settings" );
		openPainterSettingsButton.Clicked += () =>
		{
			EditorUtility.InspectorObject = painter;
		};
		Layout.Add( openPainterSettingsButton );
	}
}
