HEADER
{
	DevShader = true;
	Description = "Seamless sphere-space water projection shader";
	Version = 1;
}

FEATURES
{
	#include "common/features.hlsl"
}

MODES
{
	Forward();
	Depth();
}

COMMON
{
	#include "common/shared.hlsl"
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
};

VS
{
	#include "common/vertex.hlsl"

	PixelInput MainVs( VertexInput i )
	{
		PixelInput o = ProcessVertex( i );
		return FinalizeVertex( o );
	}
}

PS
{
	#define CUSTOM_MATERIAL_INPUTS 1
	#include "common/pixel.hlsl"

	RenderState( CullMode, NONE );
	RenderState( BlendEnable, true );
	RenderState( SrcBlend, SRC_ALPHA );
	RenderState( DstBlend, INV_SRC_ALPHA );
	RenderState( DepthWriteEnable, false );

	float g_flProjectionSpeed < Default( 0.18 ); Range( 0.0, 1.0 ); UiGroup( "Projection,10/10" ); >;
	float g_flProjectionDistort < Default( 0.34 ); Range( 0.0, 1.0 ); UiGroup( "Projection,10/20" ); >;
	float g_flProjectionRippleScale < Default( 3.8 ); Range( 0.5, 16.0 ); UiGroup( "Projection,10/30" ); >;
	float g_flProjectionFoamAmount < Default( 0.38 ); Range( 0.0, 1.0 ); UiGroup( "Projection,10/40" ); >;
	float g_flProjectionGlow < Default( 2.65 ); Range( 0.25, 8.0 ); UiGroup( "Projection,10/50" ); >;
	float g_flProjectionAlpha < Default( 1.0 ); Range( 0.0, 1.0 ); UiGroup( "Projection,10/60" ); >;
	float g_flProjectionFloorMode < Default( 0.0 ); Range( 0.0, 1.0 ); UiGroup( "Projection,10/70" ); >;
	float3 g_vProjectionLowColor < UiType( Color ); Default3( 0.012, 0.045, 0.14 ); UiGroup( "Projection,20/10" ); >;
	float3 g_vProjectionMidColor < UiType( Color ); Default3( 0.035, 0.33, 0.76 ); UiGroup( "Projection,20/20" ); >;
	float3 g_vProjectionHighColor < UiType( Color ); Default3( 0.62, 0.92, 1.0 ); UiGroup( "Projection,20/30" ); >;

	float SphereWave( float3 p, float3 axis, float scale, float phase )
	{
		float a = dot( p, normalize( axis ) ) * scale + phase;
		return sin( a ) * 0.5 + 0.5;
	}

	float SphereNoise( float3 p, float time )
	{
		float scale = max( 0.1, g_flProjectionRippleScale );
		float n = 0.0;
		n += SphereWave( p, float3( 1.0, 0.42, -0.18 ), scale * 3.8, time * 1.35 ) * 0.32;
		n += SphereWave( p, float3( -0.24, 1.0, 0.36 ), scale * 5.7, -time * 1.9 ) * 0.24;
		n += SphereWave( p, float3( 0.46, -0.31, 1.0 ), scale * 8.2, time * 2.55 ) * 0.18;
		n += SphereWave( p + p.yzx * g_flProjectionDistort, float3( 0.77, 0.19, 0.61 ), scale * 12.4, -time * 3.2 ) * 0.14;
		n += SphereWave( p.zxy - p.xyz * 0.28, float3( -0.58, 0.74, 0.33 ), scale * 17.0, time * 4.15 ) * 0.12;
		return saturate( n );
	}

	float3 FlowDomain( float3 p, float time )
	{
		float3 swirl;
		swirl.x = sin( p.y * 7.1 + time * 1.7 ) + cos( p.z * 5.4 - time * 1.1 );
		swirl.y = sin( p.z * 6.3 - time * 1.3 ) + cos( p.x * 4.8 + time * 1.6 );
		swirl.z = sin( p.x * 5.8 + time * 1.2 ) + cos( p.y * 6.7 - time * 1.4 );

		float3 flowed = normalize( p + swirl * g_flProjectionDistort * 0.18 );
		float roll = time * 0.22;
		float s = sin( roll );
		float c = cos( roll );
		return normalize( float3( flowed.x * c - flowed.y * s, flowed.x * s + flowed.y * c, flowed.z ) );
	}

	float ApproxFresnel( PixelInput i )
	{
		float3 normal = normalize( i.vNormalWs.xyz );
		float3 viewDir = normalize( -CalculatePositionToCameraDirWs( i.vPositionWithOffsetWs.xyz + g_vHighPrecisionLightingOffsetWs.xyz ) );
		return pow( 1.0 - saturate( abs( dot( normal, viewDir ) ) ), 2.1 );
	}

	float4 MainPs( PixelInput i ) : SV_Target0
	{
		float time = g_flTime * g_flProjectionSpeed;
		if ( g_flProjectionFloorMode > 0.5 )
		{
			float2 uv = i.vTextureCoords.xy;
			float2 centered = uv - 0.5;
			float radial = length( centered );
			float waveA = sin( uv.x * g_flProjectionRippleScale * 2.2 + time * 9.0 );
			float waveB = cos( uv.y * g_flProjectionRippleScale * 2.8 - time * 7.2 );
			float waveC = sin( radial * g_flProjectionRippleScale * 9.0 - time * 12.0 );
			float liquid = smoothstep( -0.5, 1.0, waveA * 0.36 + waveB * 0.32 + waveC * 0.22 );
			float highlight = smoothstep( 0.72, 0.98, liquid );
			float fresnel = ApproxFresnel( i );

			float3 color = lerp( g_vProjectionLowColor, g_vProjectionMidColor, liquid );
			color = lerp( color, g_vProjectionHighColor, highlight * g_flProjectionFoamAmount );
			color += g_vProjectionHighColor * (highlight * 0.18 + fresnel * 0.22);

			float alpha = saturate( g_flProjectionAlpha + highlight * 0.08 + fresnel * 0.12 );
			return float4( color * g_flProjectionGlow, alpha );
		}

		float3 sphereDir = normalize( -i.vNormalWs.xyz );
		float3 domainA = FlowDomain( sphereDir, time );
		float3 domainB = FlowDomain( sphereDir.zxy, time * -0.73 + 4.1 );

		float fluidA = SphereNoise( domainA, time );
		float fluidB = SphereNoise( domainB, time * 1.31 + 2.7 );
		float fluid = saturate( fluidA * 0.68 + fluidB * 0.32 );
		float fluidSoft = smoothstep( 0.18, 0.9, fluid );

		float crestA = SphereWave( domainA, float3( 0.91, -0.22, 0.41 ), g_flProjectionRippleScale * 22.0, time * 5.2 );
		float crestB = SphereWave( domainB, float3( -0.35, 0.88, 0.29 ), g_flProjectionRippleScale * 18.0, -time * 4.6 );
		float crests = smoothstep( 0.78, 0.98, crestA * crestB );
		float foam = smoothstep( 0.6, 0.96, fluid + crests * 0.5 ) * g_flProjectionFoamAmount;
		float pocket = smoothstep( 0.12, 0.52, 1.0 - fluid );
		float fresnel = ApproxFresnel( i );

		float3 color = lerp( g_vProjectionLowColor, g_vProjectionMidColor, fluidSoft );
		color = lerp( color, g_vProjectionHighColor, saturate( foam + crests * 0.22 ) );
		color += pocket * float3( 0.0, 0.02, 0.09 );
		color += crests * float3( 0.04, 0.24, 0.48 );
		color += fresnel * float3( 0.15, 0.46, 0.88 );

		return float4( color * g_flProjectionGlow, g_flProjectionAlpha );
	}
}
