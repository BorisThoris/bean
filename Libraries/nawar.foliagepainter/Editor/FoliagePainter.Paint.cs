using System;
using System.Collections.Generic;
using Sandbox;

namespace Foliage;

public partial class FoliagePainter
{
	private bool Paint( SceneTraceResult paintTrace )
	{
		if ( Settings.ContainerObject is null ) return false;
		if ( CurrentStroke is null ) return false;

		// Update brush rotation if RotateBrush is enabled
		if ( Settings.BrushMode == BrushMode.Texture && Settings.RotateBrushOnPaint )
		{
			var variance = Random.Shared.Float( -Settings.RotationAmountVariance, Settings.RotationAmountVariance );
			Settings.CurrentBrushRotation = Settings.RotationAmount + variance;
		}

		//var originalPaintTrace = paintTrace;
		paintTrace = Settings.BrushMode == BrushMode.Texture
			? GenerateRandomPaintTraceFromBrush( paintTrace )
			: GenerateRandomPaintTrace( paintTrace );

		// Shomehow we missed the ground?
		if ( !paintTrace.Hit )
			return false;

		// Choose which foliage pigment we're painting
		var foliagePigment = CurrentStroke.GetRandomPigment();
		if ( foliagePigment is null ) return false;

		var useGlobalPlacement = CurrentStroke.Palette.UseGlobalPlacement;
		var spacingAmount = useGlobalPlacement
			? CurrentStroke.Palette.GlobalPlacementOverride.SpacingAmount
			: foliagePigment.Placement.SpacingAmount;

		var includeBoundsInSpacing = useGlobalPlacement
			? CurrentStroke.Palette.GlobalPlacementOverride.IncludeBoundsInSpacing
			: foliagePigment.Placement.IncludeBoundsInSpacing;

		// Check if position is too close to existing objects
		if ( CurrentStroke.IsPositionTooClose( foliagePigment, paintTrace.HitPosition, spacingAmount.GetValue(), includeBoundsInSpacing ) )
			return false;

		var foliageTransform = GeneratePigmentTransform( foliagePigment, paintTrace );
		if ( foliageTransform is null ) return false;

		return PaintFoliage( Settings.ContainerObject, CurrentStroke.Palette, foliagePigment, foliageTransform.Value );
	}

	private SceneTraceResult GenerateRandomPaintTrace( SceneTraceResult paintTrace )
	{

		var angle = Random.Shared.Float( 0f, MathF.Tau );
		var radius = MathF.Sqrt( Random.Shared.Float( 0f, 1f ) ) * Settings.Size;

		var a = MathF.Abs( paintTrace.Normal.z ) < 0.999f ? Vector3.Up : Vector3.Left;
		var u = paintTrace.Normal.Cross( a ).Normal;
		var v = paintTrace.Normal.Cross( u ).Normal;

		var randomPosition = paintTrace.HitPosition + radius * (u * MathF.Cos( angle ) + v * MathF.Sin( angle ));

		var offsetAmount = Settings.Size;
		var traceOffset = paintTrace.Normal * offsetAmount;

		var randomizedTrace = PaintTrace( randomPosition + traceOffset, -paintTrace.Normal, offsetAmount * 1.2f );

		return randomizedTrace;
	}
	private List<Vector3> _randomBrushPositions = [];

	private SceneTraceResult GenerateRandomPaintTraceFromBrush( SceneTraceResult paintTrace )
	{
		if ( Settings.Brush is null ) return paintTrace;

		var randomBrushPixel = GetRandomBrushPixel();

		// Rotate UV coordinates around center (0.5, 0.5) if RotateBrush is enabled
		if ( Settings.CurrentBrushRotation != 0f )
		{
			var center = new Vector2( 0.5f, 0.5f );
			var offset = randomBrushPixel - center;
			var angleRad = Settings.CurrentBrushRotation * MathF.PI / 180f;
			var cos = MathF.Cos( angleRad );
			var sin = MathF.Sin( angleRad );
			var rotatedOffset = new Vector2(
				offset.x * cos - offset.y * sin,
				offset.x * sin + offset.y * cos
			);
			randomBrushPixel = center + rotatedOffset;
		}

		var scaledRandomPosition = new Vector3( randomBrushPixel.x, randomBrushPixel.y, 0 ) * Settings.Size * 2 - new Vector3( Settings.Size, Settings.Size, 0 );

		var randomPosition = paintTrace.HitPosition + scaledRandomPosition;

		var offsetAmount = Settings.Size;
		var traceOffset = paintTrace.Normal * offsetAmount;

		var randomizedTrace = PaintTrace( randomPosition + traceOffset, Vector3.Down, offsetAmount * 10 );

		//_randomBrushPositions.Add( randomizedTrace.HitPosition );

		return randomizedTrace;
	}

