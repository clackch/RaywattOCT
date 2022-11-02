#pragma once

#include <DXUT.h>
#include "CppGetSetMacros.h"

using namespace DirectX;

class RenderTarget
{
protected:
	ID3D11RenderTargetView*		m_pRenderTargetView;
	ID3D11ShaderResourceView*	m_pShaderResourceView;

public:
	RenderTarget();
	~RenderTarget();

	HRESULT Initialize(ID3D11Device* , const DXGI_SURFACE_DESC* pBackBufferSurfaceDesc);
	void Destroy();
	
	GET(ID3D11RenderTargetView*, m_pRenderTargetView, RenderTargetView);
	GET(ID3D11ShaderResourceView*, m_pShaderResourceView, ShaderResourceView);
};

