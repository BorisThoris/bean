using System;
using System.Collections.Generic;
using System.Linq;
using Editor;
using Sandbox;

namespace Foliage;

[EditorTool( "tools.foilage-painter" )]
[Title( "Foliage Painter" )] // title of your tool
[Category( "Tools" )]
[Icon( "grass" )]
[Group( "Tools" )]
public partial class FoliagePainter : EditorTool
{

	public static BrushGlue.BrushList BrushList { get; set; } = new();

	[Shortcut( "tools.foilage-painter", "Shift+F", typeof( SceneViewportWidget ) )]
	public static void ActivateTool()
	{
		EditorToolManager.SetTool( nameof( FoliagePainter ) );
	}

	private static FoliagePainterSettings? LastUsedPainterSettings { get; set; }

	public static FoliagePainterSettings Settings { get; private set; } = new();

	/// <summary>
	/// Whether the user is currently painting.
	/// </summary>
	private bool IsPainting { get; set; } = false;

	/// <summary>
	/// Whether the user is currently erasing.
	/// </summary>
	private bool IsErasing { get; set; } = false;

	private bool IsStroking { get; set; } = false;

	private TimeSince TimeSinceFinishedStroking { get; set; } = 0f;

	/// <summary>
	/// Whether the user has painted at least one foliage object this painting session.
	/// </summary>
	private bool HasPainted { get; set; } = false;

	private float PaintProgress { get; set; } = 0f;

	private float EraseProgress { get; set; } = 0f;

	public Stroke CurrentStroke { get; set; } = new();

	protected List<GameObject> FoliagePainted = [];

	protected IDisposable? UndoScope;

	public FoliagePainter()
	{
		Log.Info( "FoliagePainter - constructor" );

		if ( LastUsedPainterSettings is not null )
		{
			LastUsedPainterSettings.CopyTo( Settings );
			LastUsedPainterSettings = null;
		}
	}

	public override void OnEnabled()
	{
		base.OnEnabled();
		Settings.ContainerObject = GetPaintTargetFromSelected();

		AllowGameObjectSelection = false;
		Selection.Clear();
		Selection.Add( this );

		if ( LastUsedPainterSettings is not null )
		{
			LastUsedPainterSettings.CopyTo( Settings );
			LastUsedPainterSettings = null;
		}

		CreateOverlayWidgets();
	}

	public override void OnSelectionChanged()
	{
		base.OnSelectionChanged();
	}

	public override void OnDisabled()
	{
		base.OnDisabled();
		EditorUtility.InspectorObject = null;

		Log.Info( "OnDisabled" );

		if ( _previewObject is not null )
		{
			_previewObject.Delete();
			_previewObject = null;
		}

		LastUsedPainterSettings = Settings;
	}

	public override void Dispose()
	{
		base.Dispose();
	}


	private SceneTraceResult PaintTrace( Vector3 origin, Vector3 direction, float distance )
	{
		return Scene.Trace.Ray( new Ray( origin, direction ), distance )
			.UseRenderMeshes( true )
			.UsePhysicsWorld( true )
			.WithoutTags( "invisible", "foliage" )
			.Run();
	}

	private GameObject? GetPaintTargetFromSelected()
	{
		return Selection.OfType<GameObject>().FirstOrDefault();
	}

	private BrushGlue.Brush? _lastBrush = null;

	public override void OnUpdate()
	{
		Settings.Brush = BrushList.Selected;
		if ( _lastBrush != Settings.Brush )
		{
			BuildBrushAliasTable();
			_lastBrush = Settings.Brush;
		}

		// Hack because undo will revert this for some reason
		LastUsedPainterSettings = Settings;


		var paintTarget = Settings.ContainerObject;
		if ( paintTarget is null ) { return; }

		var paintTrace = PaintTrace( Gizmo.CurrentRay.Position, Gizmo.CurrentRay.Forward, 50000 );
		if ( !paintTrace.Hit )
		{
			return;
		}

		DrawBrushPreview( paintTrace );

		var holdingShift = Editor.Application.KeyboardModifiers.HasFlag( Sandbox.KeyboardModifiers.Shift );
		var holdingAlt = Editor.Application.KeyboardModifiers.HasFlag( Sandbox.KeyboardModifiers.Alt );

		// When holding alt the camera doesn't move but the X does >:3
		var scrollDelta = Editor.Application.MouseWheelDelta.x;
		var isScrolling = scrollDelta != 0;

		if ( holdingAlt && isScrolling )
		{
			Settings.Size += (int)(scrollDelta * 100);
			Settings.Size = Settings.Size.Clamp( 32, 2048 );
		}



		if ( Settings.Palette is null ) return;


		if ( !IsPainting && !IsErasing && Gizmo.IsLeftMouseDown && !holdingShift )
		{
			StartPainting();
		}

		if ( !IsPainting && !IsErasing && Gizmo.IsLeftMouseDown && holdingShift )
		{
			StartErasing();
		}

		if ( IsPainting && !IsErasing )
		{
			if ( !Gizmo.IsLeftMouseDown || holdingShift )
			{
				FinishPainting();
				return;
			}

			if ( IsStroking )
			{
				StrokeUpdate( paintTrace );
			}
			else if ( Settings.KeepStrokingAfterFinish && TimeSinceFinishedStroking >= Settings.StrokeDelay )
			{
				StartStroke();
			}
		}

		if ( IsErasing && !IsPainting )
		{
			if ( !Gizmo.IsLeftMouseDown || !holdingShift )
			{
				FinishErasing();
				return;
			}

			EraseUpdate( paintTrace );
		}

	}


