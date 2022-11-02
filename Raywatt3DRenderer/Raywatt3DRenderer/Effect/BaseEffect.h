#pragma once

#include <DXUT.h>
#include "SDKmisc.h"
#include <d3dx11effect.h>

using namespace DirectX;

class BaseEffect
{
public:
	BaseEffect();

	virtual void Destroy();
	
	virtual void Draw(ID3D11DeviceContext* pd3dImmediateContext, ID3D11RenderTargetView* pTargetRTV, ID3D11DepthStencilView* pTargetDSV) = 0;
	virtual HRESULT OnD3D11ResizedSwapChain(ID3D11Device* pd3dDevice, IDXGISwapChain* pSwapChain,
		const DXGI_SURFACE_DESC* pBackBufferSurfaceDesc, void* pUserContext) = 0;

	virtual void Update(float delta);

protected:
	ID3D11InputLayout*	m_pInputLayout = nullptr;
	ID3DX11Effect*		m_pEffect = nullptr;

protected:
	void ClearAndSetRenderTargets(ID3D11DeviceContext* pd3dImmediateContext, ID3D11RenderTargetView* pRTV, ID3D11DepthStencilView* pDSV, XMVECTORF32 clearColor);
};

