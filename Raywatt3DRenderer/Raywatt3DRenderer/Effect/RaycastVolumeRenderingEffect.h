#pragma once

#include <DXUT.h>
#include "SDKmisc.h"
#include <d3dx11effect.h>
#include "BaseEffect.h"
#include "BaseRenderObject.h"
#include "DXUTcamera.h"
#include "RenderTarget.h"
#include "VolumeTexture.h"
#include "ScreenPlaneObject.h"
#include "TransferTexture.h"

using namespace DirectX;

class RaycastVolumeRenderingEffect : public BaseEffect
{
public:
	RaycastVolumeRenderingEffect();
	~RaycastVolumeRenderingEffect();

	HRESULT Initialize(ID3D11Device* pDevice, std::shared_ptr<VolumeTexture> pVolumeTexture, std::shared_ptr<TransferTexture> pTransferTexture);
	
	virtual void Destroy() override;
	virtual void Draw(ID3D11DeviceContext* pd3dImmediateContext, ID3D11RenderTargetView* pTargetRTV, ID3D11DepthStencilView* pTargetDSV) override;
	virtual HRESULT OnD3D11ResizedSwapChain(ID3D11Device* pd3dDevice, IDXGISwapChain* pSwapChain,
		const DXGI_SURFACE_DESC* pBackBufferSurfaceDesc, void* pUserContext) override;
	
	RaycastVolumeRenderingEffect* SetParams(CModelViewerCamera* pCamera, std::shared_ptr<BaseRenderObject> obj);
	GET(ID3DX11EffectScalarVariable*, m_pToggle1Var, Toggle1);

	virtual void Update(float delta) override;
	
protected:
	ID3DX11EffectTechnique*							m_pTechnique = nullptr;
	ID3DX11EffectMatrixVariable*					m_pViewProjVar = nullptr;
	ID3DX11EffectMatrixVariable*					m_pViewProjTexVar = nullptr;
	ID3DX11EffectShaderResourceVariable*			m_pLastFrameTextrueVar = nullptr;
	ID3DX11EffectShaderResourceVariable*			m_pVolumeTextureVar = nullptr;
	ID3DX11EffectShaderResourceVariable*			m_pTransferTextureVar = nullptr;
	ID3DX11EffectScalarVariable*					m_pSampleRateVar = nullptr;
	ID3DX11EffectScalarVariable*					m_pVolumeDimensionsVar = nullptr;
	ID3DX11EffectScalarVariable*					m_pToggle1Var = nullptr;
	ID3DX11EffectScalarVariable*					m_pStepVar = nullptr;
	ID3DX11EffectScalarVariable*					m_pDepthLengthVar = nullptr;
	
	std::vector<std::shared_ptr<RenderTarget>>		m_RenderTargetBuffer;
	std::shared_ptr<VolumeTexture>					m_pVolumeTexture;
	std::shared_ptr<TransferTexture>				m_pTransferTexture;
	std::shared_ptr<BaseRenderObject>				m_pCubeObject;
	CModelViewerCamera*								m_pCamera;
};