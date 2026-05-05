using Sandbox;
using System;
using System.Linq;

public sealed partial class PhysicalFastestTapperGame
{
	private GameObject FindOrCreate( string name )
	{
		var gameObject = Scene.Directory.FindByName( name ).FirstOrDefault();
		return gameObject.IsValid() ? gameObject : new GameObject( name );
	}

	private GameObject FindSceneObject( string name )
	{
		return Scene.Directory.FindByName( name ).FirstOrDefault();
	}

	private static bool TryParseTrailingNumber( string value, out int number )
	{
		number = -1;
		if ( string.IsNullOrWhiteSpace( value ) )
			return false;

		var end = value.Length - 1;
		while ( end >= 0 && char.IsWhiteSpace( value[end] ) )
			end--;

		var start = end;
		while ( start >= 0 && char.IsDigit( value[start] ) )
			start--;

		if ( start == end )
			return false;

		return int.TryParse( value.Substring( start + 1, end - start ), out number );
	}

	private static void SetText( TextRenderer textRenderer, string value )
	{
		if ( !textRenderer.IsValid() )
			return;

		var textScope = textRenderer.TextScope;
		textScope.Text = value;
		textRenderer.TextScope = textScope;
	}

	private static float SmoothStep( float min, float max, float value )
	{
		var t = ((value - min) / (max - min)).Clamp( 0f, 1f );
		return t * t * (3f - 2f * t);
	}

	private static void TryPlaySound( string soundName )
	{
		try
		{
			Sound.Play( soundName );
		}
		catch
		{
		}
	}
}
