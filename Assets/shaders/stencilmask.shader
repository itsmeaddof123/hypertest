
HEADER
{
	Description = "";
}

FEATURES
{
	#include "common/features.hlsl"
}

MODES
{
	VrForward();
	Depth(); 
	ToolsVis( S_MODE_TOOLS_VIS );
	ToolsWireframe( "vr_tools_wireframe.shader" );
	ToolsShadingComplexity( "tools_shading_complexity.shader" );
}

COMMON
{
	#ifndef S_ALPHA_TEST
	#define S_ALPHA_TEST 0
	#endif
	#ifndef S_TRANSLUCENT
	#define S_TRANSLUCENT 0
	#endif
	
	#include "common/shared.hlsl"
	#include "procedural.hlsl"

	#define S_UV2 1
	#define CUSTOM_MATERIAL_INPUTS
	
	uint BufferRefEx = 45;
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"
	float4 vColor : COLOR0 < Semantic( Color ); >;
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
	float3 vPositionOs : TEXCOORD14;
	float3 vNormalOs : TEXCOORD15;
	float4 vTangentUOs_flTangentVSign : TANGENT	< Semantic( TangentU_SignV ); >;
	float4 vColor : COLOR0;
	float4 vTintColor : COLOR1;
};

VS
{
	#include "common/vertex.hlsl"

	PixelInput MainVs( VertexInput v )
	{
		PixelInput i = ProcessVertex( v );
		i.vPositionOs = v.vPositionOs.xyz;
		i.vColor = v.vColor;

		ExtraShaderData_t extraShaderData = GetExtraPerInstanceShaderData( v );
		i.vTintColor = extraShaderData.vTint;

		VS_DecodeObjectSpaceNormalAndTangent( v, i.vNormalOs, i.vTangentUOs_flTangentVSign );

		return FinalizeVertex( i );
	}
}

PS
{
	#include "common/pixel.hlsl"

    #define STENCIL_ALREADY_SET
    #define BLEND_MODE_ALREADY_SET

	float StencilReadVal < UiGroup( ",0/,0/0" ); Default1( 0 ); Range1( 0, 255 ); >;
	float StencilWriteVal < UiGroup( ",0/,0/0" ); Default1( 0 ); Range1( 0, 255 ); >;
	float StencilRefVal < UiGroup( ",0/,0/0" ); Default1( 0 ); Range1( 0, 255 ); >;

	RenderState( DepthEnable, true );
	RenderState( DepthWriteEnable, false );
	RenderState( DepthFunc, NEVER );

	RenderState( StencilEnable, true );
	RenderState( StencilReadMask, StencilReadVal );
	RenderState( StencilWriteMask, StencilWriteVal );
	RenderState( StencilRef, StencilRefVal );
	RenderState( StencilPassOp, ZERO );
	RenderState( StencilFailOp, KEEP );
	RenderState( StencilDepthFailOp, REPLACE );
	RenderState( StencilFunc, EQUAL );

    PixelOutput MainPs( PixelInput i ) 
    {
        PixelOutput o;
        o.vColor = float4(1, 1, 1, 0 + StencilReadVal - StencilReadVal + StencilWriteVal - StencilWriteVal + StencilRefVal - StencilRefVal);
        return o;
    }
}
