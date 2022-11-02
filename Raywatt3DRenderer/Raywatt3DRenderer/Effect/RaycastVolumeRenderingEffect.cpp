#include "RaycastVolumeRenderingEffect.h"
#include "Utility.h"

#define RENDER_TARGET_BUFFER_SIZE	2

RaycastVolumeRenderingEffect::RaycastVolumeRenderingEffect()
	:BaseEffect()
{
}

RaycastVolumeRenderingEffect::~RaycastVolumeRenderingEffect()
{
	Destroy();
}

HRESULT RaycastVolumeRenderingEffect::Initialize(ID3D11Device* pDevice, std::shared_ptr<VolumeTexture> pVolumeTexture, std::shared_ptr<TransferTexture> pTransferTexture)
{
	HRESULT hr = S_OK;

	m_pVolumeTexture = pVolumeTexture;
	m_pTransferTexture = pTransferTexture;
	
	DWORD dwShaderFlags = Utility::GetShaderFlag();

#if D3D_COMPILER_VERSION >= 46
	WCHAR szShaderPath[MAX_PATH];
	V_RETURN(DXUTFindDXSDKMediaFileCch(szShaderPath, MAX_PATH, L"RaycastVolmueRendering.fx"));
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
	V_RETURN(DXUTCompileFromFile(L"RaycastVolmueRendering.fx", nullptr, "none", "fx_5_0", dwShaderFlags, 0, &pEffectBuffer));
	hr = D3DX11CreateEffectFromMemory(pEffectBuffer->GetBufferPointer(), pEffectBuffer->GetBufferSize(), 0, pd3dDevice, &g_pEffect);
	SAFE_RELEASE(pEffectBuffer);
	if (FAILED(hr))
		return hr;
#endif

	m_pTechnique = m_pEffect->GetTechniqueByName("RaycastVolumeRendering");
	m_pViewProjVar = m_pEffect->GetVariableByName("g_mViewProj")->AsMatrix();
	m_pViewProjTexVar = m_pEffect->GetVariableByName("g_mViewProjTex")->AsMatrix();
	m_pLastFrameTextrueVar = m_pEffect->GetVariableByName("g_LastFrameTexture")->AsShaderResource();
	m_pVolumeTextureVar = m_pEffect->GetVariableByName("g_VolumeTexture")->AsShaderResource();
	m_pTransferTextureVar = m_pEffect->GetVariableByName("g_TransFuncTexture")->AsShaderResource();
	m_pSampleRateVar = m_pEffect->GetVariableByName("g_fSampleRate")->AsScalar();
	m_pVolumeDimensionsVar = m_pEffect->GetVariableByName("g_vVolumeDimensions")->AsScalar();
	m_pToggle1Var = m_pEffect->GetVariableByName("g_iToggle1")->AsScalar();
	m_pStepVar = m_pEffect->GetVariableByName("g_fStep")->AsScalar();
	m_pDepthLengthVar = m_pEffect->GetVariableByName("g_iDepthLength")->AsScalar();

	m_pSampleRateVar->SetFloat(3.f);
	float* dimension;
	auto volumeDataInfo = m_pVolumeTexture->GetVolumeDataInfo();
	volumeDataInfo.GetFloatDimention(&dimension);
	m_pVolumeDimensionsVar->SetFloatArray(dimension, 0, 3);
	float diagonal = volumeDataInfo.GetDiagonal();
	m_pDepthLengthVar->SetInt(static_cast<int>(diagonal));
	m_pStepVar->SetFloat(1.f / diagonal);
	SAFE_DELETE_ARRAY(dimension);

	D3D11_INPUT_ELEMENT_DESC layout[] =
	{
		{ "POSITION",	0, DXGI_FORMAT_R32G32B32_FLOAT,		0, 0,	D3D11_INPUT_PER_VERTEX_DATA, 0 },
		{ "COLOR",		0, DXGI_FORMAT_R32G32B32A32_FLOAT,	0, 12,	D3D11_INPUT_PER_VERTEX_DATA , 0 },
	};

	D3DX11_PASS_DESC PassDesc;
	V_RETURN(m_pTechnique->GetPassByIndex(0)->GetDesc(&PassDesc));
	V_RETURN(pDevice->CreateInputLayout(layout, 2, PassDesc.pIAInputSignature,
		PassDesc.IAInputSignatureSize, &m_pInputLayout));

	for (int i = 0; i < RENDER_TARGET_BUFFER_SIZE; i++)
		m_RenderTargetBuffer.push_back(std::make_shared<RenderTarget>());

	return S_OK;
}

