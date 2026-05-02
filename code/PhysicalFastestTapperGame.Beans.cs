using Sandbox;
using Sandbox.Citizen;
using System;

public sealed partial class PhysicalFastestTapperGame
{
	private void EnsurePlayerBeans()
	{
		for ( var i = 0; i < Players.Count; i++ )
		{
			var player = Players[i];
			EnsurePlayerBean( player );
			UpdatePlayerBeanVisuals( player, i );
		}
	}

	private void EnsurePlayerBean( PlayerScore player )
	{
		if ( player is null )
			return;

		if ( player.Bean.IsValid() && player.BeanController.IsValid() )
			return;

		player.ConnectionKey ??= ConnectionKey( player.Connection );
		var bean = FindOrCreate( $"Tapper Bean {player.ConnectionKey}" );
		bean.LocalPosition = GetBeanSpawnPosition( Players.IndexOf( player ) );
		bean.LocalRotation = Rotation.FromYaw( 0f );
		bean.LocalScale = Vector3.One;

		var clothing = ClothingContainer.CreateFromLocalUser();
		var renderer = bean.Components.GetOrCreate<SkinnedModelRenderer>();
		renderer.Model = Model.Load( clothing.PrefersHuman ? "models/citizen_human/citizen_human_female.vmdl" : "models/citizen/citizen.vmdl" );
		renderer.UseAnimGraph = true;
		renderer.Tint = Color.White;

		var dresser = bean.Components.GetOrCreate<Dresser>();
		dresser.Source = Dresser.ClothingSource.LocalUser;
		dresser.BodyTarget = renderer;
		dresser.ApplyHeightScale = true;
		dresser.RemoveUnownedItems = true;
		clothing.Apply( renderer );

		var animation = bean.Components.GetOrCreate<CitizenAnimationHelper>();
		animation.Target = renderer;

		var collider = bean.Components.GetOrCreate<CapsuleCollider>();
		collider.Radius = 18f;
		collider.Start = Vector3.Up * 8f;
		collider.End = Vector3.Up * 76f;
		collider.Static = false;
		collider.IsTrigger = true;

		var controller = bean.Components.GetOrCreate<TapperPlayerBean>();
		controller.Configure( IsLocalPlayer( player ), renderer, animation );

		var labelObject = FindOrCreate( $"Tapper Bean {player.ConnectionKey} Name" );
		labelObject.SetParent( bean, true );
		labelObject.LocalPosition = new Vector3( 0f, 0f, 92f );
		labelObject.LocalRotation = Rotation.FromYaw( 35f );
		labelObject.LocalScale = Vector3.One;
		var label = labelObject.Components.GetOrCreate<TextRenderer>();
		label.Scale = 0.24f;
		label.Color = Color.White;

		player.Bean = bean;
		player.BeanController = controller;
		player.BeanNameText = label;
	}

	private void UpdatePlayerBeanVisuals( PlayerScore player, int slot )
	{
		if ( player is null )
			return;

		if ( !player.Bean.IsValid() )
			return;

		if ( player.BeanController.IsValid() )
			player.BeanController.IsLocalPlayer = IsLocalPlayer( player );

		if ( player.BeanNameText.IsValid() )
		{
			player.BeanNameText.GameObject.Enabled = true;
			var stationText = player.StationIndex >= 0 ? $"S{player.StationIndex + 1}" : "UNCLAIMED";
			SetText( player.BeanNameText, $"{player.Name}\n{stationText}" );
			player.BeanNameText.Color = player.StationIndex >= 0 ? ReadyStationColor : Color.White;
		}

		if ( player.StationIndex < 0 && player.Bean.WorldPosition.LengthSquared < 1f )
			player.Bean.WorldPosition = GetBeanSpawnPosition( slot );
	}

	private bool IsLocalPlayer( PlayerScore player )
	{
		return player is not null && (player.ConnectionKey ?? ConnectionKey( player.Connection )) == ConnectionKey( Connection.Local );
	}

	private Vector3 GetBeanSpawnPosition( int slot )
	{
		var lane = slot < 0 ? 0 : slot;
		var stage = GetVenueStageOrigin();
		return stage + new Vector3( -430f, -560f + lane * 110f, 84f );
	}

	private bool IsPlayerCloseEnoughToClaim( PlayerScore player, TapperStation station )
	{
		if ( player?.BeanController is null || !player.BeanController.IsValid() )
			return true;

		return player.BeanController.IsWithinClaimRange( station.Origin );
	}

	private void MoveBeanToClaimedStation( PlayerScore player, TapperStation station )
	{
		if ( player?.Bean is null || !player.Bean.IsValid() || station is null )
			return;

		player.Bean.WorldPosition = station.Origin + new Vector3( -120f, -145f, 84f );
		var look = (station.Origin - player.Bean.WorldPosition).WithZ( 0f );
		if ( look.LengthSquared > 1f )
			player.Bean.WorldRotation = Rotation.LookAt( look.Normal, Vector3.Up );
	}

	private PlayerScore GetLocalPlayer()
	{
		var key = ConnectionKey( Connection.Local );
		return PlayersByConnection.TryGetValue( key, out var player ) ? player : null;
	}

	private void DeletePlayerBean( PlayerScore player )
	{
		if ( player?.BeanNameText is not null && player.BeanNameText.GameObject.IsValid() )
			player.BeanNameText.GameObject.Destroy();

		if ( player?.Bean is not null && player.Bean.IsValid() )
			player.Bean.Destroy();

		if ( player is null )
			return;

		player.Bean = null;
		player.BeanController = null;
		player.BeanNameText = null;
	}
}
