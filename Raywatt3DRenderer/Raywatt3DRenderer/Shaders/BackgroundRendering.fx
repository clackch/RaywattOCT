Texture2D	g_LastFrameTexture;

SamplerState LastFrameSampler
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

DepthStencilState DisableDepth
{
	DepthEnable = FALSE;
	DepthWriteMask = ZERO;
};

RasterizerState BackCull
{
	CullMode = BACK;
};

BlendState NoBlend
{
	AlphaToCoverageEnable = FALSE;
	BlendEnable[0] = FALSE;
};

struct VS_INPUT
{
	float4 Pos : POSITION;
	float4 Color : COLOR;
	float2 Tex	: TEXCOORD;
};

struct PS_INPUT
{
	float4 Pos : SV_POSITION;
	float4 Color : COLOR;
	float2 Tex	: TEXCOORD0;
};

PS_INPUT VS_ProxyScreenPlane(VS_INPUT input)
{
	PS_INPUT output = (PS_INPUT)0;
	output.Pos = input.Pos;
	output.Color = input.Color;
	output.Tex = input.Tex;
	return output;
}

float4 PS_DrawLastFrame(PS_INPUT input) : SV_Target
{
	float4 lastFrame = g_LastFrameTexture.Sample(LastFrameSampler, input.Tex);

	float3 color = input.Color;

	float4 result = 0;

	result.rgb = lastFrame.rgb * (lastFrame.a) + color * (1 - lastFrame.a);
	result.a = 1;
	return result;
}

technique11 BackgroundRendering
{
	pass P0
	{
		SetVertexShader(CompileShader(vs_4_0, VS_ProxyScreenPlane()));
		SetGeometryShader(NULL);
		SetPixelShader(CompileShader(ps_4_0, PS_DrawLastFrame()));

		SetDepthStencilState(DisableDepth, 0);
		SetRasterizerState(BackCull);
		SetBlendState(NoBlend, float4(0.0f, 0.0f, 0.0f, 0.0f), 0xFFFFFFFF);
	}
};