void RaycastVolumeRenderingEffect::Destroy()
{
	BaseEffect::Destroy();
	m_RenderTargetBuffer.clear();
}

void RaycastVolumeRenderingEffect::Update(float delta)
{
	HRESULT hr = S_OK;

	m_pCubeObject->Update(delta);

	XMMATRIX mView = (*m_pCamera).GetViewMatrix();
	XMMATRIX mProj = (*m_pCamera).GetProjMatrix();

	XMMATRIX mViewProj = mView * mProj;
	XMFLOAT4X4 m;
	XMStoreFloat4x4(&m, mViewProj);
	V(m_pViewProjVar->SetMatrix((float*)&m));

	XMMATRIX scaleTexCoord, transTexCoord;
	scaleTexCoord = XMMatrixScaling(0.5f, 0.5f, 0.5f);
	transTexCoord = XMMatrixTranslation(0.5f, 0.5f, 0.5f);
	XMMATRIX mViewProjTex = mViewProj * scaleTexCoord * transTexCoord;
	XMStoreFloat4x4(&m, mViewProjTex);
	V(m_pViewProjTexVar->SetMatrix((float*)&m));
}

RaycastVolumeRenderingEffect* RaycastVolumeRenderingEffect::SetParams(CModelViewerCamera* pCamera, std::shared_ptr<BaseRenderObject> obj)
{
	m_pCamera = pCamera;
	m_pCubeObject = obj;
	return this;
}

void RaycastVolumeRenderingEffect::Draw(ID3D11DeviceContext* pd3dImmediateContext, ID3D11RenderTargetView* pTargetRTV, ID3D11DepthStencilView* pTargetDSV)
{
	const XMVECTORF32 clearColor = { { { 0.000000000f, 0.000000000f, 0.000000000f, 0.000000000f } } };

	 //PASS 1 : Entry point
	ClearAndSetRenderTargets(pd3dImmediateContext, m_RenderTargetBuffer.at(0)->GetRenderTargetView(), pTargetDSV, clearColor);
	m_pCubeObject->SetLayout(pd3dImmediateContext, m_pInputLayout);
	m_pTechnique->GetPassByIndex(0)->Apply(0, pd3dImmediateContext);
	m_pCubeObject->Draw(pd3dImmediateContext);

	// PASS 2 : Raycasting
	ClearAndSetRenderTargets(pd3dImmediateContext, pTargetRTV, pTargetDSV, clearColor);
	m_pLastFrameTextrueVar->SetResource(m_RenderTargetBuffer.at(0)->GetShaderResourceView());
	m_pTransferTextureVar->SetResource(m_pTransferTexture->GetShaderResourceView());
	m_pVolumeTextureVar->SetResource(m_pVolumeTexture->GetShaderResourceView());
	m_pTechnique->GetPassByIndex(1)->Apply(0, pd3dImmediateContext);
	m_pCubeObject->Draw(pd3dImmediateContext);
}

HRESULT RaycastVolumeRenderingEffect::OnD3D11ResizedSwapChain(ID3D11Device* pd3dDevice, IDXGISwapChain* pSwapChain,
	const DXGI_SURFACE_DESC* pBackBufferSurfaceDesc, void* pUserContext)
{
	HRESULT hr = S_OK;

	for (const auto& p : m_RenderTargetBuffer) {
		p->Destroy();
		V_RETURN(p->Initialize(pd3dDevice, pBackBufferSurfaceDesc));
	}
	return hr;
}
