using Editor;
using Sandbox;
using System.Linq;

namespace TikTokTTS.Editor;

[CustomEditor( typeof( TikTokSpeech ) )]
public class TikTokSpeechComponentEditor : ComponentEditorWidget
{

	SerializedObject Target;

	public TikTokSpeechComponentEditor( SerializedObject obj ) : base( obj )
	{
		Target = obj;
		Layout = Layout.Column();
		var sheet = new PropertyControlSheet();
		sheet.AddObject( obj );
		Layout.Add( sheet );
		Layout.Spacing = 2;
		MinimumHeight = 70;

		var buttonPanel = Layout.Column();
		buttonPanel.Margin = new Sandbox.UI.Margin( 12, 0 );
		var button = new Button( "Speak", "play_arrow" )
		{
			Width = 200,
			Clicked = () =>
			{
				var component = Target.Targets.First() as TikTokSpeech;
				component?.Speak();
			}
		};
		buttonPanel.Add( button );
		Layout.Add( buttonPanel );
	}

	protected override void OnPaint()
	{
	}

}