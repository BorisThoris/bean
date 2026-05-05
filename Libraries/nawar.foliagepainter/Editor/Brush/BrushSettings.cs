using Editor;
using Sandbox;

namespace Foliage.BrushGlue;

public class BrushSettings
{
	[Property, Range( 8, 1024 ), Step( 1 )] public int Size { get; set; } = 200;
	[Property, Range( 0.0f, 1.0f ), Step( 0.01f )] public float Opacity { get; set; } = 0.5f;
}

public class BrushSettingsWidgetWindow : WidgetWindow
{
	class BrushSelectedWidget : Widget
	{
		public FoliagePainter FoliagePainter { get; protected set; }

		public BrushSelectedWidget( Widget parent, FoliagePainter foliagePainter ) : base( parent )
		{
			MinimumSize = new( 48, 48 );
			Cursor = CursorShape.Finger;
			FoliagePainter = foliagePainter;
		}

		protected override void OnPaint()
		{
			base.OnPaint();

			Paint.Antialiasing = true;

			Paint.ClearPen();
			Paint.DrawRect( LocalRect );

			var pixmap = FoliagePainter.Settings.Brush!.Pixmap;
			Paint.Draw( LocalRect.Contain( pixmap.Size ), pixmap );
		}

		protected override void OnMouseClick( MouseEvent e )
		{
			var popup = new PopupWidget( null );
			popup.Position = Editor.Application.CursorPosition;
			popup.Visible = true;
			popup.Layout = Layout.Column();
			popup.Layout.Margin = 10;
			popup.MaximumSize = new Vector2( 300, 150 );

			var list = new BrushListWidget();
			list.BrushSelected += () => { popup.Close(); Update(); };
			popup.Layout.Add( list );
		}
	}

	public BrushSettingsWidgetWindow( Widget parent, SerializedObject so, FoliagePainter foliagePainter ) : base( parent, "Brush Settings" )
	{
		Layout = Layout.Row();
		Layout.Margin = 8;
		MaximumWidth = 400.0f;

		var cs = new ControlSheet();
		cs.AddObject( so );
		Layout.Add( new BrushSelectedWidget( this, foliagePainter ) );

		var l = Layout.Row();
		l.Add( cs );

		Layout.Add( l );
	}
}
