using System;
using System.Collections.Generic;
using Sandbox;
namespace Foliage;

public partial class FoliagePainter
{
	private float[]? _brushAliasProbabilities;
	private int[]? _brushAliasIndices;
	private int _brushAliasWidth;
	private int _brushAliasHeight;

	private void BuildBrushAliasTable()
	{
		_brushAliasProbabilities = null;
		_brushAliasIndices = null;
		_brushAliasWidth = 0;
		_brushAliasHeight = 0;

		if ( Settings.Brush is null || Settings.Brush.Pixmap is null )
			return;

		var pixmap = Settings.Brush.Pixmap;
		var width = pixmap.Width;
		var height = pixmap.Height;

		if ( width <= 0 || height <= 0 )
			return;

		var count = width * height;
		var weights = new float[count];

		float totalWeight = 0f;

		for ( int y = 0; y < height; y++ )
		{
			for ( int x = 0; x < width; x++ )
			{
				var index = y * width + x;
				var color = pixmap.GetPixel( x, y );
				var weight = MathF.Max( 0f, color.Luminance * color.a );
				weights[index] = weight;
				totalWeight += weight;
			}
		}

		if ( totalWeight <= 0f )
		{
			// Fallback to uniform distribution
			_brushAliasProbabilities = new float[count];
			_brushAliasIndices = new int[count];
			for ( int i = 0; i < count; i++ )
			{
				_brushAliasProbabilities[i] = 1f;
				_brushAliasIndices[i] = i;
			}

			_brushAliasWidth = width;
			_brushAliasHeight = height;
			return;
		}

		var scaledWeights = new float[count];
		var small = new Queue<int>();
		var large = new Queue<int>();

		for ( int i = 0; i < count; i++ )
		{
			var scaled = weights[i] * count / totalWeight;
			scaledWeights[i] = scaled;
			if ( scaled < 1f )
				small.Enqueue( i );
			else
				large.Enqueue( i );
		}

		var probabilities = new float[count];
		var aliases = new int[count];

		while ( small.Count > 0 && large.Count > 0 )
		{
			var less = small.Dequeue();
			var more = large.Dequeue();

			probabilities[less] = scaledWeights[less];
			aliases[less] = more;

			scaledWeights[more] = (scaledWeights[more] + scaledWeights[less]) - 1f;

			if ( scaledWeights[more] < 1f )
				small.Enqueue( more );
			else
				large.Enqueue( more );
		}

		while ( large.Count > 0 )
		{
			var index = large.Dequeue();
			probabilities[index] = 1f;
			aliases[index] = index;
		}

		while ( small.Count > 0 )
		{
			var index = small.Dequeue();
			probabilities[index] = 1f;
			aliases[index] = index;
		}

		_brushAliasProbabilities = probabilities;
		_brushAliasIndices = aliases;
		_brushAliasWidth = width;
		_brushAliasHeight = height;
	}

	private Vector2 GetRandomBrushPixel()
	{
		if ( _brushAliasProbabilities is null || _brushAliasIndices is null || _brushAliasWidth <= 0 || _brushAliasHeight <= 0 )
		{
			BuildBrushAliasTable();

			if ( _brushAliasProbabilities is null || _brushAliasIndices is null || _brushAliasWidth <= 0 || _brushAliasHeight <= 0 )
				return Vector2.Zero;
		}

		var count = _brushAliasProbabilities.Length;
		if ( count == 0 )
			return Vector2.One * 0.5f;

		var index = System.Random.Shared.Next( count );
		var threshold = System.Random.Shared.Float( 0f, 1f );
		var chosen = threshold < _brushAliasProbabilities[index] ? index : _brushAliasIndices[index];

		var x = chosen % _brushAliasWidth;
		var y = chosen / _brushAliasWidth;

		var jitterX = System.Random.Shared.Float( 0f, 1f );
		var jitterY = System.Random.Shared.Float( 0f, 1f );

		var u = (x + jitterX) / _brushAliasWidth;
		var v = (y + jitterY) / _brushAliasHeight;

		return new Vector2( u, v );
	}
}
