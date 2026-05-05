using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace Foliage;

public partial class FoliagePainter
{
	private void Erase( SceneTraceResult paintTrace )
	{
		if ( Settings.ContainerObject is null ) return;

		// Update brush rotation if RotateBrush is enabled
		if ( Settings.BrushMode == BrushMode.Texture && Settings.RotateBrushOnPaint )
		{
			var variance = Random.Shared.Float( -Settings.RotationAmountVariance, Settings.RotationAmountVariance );
			Settings.CurrentBrushRotation = Settings.RotationAmount + variance;
		}

		var didErase = false;
		// Try up to 10 times to find an object to erase
		for ( int attempt = 0; attempt < 50; attempt++ )
		{
			if ( TryErase( paintTrace ) )
			{
				didErase = true;
				break;
			}
		}

		if ( !didErase )
		{
			Log.Info( "Failed to erase any foliage" );
		}
	}

	private bool TryErase( SceneTraceResult paintTrace )
	{
		var container = Settings.ContainerObject;
		List<GameObject> candidateObjects = [];
		if ( container is null )
		{
			// Find all objects that have FoliageInfo in the scene
			candidateObjects = Scene.GetAllObjects( true ).Where( obj => obj.Components.TryGet<FoliageInfo>( out _ ) ).ToList();
		}
		else
		{
			candidateObjects = [.. container.Children];
		}

		if ( candidateObjects.Count == 0 )
			return false;

		// Get a random position within the brush
		Vector3 erasePosition;
		if ( Settings.BrushMode == BrushMode.Texture )
		{
			if ( Settings.Brush is null ) return false;
			var randomBrushPixel = GetRandomBrushPixel();

			// Rotate UV coordinates around center (0.5, 0.5) if RotateBrush is enabled
			if ( Settings.RotateBrushOnPaint && Settings.CurrentBrushRotation != 0f )
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
			erasePosition = paintTrace.HitPosition + scaledRandomPosition;
		}
		else
		{
			var angle = Random.Shared.Float( 0f, MathF.Tau );
			var radius = MathF.Sqrt( Random.Shared.Float( 0f, 1f ) ) * Settings.Size;

			var a = MathF.Abs( paintTrace.Normal.z ) < 0.999f ? Vector3.Up : Vector3.Left;
			var u = paintTrace.Normal.Cross( a ).Normal;
			var v = paintTrace.Normal.Cross( u ).Normal;

			erasePosition = paintTrace.HitPosition + radius * (u * MathF.Cos( angle ) + v * MathF.Sin( angle ));
		}

		// Filter by distance (within search radius from erase position)
		var searchRadiusSquared = Settings.EraseSearchRadius * Settings.EraseSearchRadius;
		var objectsInRange = candidateObjects
			.Where( obj =>
			{
				var distanceSquared = Vector3.DistanceBetweenSquared( erasePosition, obj.WorldPosition );
				return distanceSquared <= searchRadiusSquared;
			} )
			.ToList();

		if ( objectsInRange.Count == 0 )
			return false;

		// Filter by palette if EraseOnlyPalette is enabled
		if ( Settings.EraseOnlyPalette && Settings.Palette is not null )
		{
			objectsInRange = objectsInRange
				.Where( obj =>
				{
					if ( !obj.Components.TryGet<FoliageInfo>( out var foliageInfo ) )
						return false;
					return foliageInfo.PaletteResourceId == Settings.Palette.ResourceId;
				} )
				.ToList();

			if ( objectsInRange.Count == 0 )
				return false;
		}

		// Pick a random object and delete it
		var objectToErase = Game.Random.FromList( objectsInRange );
		if ( objectToErase is not null && objectToErase.IsValid() )
		{
			objectToErase.Destroy();
			return true;
		}

		return false;
	}
}

