using Sandbox;
using Sandbox.Citizen;

[Category( "Gameplay" ), Icon( "directions_run" )]
public sealed class TapperPlayerBean : Component
{
	[Property] public bool IsLocalPlayer { get; set; }
	[Property] public float MoveSpeed { get; set; } = 185f;
	[Property] public float ClaimRange { get; set; } = 190f;
	[Property] public SkinnedModelRenderer Renderer { get; set; }
	[Property] public CitizenAnimationHelper Animation { get; set; }

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

		if ( !IsLocalPlayer )
		{
			ApplyAnimation( Vector3.Zero );
			return;
		}

		var input = Input.AnalogMove;
		var wish = new Vector3( input.x, input.y, 0f );
		if ( wish.LengthSquared > 1f )
			wish = wish.Normal;

		var velocity = wish * MoveSpeed;
		if ( velocity.LengthSquared > 0.5f )
		{
			WorldPosition += velocity * Time.Delta;
			WorldRotation = Rotation.LookAt( velocity.WithZ( 0f ).Normal, Vector3.Up );
		}

		ApplyAnimation( velocity );
	}

	private void ApplyAnimation( Vector3 velocity )
	{
		if ( !Renderer.IsValid() )
			return;

		if ( Animation.IsValid() )
		{
			Animation.IsGrounded = true;
			Animation.WithVelocity( velocity );
			Animation.WithWishVelocity( velocity );
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
