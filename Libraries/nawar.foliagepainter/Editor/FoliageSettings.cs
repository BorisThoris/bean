using System;
using Editor;
using Sandbox;

namespace Foliage;


public class FoliageSettingsWidgetWindow : WidgetWindow
{
	class FoliageSelectedWidget : Widget
	{

		public FoliageSelectedWidget( Widget parent ) : base( parent )
		{
			MinimumSize = new( 48, 48 );
			Cursor = CursorShape.Finger;
		}

		protected override void OnMouseClick( MouseEvent e )
		{
			var popup = new PopupWidget( null );
			popup.Position = Editor.Application.CursorPosition;
			popup.Visible = true;
			popup.Layout = Layout.Column();
			popup.Layout.Margin = 10;
			//popup.MaximumSize = new Vector2( 400, 150 );
		}
	}

	public FoliageSettingsWidgetWindow( Widget parent, SerializedObject so ) : base( parent, "Foliage Settings" )
	{
		Layout = Layout.Row();
		Layout.Margin = 4;
		MaximumWidth = 300.0f;

		var cs = new ControlSheet();
		cs.AddRow( so.GetProperty( nameof( FoliagePainterSettings.Size ) ) );
		cs.AddRow( so.GetProperty( nameof( FoliagePainterSettings.ObjectsPaintedPerSecond ) ) );
		cs.AddRow( so.GetProperty( nameof( FoliagePainterSettings.Palette ) ) );
		cs.AddRow( so.GetProperty( nameof( FoliagePainterSettings.EraseOnlyPalette ) ) );
		cs.SetMinimumColumnWidth( 0, 20 );
		cs.Margin = new Sandbox.UI.Margin( 8, 0, 4, 0 );

		var text = Layout.Column();
		text.Add( new Label.Body( "LMB = paint" ) );
		text.Add( new Label.Body( "shift+LMB = erase" ) );
		text.Alignment = TextFlag.LeftBottom;
		text.Margin = new Sandbox.UI.Margin( 16, 6, 4, 0 );

		var l = Layout.Column();
		l.Add( cs );
		l.Add( text );
		Layout.Add( l );

	}
}
