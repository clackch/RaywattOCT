#include "BackgroundRenderingEffect.h"
#include "Utility.h"

BackgroundRenderingEffect::BackgroundRenderingEffect()
	:BaseEffect()
{

}

BackgroundRenderingEffect::~BackgroundRenderingEffect()
{
	Destroy();
}

HRESULT BackgroundRenderingEffect::Initialize(ID3D11Device* pDevice, const XMFLOAT4& topColor, const XMFLOAT4& downColor)
{
	HRESULT hr = S_OK;

	DWORD dwShaderFlags = Utility::GetShaderFlag();

#if D3D_COMPILER_VERSION >= 46
	WCHAR szShaderPath[MAX_PATH];
	V_RETURN(DXUTFindDXSDKMediaFileCch(szShaderPath, MAX_PATH, L"BackgroundRendering.fx"));
	ID3DBlob* pErrorBlob = nullptr;
	hr = D3DX11CompileEffectFromFile(szShaderPath, nullptr, D3D_COMPILE_STANDARD_FILE_INCLUDE, dwShaderFlags, 0, pDevice, &m_pEffect, &pErrorBlob);
	if (pErrorBlob)
	{
		OutputDebugStringA(reinterpret_cast<const char*>(pErrorBlob->GetBufferPointer()));
		pErrorBlob->Release();
	}
	if (FAILED(hr))
		return hr;
#else
	ID3DBlob* pEffectBuffer = nullptr;
	V_RETURN(DXUTCompileFromFile(L"BackgroundRendering.fx", nullptr, "none", "fx_5_0", dwShaderFlags, 0, &pEffectBuffer));
	hr = D3DX11CreateEffectFromMemory(pEffectBuffer->GetBufferPointer(), pEffectBuffer->GetBufferSize(), 0, pd3dDevice, &g_pEffect);
	SAFE_RELEASE(pEffectBuffer);
	if (FAILED(hr))
		return hr;
#endif
	
	m_pTechnique = m_pEffect->GetTechniqueByName("BackgroundRendering");
	m_pLastFrameTextrueVar = m_pEffect->GetVariableByName("g_LastFrameTexture")->AsShaderResource();

	D3D11_INPUT_ELEMENT_DESC layout[] =
	{
		{ "POSITION",	0, DXGI_FORMAT_R32G32B32_FLOAT,		0, 0,	D3D11_INPUT_PER_VERTEX_DATA, 0 },
		{ "COLOR",		0, DXGI_FORMAT_R32G32B32A32_FLOAT,	0, 12,	D3D11_INPUT_PER_VERTEX_DATA , 0 },
		{ "TEXCOORD",	0, DXGI_FORMAT_R32G32_FLOAT,		0, 28,	D3D11_INPUT_PER_VERTEX_DATA , 0 },
	};

	D3DX11_PASS_DESC PassDesc;
	V_RETURN(m_pTechnique->GetPassByIndex(0)->GetDesc(&PassDesc));
	V_RETURN(pDevice->CreateInputLayout(layout, 3, PassDesc.pIAInputSignature,
		PassDesc.IAInputSignatureSize, &m_pInputLayout));
	
	m_pScreenPlaneObject = std::make_shared<ScreenPlaneObject>();
	m_pScreenPlaneObject->Initialize(pDevice, topColor, downColor);

	return hr;
}

void BackgroundRenderingEffect::Destroy()
{
	BaseEffect::Destroy();
	SAFE_RELEASE(m_pEffect);
	SAFE_RELEASE(m_pInputLayout);
	m_pScreenPlaneObject.reset();
}

BackgroundRenderingEffect* BackgroundRenderingEffect::SetParam(ID3D11ShaderResourceView* pLastFrameShaderResourceView)
{
	m_pLastFrameShaderResourceView = pLastFrameShaderResourceView;
	return this;
}

void BackgroundRenderingEffect::Draw(ID3D11DeviceContext* pd3dImmediateContext, ID3D11RenderTargetView* pTargetRTV, ID3D11DepthStencilView* pTargetDSV)
{
	const XMVECTORF32 clearColor = { { { 0.000000000f, 0.000000000f, 0.000000000f, 0.000000000f } } };
	ClearAndSetRenderTargets(pd3dImmediateContext, pTargetRTV, pTargetDSV, clearColor);
	m_pScreenPlaneObject->SetLayout(pd3dImmediateContext, m_pInputLayout);
	m_pLastFrameTextrueVar->SetResource(m_pLastFrameShaderResourceView);
	m_pTechnique->GetPassByIndex(0)->Apply(0, pd3dImmediateContext);
	m_pScreenPlaneObject->Draw(pd3dImmediateContext);
}

HRESULT BackgroundRenderingEffect::OnD3D11ResizedSwapChain(ID3D11Device* pd3dDevice, IDXGISwapChain* pSwapChain,
	const DXGI_SURFACE_DESC* pBackBufferSurfaceDesc, void* pUserContext)
{
	HRESULT hr = S_OK;
	return hr;
}