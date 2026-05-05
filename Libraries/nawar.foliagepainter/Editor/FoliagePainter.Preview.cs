using Sandbox;

namespace Foliage;

public partial class FoliagePainter
{

	Foliage.BrushGlue.BrushPreviewSceneObject? _previewObject;

	protected void DrawBrushPreview( SceneTraceResult paintTrace )
	{
		if ( Settings.BrushMode != BrushMode.Texture )
		{
			if ( _previewObject is not null )
			{
				_previewObject.Delete();
				_previewObject = null;
			}
		}

		// Chosen position debug
		using ( Gizmo.Scope( "cursor" ) )
		{
			Gizmo.Draw.Color = Color.Red;
			foreach ( var position in _randomBrushPositions )
			{
				Gizmo.Transform = new Transform( position, Rotation.LookAt( Vector3.Up, Vector3.Forward ) );
				Gizmo.Draw.LineCircle( 0, 32, 0, 360, 12 );
			}
		}


		if ( Settings.BrushMode == BrushMode.Circle )
		{
			// Draw circle only in circle mode
			using ( Gizmo.Scope( "cursor" ) )
			{
				Gizmo.Transform = new Transform( paintTrace.HitPosition, Rotation.LookAt( paintTrace.Normal ) );
				var numSegments = Settings.Size / 32;
				numSegments = numSegments.Clamp( 32, 128 );
				Gizmo.Draw.LineCircle( Vector3.Forward, Settings.Size, 0, 360, numSegments );

				var totalHeight = 150f;
				var numSegmentsHeight = 10;
				var segmentHeight = totalHeight / numSegmentsHeight;
				for ( int i = 0; i < numSegmentsHeight; i++ )
				{
					var opacity = 0.5f - float.Lerp( 0f, 0.4f, i / (float)numSegmentsHeight );
					Gizmo.Draw.Color = new Color( 1, 1, 1, opacity );
					Gizmo.Draw.LineCircle( Vector3.Forward * i * segmentHeight, Settings.Size, 0, 360, numSegments );
				}

			}
			return;
		}

		// Texture mode - show preview object
		if ( Settings.Brush is null ) return;


		_previewObject ??= new Foliage.BrushGlue.BrushPreviewSceneObject( Gizmo.World );

		var color = Color.FromBytes( 150, 150, 250 );

		if ( Editor.Application.KeyboardModifiers.HasFlag( Sandbox.KeyboardModifiers.Shift ) )
			color = color.AdjustHue( 90 );

		color.a = 0.1f;

		// Calculate rotation around surface normal
		var baseRotation = Rotation.LookAt( Vector3.Left );
		var rotatedTransform = baseRotation.RotateAroundAxis( Vector3.Up, Settings.CurrentBrushRotation );

		Log.Info( $"Rotated transform: {rotatedTransform.Pitch()}, {rotatedTransform.Yaw()}, {rotatedTransform.Roll()}" );

		_previewObject.RenderLayer = SceneRenderLayer.OverlayWithDepth;
		_previewObject.Bounds = BBox.FromPositionAndSize( 0, float.MaxValue );
		_previewObject.Transform = new Transform( paintTrace.HitPosition, rotatedTransform );
		_previewObject.Radius = Settings.Size;
		_previewObject.Texture = Settings.Brush.Texture;
		_previewObject.Color = color;
	}
}
