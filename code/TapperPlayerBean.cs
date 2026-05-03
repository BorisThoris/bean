using Sandbox;
using Sandbox.Citizen;

[Category( "Gameplay" ), Icon( "directions_run" )]
public sealed class TapperPlayerBean : Component
{
	[Property] public bool IsLocalPlayer { get; set; }
	[Property] public float WalkSpeed { get; set; } = 185f;
	[Property] public float SprintSpeed { get; set; } = 300f;
	[Property] public float ClaimRange { get; set; } = 190f;
	[Property] public float CameraYaw { get; set; }
	[Property] public SkinnedModelRenderer Renderer { get; set; }
	[Property] public CitizenAnimationHelper Animation { get; set; }

	private Rigidbody Body;
	private PlayerController Controller;
	private Vector3 LastWishVelocity;

	public void Configure( bool isLocalPlayer, SkinnedModelRenderer renderer, CitizenAnimationHelper animation )
	{
		IsLocalPlayer = isLocalPlayer;
		Renderer = renderer;
		Animation = animation;
	}

	protected override void OnUpdate()
	{
		if ( Renderer.IsValid() )
			Renderer.Enabled = true;

		ConfigurePlayerController();

		if ( !IsLocalPlayer )
		{
			ApplyAnimation( Body.IsValid() ? Body.Velocity : Vector3.Zero, true );
			return;
		}

		var wishVelocity = BuildWishVelocity();
		LastWishVelocity = wishVelocity;
		if ( wishVelocity.WithZ( 0f ).LengthSquared > 0.5f )
			WorldRotation = Rotation.LookAt( wishVelocity.WithZ( 0f ).Normal, Vector3.Up );

		ApplyAnimation( Body.IsValid() ? Body.Velocity : wishVelocity, IsGrounded() );
	}

	protected override void OnFixedUpdate()
	{
		if ( !IsLocalPlayer )
			return;

		Body ??= Components.Get<Rigidbody>();
		if ( !Body.IsValid() )
			return;

		var wishVelocity = BuildWishVelocity();
		LastWishVelocity = wishVelocity;
		Body.Velocity = new Vector3( wishVelocity.x, wishVelocity.y, Body.Velocity.z );
	}

	private bool IsGrounded()
	{
		var trace = Scene.Trace
			.Ray( WorldPosition + Vector3.Up * 6f, WorldPosition + Vector3.Down * 10f )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "trigger" )
			.Run();

		return trace.Hit;
	}

	private void ConfigurePlayerController()
	{
		Body ??= Components.Get<Rigidbody>();
		Controller ??= Components.Get<PlayerController>();
		if ( !Controller.IsValid() )
			return;

		Controller.WalkSpeed = WalkSpeed;
		Controller.RunSpeed = SprintSpeed;
		Controller.RunByDefault = false;
		Controller.AltMoveButton = "run";
		Controller.UseInputControls = false;
		Controller.UseLookControls = false;
		Controller.UseCameraControls = false;
		Controller.UseAnimatorControls = false;
		Controller.ThirdPerson = false;
		Controller.CameraOffset = Vector3.Zero;
		Controller.Body = Body;

		if ( Renderer.IsValid() )
			Controller.Renderer = Renderer;
	}

	private Vector3 BuildWishVelocity()
	{
		var input = Input.AnalogMove;
		var cameraRotation = Rotation.FromYaw( CameraYaw );
		var forward = cameraRotation.Forward.WithZ( 0f ).Normal;
		var right = cameraRotation.Right.WithZ( 0f ).Normal;
		var wish = forward * input.x - right * input.y;
		if ( wish.LengthSquared > 1f )
			wish = wish.Normal;

		return wish * (IsSprinting() ? SprintSpeed : WalkSpeed);
	}

	private static bool IsSprinting()
	{
		return Input.Down( "run" ) || Input.Keyboard.Down( "SHIFT" ) || Input.Keyboard.Down( "LEFTSHIFT" ) || Input.Keyboard.Down( "RIGHTSHIFT" );
	}

	private void ApplyAnimation( Vector3 velocity, bool grounded )
	{
		if ( !Renderer.IsValid() )
			return;

		if ( Animation.IsValid() )
		{
			Animation.IsGrounded = grounded;
			Animation.WithVelocity( velocity );
			Animation.WithWishVelocity( LastWishVelocity );
			Animation.MoveStyle = CitizenAnimationHelper.MoveStyles.Auto;
			return;
		}

		Renderer.Set( "move_speed", velocity.Length );
		Renderer.Set( "move_groundspeed", velocity.WithZ( 0f ).Length );
	}

	public bool IsWithinClaimRange( Vector3 stationOrigin )
	{
		return WorldPosition.Distance( stationOrigin ) <= ClaimRange;
	}
}
