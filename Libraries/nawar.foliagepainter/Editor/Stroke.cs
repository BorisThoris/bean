using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace Foliage;

public class Stroke
{
	public FoliagePalette Palette { get; private set; }

	public List<int> MaxObjects { get; private set; } = [];
	public List<List<GameObject>> PaintedObjects { get; private set; } = [];
	public List<IFoliagePigment> Pigments { get; private set; } = [];
	public List<FoliageInfo> ExistingFoliage { get; private set; } = [];


	public int TotalPaintedObjects { get; private set; } = 0;
	public int TotalObjectsToPaint { get; private set; } = 0;

	public bool HasFinished => TotalPaintedObjects >= TotalObjectsToPaint;

	public Stroke()
	{
		MaxObjects = [];
		PaintedObjects = [];

		// We'll never actually paint with this brush
		Palette = null!;
	}

	public Stroke( FoliagePainterSettings settings )
	{
		MaxObjects = [];
		PaintedObjects = [];
		ExistingFoliage = [];
		if ( settings.ContainerObject is null )
		{
			throw new Exception( "No container object selected" );
		}
		if ( settings.Palette is null )
		{
			throw new Exception( "No brush selected" );
		}

		Palette = settings.Palette;
		if ( settings.IncludeExistingFoliageForSpacing && settings.ContainerObject is not null )
		{
			ExistingFoliage = settings.ContainerObject.GetComponentsInChildren<FoliageInfo>().ToList();
		}

		var useGlobalObjectSettings = Palette.UseGlobalObjectSettings;

		Pigments = [];
		if ( Palette.Mode == FoliagePalette.FoliageMode.Prefab )
		{
			Pigments.AddRange( Palette.FoliagePrefabs );
		}
		else
		{
			Pigments.AddRange( Palette.FoliageModels );
		}

		for ( int i = 0; i < Pigments.Count; i++ )
		{
			var maxObjectsPerStroke = useGlobalObjectSettings ? Palette.GlobalObjectOverride.MaxPerStroke : Pigments[i].Settings.MaxPerStroke;

			var countMultiplier = useGlobalObjectSettings ? Palette.GlobalObjectOverride.CountMultiplier : Pigments[i].Settings.CountMultiplier;
			var calculatedCount = (int)MathF.Round( settings.ObjectsPaintedPerStroke * countMultiplier.GetValue() );

			var actualCount = calculatedCount;
			if ( maxObjectsPerStroke.GetValue() < 0 )
				actualCount = (int)Math.Clamp( calculatedCount, 0, MathF.Round( maxObjectsPerStroke.GetValue() ) );

			Log.Info( $"Pigment {i}: Max objects per stroke: {actualCount}" );

			MaxObjects.Add( actualCount );
			PaintedObjects.Add( [] );

			TotalObjectsToPaint += actualCount;
		}
	}

	public int GetPigmentIndex( IFoliagePigment pigment )
	{
		return Pigments.FindIndex( p => p == pigment );
	}

	public IFoliagePigment? GetPigment( int pigmentIndex )
	{
		if ( pigmentIndex >= Pigments.Count || pigmentIndex < 0 )
		{
			return null;
		}
		return Pigments[pigmentIndex];
	}

	public bool HasPigment( int pigmentIndex )
	{
		return !(pigmentIndex >= Pigments.Count || pigmentIndex < 0);
	}

	public IFoliagePigment? GetRandomPigment()
	{
		var shuffled = Pigments.OrderBy( p => Game.Random.Float() ).ToList();
		// Find the first pigment that we can paint
		foreach ( var pigment in shuffled )
		{
			if ( CanPaintPigment( GetPigmentIndex( pigment ) ) )
			{
				return pigment;
			}
		}
		return null;
	}

	public int GetMaxObjectsCount( int pigmentIndex )
	{
		if ( !HasPigment( pigmentIndex ) )
		{
			Log.Error( $"Pigment index out of range: {pigmentIndex} vs {Pigments.Count}" );
			return 0;
		}
		return MaxObjects[pigmentIndex];
	}

	public int GetPaintedObjectsCount( int pigmentIndex )
	{
		if ( !HasPigment( pigmentIndex ) )
		{
			Log.Error( $"Pigment index out of range: {pigmentIndex} vs {Pigments.Count}" );
			return 0;
		}
		return PaintedObjects[pigmentIndex].Count;
	}

	public void AddPaintedObject( int pigmentIndex, GameObject foliageObject )
	{
		if ( !HasPigment( pigmentIndex ) )
		{
			Log.Error( $"Pigment index out of range: {pigmentIndex} vs {Pigments.Count}" );
			return;
		}

		var paintedObjects = PaintedObjects[pigmentIndex];
		paintedObjects.Add( foliageObject );

		TotalPaintedObjects++;
	}

	public bool IsPositionTooClose( IFoliagePigment pigmentToPaint, Vector3 position, float spacingAmount, bool includeBoundsInSpacing )
	{
		if ( spacingAmount <= 0f && !includeBoundsInSpacing )
			return false;

		// Check against objects painted in this stroke
		foreach ( var paintedObjectList in PaintedObjects )
		{
			var pigmentIndex = PaintedObjects.IndexOf( paintedObjectList );
			var existingPigment = GetPigment( pigmentIndex );

			foreach ( var paintedObject in paintedObjectList )
			{
				if ( !paintedObject.IsValid() )
					continue;

				var paintedPosition = paintedObject.WorldPosition;
				var distance = Vector3.DistanceBetween( position, paintedPosition );

				var requiredDistance = spacingAmount;
				if ( includeBoundsInSpacing )
				{
					var toPaintRadius = pigmentToPaint?.Radius() ?? 0f;
					var existingFoliageRadius = existingPigment?.Radius() ?? 0f;
					requiredDistance += toPaintRadius + existingFoliageRadius;
				}

				if ( distance < requiredDistance )
				{
					return true;
				}
			}
		}

		// Check against existing foliage objects
		foreach ( var foliageInfo in ExistingFoliage )
		{
			if ( !foliageInfo.IsValid() )
				continue;

			var existingPosition = foliageInfo.GameObject.WorldPosition;
			var distance = Vector3.DistanceBetweenSquared( position, existingPosition );

			var requiredDistance = spacingAmount;
			if ( includeBoundsInSpacing )
			{
				var toPaintRadius = pigmentToPaint?.Radius() ?? 0f;
				var existingFoliageRadius = foliageInfo.Radius;
				requiredDistance += toPaintRadius + existingFoliageRadius;
			}

			if ( distance < requiredDistance * requiredDistance )
			{
				return true;
			}
		}

		return false;
	}

	public bool CanPaintPigment( int pigmentIndex )
	{
		var useGlobalObjectSettings = Palette.UseGlobalObjectSettings;

		var pigment = GetPigment( pigmentIndex );
		if ( pigment is null )
		{
			Log.Error( $"Pigment is null" );
			return false;
		}

		var maxObjectsCount = GetMaxObjectsCount( pigmentIndex );
		var paintedObjectsCount = GetPaintedObjectsCount( pigmentIndex );

		return paintedObjectsCount < maxObjectsCount;
	}
}
