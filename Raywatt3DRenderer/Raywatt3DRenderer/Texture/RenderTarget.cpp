#include "RenderTarget.h"

RenderTarget::RenderTarget()
{
	m_pRenderTargetView = nullptr;
	m_pShaderResourceView = nullptr;
}

RenderTarget::~RenderTarget()
{
	Destroy();
}

HRESULT RenderTarget::Initialize(ID3D11Device* pDevice, const DXGI_SURFACE_DESC* pBackBufferSurfaceDesc)
{
	HRESULT hr;

	int w = pBackBufferSurfaceDesc->Width;
	int h = pBackBufferSurfaceDesc->Height;

	D3D11_TEXTURE2D_DESC textureDesc;
	ZeroMemory(&textureDesc, sizeof(textureDesc));
	textureDesc.Width = w;
	textureDesc.Height = h;
	textureDesc.MipLevels = 1;
	textureDesc.ArraySize = 1;
	textureDesc.Format = DXGI_FORMAT_R32G32B32A32_FLOAT;
	textureDesc.SampleDesc.Count = 1;
	textureDesc.SampleDesc.Quality = 0;
	textureDesc.Usage = D3D11_USAGE_DEFAULT;
	textureDesc.BindFlags = D3D11_BIND_RENDER_TARGET | D3D11_BIND_SHADER_RESOURCE;
	textureDesc.CPUAccessFlags = 0;
	textureDesc.MiscFlags = 0;

	ID3D11Texture2D* pTex;
	V_RETURN(pDevice->CreateTexture2D(&textureDesc, nullptr, &pTex));

	// --- 

	D3D11_RENDER_TARGET_VIEW_DESC renderTargetViewDesc;

	renderTargetViewDesc.Format = textureDesc.Format;
	renderTargetViewDesc.ViewDimension = D3D11_RTV_DIMENSION_TEXTURE2D;
	renderTargetViewDesc.Texture2D.MipSlice = 0;

	V_RETURN(pDevice->CreateRenderTargetView(pTex, &renderTargetViewDesc, &m_pRenderTargetView));

	D3D11_SHADER_RESOURCE_VIEW_DESC	shaderResourceViewDesc;

	shaderResourceViewDesc.Format = textureDesc.Format;
	shaderResourceViewDesc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE2D;
	shaderResourceViewDesc.Texture2D.MostDetailedMip = 0;
	shaderResourceViewDesc.Texture2D.MipLevels = 1;

	V_RETURN(pDevice->CreateShaderResourceView(pTex, &shaderResourceViewDesc, &m_pShaderResourceView));

	SAFE_RELEASE(pTex);

	return S_OK;
}

void RenderTarget::Destroy() 
{
	SAFE_RELEASE(m_pShaderResourceView);
	SAFE_RELEASE(m_pRenderTargetView);
}