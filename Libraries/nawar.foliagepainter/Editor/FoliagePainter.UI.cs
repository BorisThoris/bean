using System.Linq;
using Editor;
using Editor.RectEditor;
using Editor.TerrainEditor;
using Sandbox;

namespace Foliage;

[Inspector( typeof( FoliagePainter ) )]
public class FoliagePainterInspector : InspectorWidget
{
	public FoliagePainterInspector( SerializedObject so ) : base( so )
	{
		Log.Info( "FoliagePainterInspector" );
		if ( so.Targets.FirstOrDefault() is not FoliagePainter painter )
			return;

		Log.Info( "FoliagePainterInspector - adding inspector UI" );

		Layout = Layout.Column();
		Layout.Add( painter.BuildUI() );
	}
}


public partial class FoliagePainterSettingsWidget : Widget
{

	private FoliagePainter Painter { get; set; }
	private static FoliagePainterSettings Settings => FoliagePainter.Settings;
	private SerializedObject so { get; set; }

	public FoliagePainterSettingsWidget( FoliagePainter painter, SerializedObject _so )
	{
		Painter = painter;
		so = _so;

		Layout = Layout.Column();

		so.OnPropertyChanged += ( p ) =>
		{
			if ( p.Name == nameof( Settings.KeepStrokingAfterFinish ) || p.Name == nameof( Settings.BrushMode ) || p.Name == nameof( Settings.RotateBrushOnPaint ) )
			{
				Rebuild();
			}
		};

		Rebuild();
	}

	void Rebuild()
	{
		Layout.Clear( true );
		var Settings = FoliagePainter.Settings;

		Layout.Spacing = 0;
		Layout.Margin = 8;

		var controlSheet = new ControlSheet();
		controlSheet.SetMinimumColumnWidth( 0, 20 );
		controlSheet.AddRow( so.GetProperty( nameof( Settings.ContainerObject ) ) );
		controlSheet.AddRow( so.GetProperty( nameof( Settings.Size ) ) );
		controlSheet.AddRow( so.GetProperty( nameof( Settings.ObjectsPaintedPerSecond ) ) );
		controlSheet.AddRow( so.GetProperty( nameof( Settings.EraseSpeed ) ) );
		controlSheet.AddRow( so.GetProperty( nameof( Settings.ObjectsPaintedPerStroke ) ) );
		controlSheet.AddRow( so.GetProperty( nameof( Settings.Palette ) ) );
		controlSheet.AddRow( so.GetProperty( nameof( Settings.EraseOnlyPalette ) ) );
		controlSheet.AddRow( so.GetProperty( nameof( Settings.EraseSearchRadius ) ) );
		controlSheet.AddRow( so.GetProperty( nameof( Settings.IncludeExistingFoliageForSpacing ) ) );
		controlSheet.AddRow( so.GetProperty( nameof( Settings.KeepStrokingAfterFinish ) ) );
		if ( Settings.KeepStrokingAfterFinish )
		{
			controlSheet.AddRow( so.GetProperty( nameof( Settings.StrokeDelay ) ) );
		}
		// broken right now, need to fix, but also wasn't all that useful in the first place
		//controlSheet.AddRow( so.GetProperty( nameof( Settings.BrushMode ) ) );
		if ( Settings.BrushMode == BrushMode.Texture )
		{
			controlSheet.AddRow( so.GetProperty( nameof( Settings.CurrentBrushRotation ) ) );
			controlSheet.AddRow( so.GetProperty( nameof( Settings.RotateBrushOnPaint ) ) );
			if ( Settings.RotateBrushOnPaint )
			{
				controlSheet.AddRow( so.GetProperty( nameof( Settings.RotationAmount ) ) );
				controlSheet.AddRow( so.GetProperty( nameof( Settings.RotationAmountVariance ) ) );
			}

		}
		Layout.Add( controlSheet );

		Layout.AddSpacingCell( 8 );

		if ( Settings.BrushMode == BrushMode.Texture )
		{

			var brushList = new BrushGlue.BrushListWidget();
			var brushListSection = Layout.AddRow();
			brushListSection.Add( brushList );
			Layout.AddSpacingCell( 8 );
		}


		var instructions = Layout.AddColumn();
		instructions.Margin = 10;
		instructions.Add( new Label.Body( "LMB = Paint" ) );
		instructions.Add( new Label.Body( "Shift + LMB = Erase" ) );
		instructions.Add( new Label.Body( "Alt + Scroll = Change Brush Size" ) );

		Layout.AddStretchCell( 1 );

		DoLayout();
	}


}

public partial class FoliagePainter
{
	private Widget RebuildUI( SerializedObject so )
	{
		var root = new Widget( null )
		{
			Layout = Layout.Column()
		};



		return root;
	}

	public Widget BuildUI()
	{
		// var so = EditorUtility.GetSerializedObject( Settings );
		// return RebuildUI( so );
		return new FoliagePainterSettingsWidget( this, EditorUtility.GetSerializedObject( Settings ) );
	}
}

