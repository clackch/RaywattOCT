float4x4	g_mViewProj;
float4x4	g_mViewProjTex;

Texture2D	g_LastFrameTexture;
Texture3D	g_VolumeTexture;
Texture1D	g_TransFuncTexture;

int			g_iToggle1;
float		g_fSampleRate;
float3		g_vVolumeDimensions;	
float		g_fStep;
int			g_iDepthLength;

DepthStencilState EnableDepth
{
	DepthEnable = TRUE;
	DepthWriteMask = ALL;
	DepthFunc = LESS_EQUAL;
};

DepthStencilState DisableDepth
{
	DepthEnable = FALSE;
	DepthWriteMask = ZERO;
};

RasterizerState FrontCull
{
	CullMode = FRONT;
};

RasterizerState BackCull
{
	CullMode = BACK;
};

SamplerState RaySampler
{
	Filter = MIN_MAG_MIP_POINT;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

SamplerState LastFrameSampler
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

SamplerState TransferSampler {
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

SamplerState VolumeSampler
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = BORDER;
	AddressV = BORDER;
	AddressW = BORDER;

	BorderColor = float4(0, 0, 0, 0);
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
};

struct PS_INPUT
{
	float4 Pos : SV_POSITION;
	float4 Color : COLOR;
	float4 Tex	: TEXCOORD0;
};

PS_INPUT VS_ProxyCube(VS_INPUT input)
{
	PS_INPUT output = (PS_INPUT)0;
	output.Pos = mul(input.Pos, g_mViewProj);
	output.Tex = mul(input.Pos, g_mViewProjTex);
	output.Color = input.Color;
	return output;
}

PS_INPUT VS_ProxyScreenPlane(VS_INPUT input)
{
	PS_INPUT output = (PS_INPUT)0;
	output.Pos = input.Pos;
	output.Color = input.Color;
	output.Tex = input.Color;
	return output;
}

void perspDivision(inout float4 v)
{
	v.xy = v.xy / v.w;
	v.y = 1 - v.y;
}

float4 compositeDVR(float4 curResult, float4 color, float tIncr)
{
	float4 result = curResult;
	color.a = 1.0 - pow(1.0 - color.a, tIncr * 150.0);
	result.rgb = result.rgb + (1.0 - result.a) * color.a * color.rgb;
	result.a = result.a + (1.0 - result.a) * color.a;
	return result;
}

float4 composite(float4 curResult, float4 color)
{
	float4 result = curResult;
	result.rgb += color.rgb * (1 - result.a) * color.a;
	result.a += (1 - result.a) * color.a;
	return result;
}

float4 PS_BackFace(PS_INPUT input) : SV_Target
{
	return input.Color;
}

float4 PS_RayCast(PS_INPUT input) : SV_Target
{
	perspDivision(input.Tex);
	
	float3 back = g_LastFrameTexture.Sample(RaySampler, input.Tex).rgb;
	float3 front = input.Color.rgb;

	float3 rayDirection = back - front;
	float tEnd = length(rayDirection);
	float tIncr = min(
		tEnd, tEnd / (g_fSampleRate * length(rayDirection * 100)));
	float samples = ceil(tEnd / tIncr);
	tIncr = tEnd / samples;
	float t = 0.5f * tIncr;
	rayDirection = normalize(rayDirection);
	
	float4 result = 0;

	int depthLength = g_iDepthLength;

	float3 samplePos;

	const int MAXIMUM_LOOP_COUNT = 10000;
	int loopCount = 0;

	[loop][fastopt] while (t < tEnd && loopCount++ < MAXIMUM_LOOP_COUNT)
	{
		samplePos = front + t * rayDirection;
		float density = g_VolumeTexture.Sample(VolumeSampler, samplePos);
		float4 color = g_TransFuncTexture.Sample(TransferSampler, density);
		if (color.a > 0) 
		{
			result = compositeDVR(result, color, tIncr);
		}
		if (result.a > 0.99)
		{
			t = tEnd;
		}
		else {
			t += tIncr;
		}
	}
	
	result = saturate(result);

	return result;
}

technique11 RaycastVolumeRendering
{
	pass P0
	{
		SetVertexShader(CompileShader(vs_4_0, VS_ProxyCube()));
		SetGeometryShader(NULL);
		SetPixelShader(CompileShader(ps_4_0, PS_BackFace()));

		SetDepthStencilState(DisableDepth, 0);
		SetRasterizerState(FrontCull);
		SetBlendState(NoBlend, float4(0.0f, 0.0f, 0.0f, 0.0f), 0xFFFFFFFF);
	}

	pass P1
	{
		SetVertexShader(CompileShader(vs_4_0, VS_ProxyCube()));
		SetGeometryShader(NULL);
		SetPixelShader(CompileShader(ps_4_0, PS_RayCast()));

		SetDepthStencilState(DisableDepth, 0);
		SetRasterizerState(BackCull);
		SetBlendState(NoBlend, float4(0.0f, 0.0f, 0.0f, 0.0f), 0xFFFFFFFF);
	}
};