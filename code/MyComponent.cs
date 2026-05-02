using Sandbox;
using System.Collections.Generic;

public sealed class MyCameraController : Component
{
	[Property] public float MoveSpeed { get; set; } = 200f;

	protected override void OnUpdate()
	{
		Vector3 movement = Vector3.Zero;

		// Handle movement input
		if (Input.Down("Forward")) movement += WorldRotation.Forward;
		if (Input.Down("Backward")) movement -= WorldRotation.Forward;
		if (Input.Down("Right")) movement -= WorldRotation.Left;
		if (Input.Down("Left")) movement -= WorldRotation.Right;
		if (Input.Down("Jump")) movement += Vector3.Up;
		if (Input.Down("Duck")) movement -= Vector3.Up;

		// Apply movement to the camera
		WorldPosition += movement.Normal * MoveSpeed * Time.Delta;

		// Debug: Log current inputs
		LogCurrentInputs();
	}

	private void LogCurrentInputs()
	{
		List<string> inputs = new List<string>
		{
			"Forward", "Backward", "Left", "Right", "Jump", "Duck"
		};

		foreach (var input in inputs)
		{
			if (Input.Down(input))
			{
				Log.Info($"{input} key is currently pressed.");
			}
		}
	}
}
