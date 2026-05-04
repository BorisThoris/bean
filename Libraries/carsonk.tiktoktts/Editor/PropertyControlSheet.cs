using System;
using Editor;
using Sandbox;

namespace TikTokTTS.Editor;

public class PropertyControlSheet : GridLayout
{
	public SerializedObject TargetObject { get; set; }

	int rows = 0;

	public PropertyControlSheet() : base()
	{
		Margin = new Sandbox.UI.Margin( 16, 8, 16, 8 );
		HorizontalSpacing = 10;
		VerticalSpacing = 2;
		SetColumnStretch( 1, 2 );
		SetMinimumColumnWidth( 0, 120 );
	}

	public void AddObject( SerializedObject obj )
	{
		foreach ( var entry in obj )
		{
			if ( !entry.HasAttribute<PropertyAttribute>() )
			{
				continue;
			}
			if ( entry.PropertyType.Name.StartsWith( "Action" ) )
			{
				continue;
			}

			AddRow( entry );
		}
	}

	/// <summary>
	/// Add a serialized property row. This will create an editor for the row and a label.
	/// </summary>
	public void AddRow( SerializedProperty property, float labelIndent = 0.0f )
	{
		var editor = ControlWidget.Create( property );
		if ( editor is null )
			return;

		if ( editor.IsWideMode )
		{
			AddCell( 0, rows++, new Label( property.DisplayName ) { MinimumHeight = Theme.RowHeight, Alignment = TextFlag.LeftCenter }, 2, 1, TextFlag.LeftTop );
			var lo = AddCell( 0, rows, Layout.Column(), 2, 1, TextFlag.LeftTop );

			lo.Margin = new Sandbox.UI.Margin( 16, 0, 0, 0 );
			lo.Add( editor );
		}
		else
		{
			int cell = 0;

			var label = AddCell( cell++, rows, new Label( property.DisplayName ), 1, 1, TextFlag.LeftTop );
			label.MinimumHeight = Theme.RowHeight;
			label.Alignment = TextFlag.LeftCenter;
			label.SetStyles( "color: #aaa;" );

			label.ToolTip = property.Description ?? property.DisplayName;

			if ( labelIndent > 0 )
			{
				label.ContentMargins = new Sandbox.UI.Margin( labelIndent, 0, 0, 0 );
			}

			AddCell( cell++, rows, editor, 1, 1, TextFlag.LeftTop );
		}

		rows++;
	}

	/// <summary>
	/// Add a layout to a double wide cell
	/// </summary>
	public void AddLayout( Layout layout )
	{
		AddCell( 0, rows++, layout, 2, 1, TextFlag.LeftTop );
	}

}