	private void StartPainting()
	{
		//_randomBrushPositions.Clear();
		Log.Info( "StartPainting" );

		if ( IsPainting )
		{
			Log.Error( "Already painting" );
			return;
		}

		if ( Settings.Palette is null )
		{
			Log.Error( "No brush selected" );
			return;
		}

		IsPainting = true;
		HasPainted = false;

		StartStroke();
	}

	private void StartStroke()
	{
		Log.Info( "StartStroke" );

		if ( Settings.Palette is null )
		{
			Log.Error( "No brush selected" );
			return;
		}

		Settings.Palette.CalculatePigmentRadii();

		UndoScope?.Dispose();
		UndoScope = SceneEditorSession.Active.UndoScope( "Paint Foliage" ).WithGameObjectChanges( Settings.ContainerObject, GameObjectUndoFlags.Children ).Push();

		CurrentStroke = new Stroke( Settings );

		IsStroking = true;
	}

	private void StrokeUpdate( SceneTraceResult paintTrace )
	{
		PaintProgress += Time.Delta * Settings.ObjectsPaintedPerSecond;
		while ( PaintProgress >= 1 )
		{
			bool painted = false;
			for ( int attempt = 0; attempt < 10; attempt++ )
			{
				if ( Paint( paintTrace ) )
				{
					painted = true;
					break;
				}
			}

			if ( !painted )
			{
				Log.Warning( "Failed to paint foliage object after 10 attempts" );
			}

			PaintProgress -= 1;
		}

		if ( CurrentStroke.HasFinished )
			FinishStroke();
	}

	private void StartErasing()
	{
		Log.Info( "StartErasing" );

		if ( IsErasing )
		{
			Log.Error( "Already erasing" );
			return;
		}

		if ( Settings.ContainerObject is null )
		{
			Log.Error( "No container object selected" );
			return;
		}

		IsErasing = true;
		EraseProgress = 0f;

		UndoScope?.Dispose();
		UndoScope = SceneEditorSession.Active.UndoScope( "Erase Foliage" ).WithGameObjectChanges( Settings.ContainerObject, GameObjectUndoFlags.Children ).Push();
	}

	private void EraseUpdate( SceneTraceResult paintTrace )
	{
		EraseProgress += Time.Delta * Settings.EraseSpeed;
		while ( EraseProgress >= 1 )
		{
			Erase( paintTrace );
			EraseProgress -= 1;
		}
	}

	private void FinishErasing()
	{
		Log.Info( "Finishing erasing" );
		IsErasing = false;
		EraseProgress = 0f;

		UndoScope?.Dispose();
		UndoScope = null;
	}

	private void FinishPainting()
	{
		FinishStroke();

		Log.Info( "Finishing painting" );
		IsPainting = false;
	}

	private void FinishStroke()
	{
		Log.Info( "Finishing stroke" );
		IsStroking = false;
		UndoScope?.Dispose();
		UndoScope = null;

		TimeSinceFinishedStroking = 0f;
	}

	private static Vector3 GetPitchAlignedUp( Vector3 surfaceNormal, float alignment )
	{
		var normal = surfaceNormal.Normal;
		var worldUp = Vector3.Up;

		if ( alignment <= -1f )
			alignment = -1f;
		else if ( alignment >= 1f )
			alignment = 1f;
		else if ( MathF.Abs( alignment ) <= 0.001f )
			return normal;

		var sideways = normal - (normal.Dot( worldUp ) * worldUp);
		if ( sideways.Length <= 0.001f )
		{
			// Surface is essentially vertical; arbitrarily choose a sideways direction
			sideways = Vector3.Right;
		}
		sideways = sideways.Normal;

		Vector3 targetUp;

		if ( alignment > 0f )
		{
			targetUp = Vector3.Lerp( normal, worldUp, alignment );
		}
		else
		{
			targetUp = Vector3.Lerp( normal, sideways, -alignment );
		}

		if ( targetUp.Length <= 0.001f )
			return normal;

		return targetUp.Normal;
	}

}
