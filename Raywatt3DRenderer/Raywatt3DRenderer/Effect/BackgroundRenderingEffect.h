#pragma once

#include <DXUT.h>
#include "SDKmisc.h"
#include <d3dx11effect.h>
#include "BaseEffect.h"
#include "ScreenPlaneObject.h"

using namespace DirectX;

class BackgroundRenderingEffect : public BaseEffect
{
public:
	BackgroundRenderingEffect();
	~BackgroundRenderingEffect();

	HRESULT Initialize(ID3D11Device* pDevice, const XMFLOAT4& topColor, const XMFLOAT4& downColor);

	virtual void Destroy() override;
	virtual void Draw(ID3D11DeviceContext* pd3dImmediateContext, ID3D11RenderTargetView* pTargetRTV, ID3D11DepthStencilView* pTargetDSV) override;
	virtual HRESULT OnD3D11ResizedSwapChain(ID3D11Device* pd3dDevice, IDXGISwapChain* pSwapChain,
		const DXGI_SURFACE_DESC* pBackBufferSurfaceDesc, void* pUserContext) override;

	BackgroundRenderingEffect* SetParam(ID3D11ShaderResourceView* pLastFrameShaderResourceView);

protected:
	ID3DX11EffectTechnique*							m_pTechnique = nullptr;
	ID3DX11EffectShaderResourceVariable*			m_pLastFrameTextrueVar = nullptr;

	ID3D11ShaderResourceView*						m_pLastFrameShaderResourceView = nullptr;

	std::shared_ptr<ScreenPlaneObject>				m_pScreenPlaneObject = nullptr;
};