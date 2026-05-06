using Sandbox;
using Sandbox.Citizen;
using System;
using System.Collections.Generic;

[Category( "Gameplay" ), Icon( "directions_run" )]
public sealed class TapperPlayerBean : Component
{
	private static readonly string[] HappyMorphCandidates =
	{
		"smile",
		"happy",
		"joy",
		"grin",
		"mouth_smile",
		"mouthSmile",
		"face_smile",
		"expression_happy",
		"emotion_happy",
		"happy_big",
		"smile_big"
	};

	private static readonly string[] NeutralizingMorphCandidates =
	{
		"sad",
		"frown",
		"angry",
		"mouth_frown",
		"mouthFrown",
		"face_sad",
		"expression_sad",
		"emotion_sad"
	};

	[Property] public bool IsLocalPlayer { get; set; }
	[Property] public float WalkSpeed { get; set; } = 185f;
	[Property] public float SprintSpeed { get; set; } = 300f;
	[Property] public float ClaimRange { get; set; } = 190f;
	[Property] public float CameraYaw { get; set; }
	[Property] public float CameraPitch { get; set; }
	[Property] public Vector3 LookTarget { get; set; }
	[Property] public bool IsFirstPersonView { get; set; }
	[Property] public float Happiness { get; set; }
	[Property] public SkinnedModelRenderer Renderer { get; set; }
	[Property] public CitizenAnimationHelper Animation { get; set; }

	private Rigidbody Body;
	private Vector3 LastWishVelocity;
	private Model CachedMorphModel;
	private HashSet<string> AvailableMorphs;
	private float DisplayedHappiness;

	public void Configure( bool isLocalPlayer, SkinnedModelRenderer renderer, CitizenAnimationHelper animation )
	{
		IsLocalPlayer = isLocalPlayer;
		Renderer = renderer;
		Animation = animation;
	}

	protected override void OnUpdate()
	{
		var hasNetworkOwner = GameObject.Network.Active;
		var canControl = IsLocalPlayer && (!hasNetworkOwner || !GameObject.Network.IsProxy);

		if ( Renderer.IsValid() )
			Renderer.Enabled = !(IsLocalPlayer && IsFirstPersonView);

		if ( !canControl )
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
		var hasNetworkOwner = GameObject.Network.Active;
		if ( !IsLocalPlayer || (hasNetworkOwner && GameObject.Network.IsProxy) )
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

		ApplyFacialExpression();

		if ( Animation.IsValid() )
		{
			Animation.IsGrounded = grounded;
			Animation.WithVelocity( velocity );
			Animation.WithWishVelocity( LastWishVelocity );
			Animation.WithLook( GetLookDirection(), 1f, 0.85f, 0.15f );
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

	private Vector3 GetLookDirection()
	{
		var eyePosition = WorldPosition + Vector3.Up * 72f;
		var direction = LookTarget - eyePosition;
		if ( direction.LengthSquared > 1f )
			return direction.Normal;

		return Rotation.From( CameraPitch, CameraYaw, 0f ).Forward;
	}

	private void ApplyFacialExpression()
	{
		if ( !Renderer.IsValid() )
			return;

		EnsureMorphCache();
		if ( AvailableMorphs is null || AvailableMorphs.Count == 0 )
			return;

		var target = MathF.Pow( Happiness.Clamp( 0f, 1f ), 0.55f );
		DisplayedHappiness = DisplayedHappiness.Approach( target, RealTime.Delta * 8f );

		foreach ( var morphName in GetMatchingMorphs( HappyMorphCandidates ) )
			SetMorph( morphName, DisplayedHappiness );

		foreach ( var morphName in GetMatchingMorphs( NeutralizingMorphCandidates ) )
			SetMorph( morphName, 0f );
	}

	private void EnsureMorphCache()
	{
		if ( CachedMorphModel == Renderer.Model && AvailableMorphs is not null )
			return;

		CachedMorphModel = Renderer.Model;
		AvailableMorphs = new HashSet<string>( StringComparer.OrdinalIgnoreCase );

		try
		{
			for ( var i = 0; i < Math.Max( 0, CachedMorphModel.MorphCount ); i++ )
			{
				var morphName = CachedMorphModel.GetMorphName( i );
				if ( !string.IsNullOrWhiteSpace( morphName ) )
					AvailableMorphs.Add( morphName );
			}
		}
		catch
		{
			AvailableMorphs.Clear();
		}
	}

	private IEnumerable<string> GetMatchingMorphs( IEnumerable<string> candidates )
	{
		if ( AvailableMorphs is null )
			yield break;

		foreach ( var morphName in AvailableMorphs )
		{
			var normalizedMorph = NormalizeMorphName( morphName );
			foreach ( var candidate in candidates )
			{
				if ( normalizedMorph.Contains( NormalizeMorphName( candidate ) ) )
				{
					yield return morphName;
					break;
				}
			}
		}
	}

	private void SetMorph( string morphName, float value )
	{
		if ( !Renderer.IsValid() )
			return;

		Renderer.Morphs.Set( morphName, value, 0.08f );
	}

	private static string NormalizeMorphName( string value )
	{
		return string.IsNullOrWhiteSpace( value )
			? ""
			: value.Replace( "_", "", StringComparison.Ordinal ).Replace( "-", "", StringComparison.Ordinal ).Replace( ".", "", StringComparison.Ordinal ).ToLowerInvariant();
	}
}