	private Transform? GeneratePigmentTransform( IFoliagePigment pigment, SceneTraceResult paintTrace )
	{
		var useGlobalPlacement = CurrentStroke.Palette.UseGlobalPlacement;

		// Is our surface too steep?
		var normalUp = 1 - ((paintTrace.Normal.Dot( new Vector3( 0, 0, 1 ) ) + 1f) / 2f);
		var maxNormal = useGlobalPlacement ? CurrentStroke.Palette.GlobalPlacementOverride.MaxNormal : pigment.Placement.MaxNormal;
		if ( normalUp > maxNormal ) return null;

		// Figure out how we're aligned with the surface by calculating a desired up direction
		var desiredUp = Vector3.Up;
		var shouldAlignToNormal = useGlobalPlacement ? CurrentStroke.Palette.GlobalPlacementOverride.AlignToNormal : pigment.Placement.AlignToNormal;
		if ( shouldAlignToNormal )
		{
			var pitchAlignment = useGlobalPlacement ? CurrentStroke.Palette.GlobalPlacementOverride.PitchAlignment : pigment.Placement.PitchAlignment;
			desiredUp = GetPitchAlignedUp( paintTrace.Normal, pitchAlignment.GetValue() );
		}
		desiredUp = desiredUp.Normal;

		// Figure out how much we're spun around our up direction
		var hasRandomYaw = useGlobalPlacement ? CurrentStroke.Palette.GlobalPlacementOverride.RandomYaw : pigment.Placement.RandomYaw;
		var desiredYawRotation = hasRandomYaw ? Random.Shared.Float( 0, 360 ) : 0;

		// Finally calculate our desired rotation
		var desiredRotation = Rotation.LookAt( desiredUp ) * Rotation.FromPitch( 90f );
		desiredRotation = desiredRotation.RotateAroundAxis( desiredUp, desiredYawRotation );

		// How deep into the surface are we going to embed our foliage?
		var embedAmount = useGlobalPlacement ? CurrentStroke.Palette.GlobalPlacementOverride.EmbedAmount : pigment.Placement.EmbedAmount;

		// Calculate our desired transform
		var desiredTransform = new Transform( paintTrace.HitPosition + (paintTrace.Normal * embedAmount.GetValue()), desiredRotation );

		return desiredTransform;
	}

	private bool PaintFoliage( GameObject target, FoliagePalette brush, IFoliagePigment pigment, Transform transform )
	{
		var pigmentIndex = CurrentStroke.GetPigmentIndex( pigment );

		if ( !CurrentStroke.CanPaintPigment( pigmentIndex ) ) return false;

		var foliageObject = pigment.Paint( target.Scene );
		if ( foliageObject is null )
		{
			Log.Error( "Foliage object from pigment is null" );
			return false;
		}

		// Adjust since we're about to be parented
		transform = target.WorldTransform.ToLocal( transform );

		var useGlobalObjectSettings = brush.UseGlobalObjectSettings;

		var randomScale = useGlobalObjectSettings ? brush.GlobalObjectOverride.Scale : pigment.Settings.Scale;
		transform = transform.WithScale( randomScale.GetValue() );


		// Try to set tint
		var tint = useGlobalObjectSettings ? brush.GlobalObjectOverride.Tint : pigment.Settings.Tint;
		if ( foliageObject.Components.TryGet<ModelRenderer>( out var modelRenderer, FindMode.EverythingInSelfAndChildren ) )
		{
			var randomTint = tint.Evaluate( Random.Shared.Float( 0, 1 ) );
			modelRenderer.Tint = randomTint;
		}

		// All finished
		foliageObject.LocalTransform = transform;
		foliageObject.Tags.Add( "foliage" );
		foliageObject.Parent = target;
		foliageObject.Enabled = true;

		// Add our tracker component
		var foliageInfo = foliageObject.GetOrAddComponent<FoliageInfo>();
		foliageInfo.Radius = pigment.Radius();
		foliageInfo.PaletteResourceId = brush.ResourceId;
		foliageInfo.PigmentIndex = pigmentIndex;

		CurrentStroke.AddPaintedObject( pigmentIndex, foliageObject );
		return true;
	}
}
