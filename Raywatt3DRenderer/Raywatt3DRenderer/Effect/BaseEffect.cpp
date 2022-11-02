#include "BaseEffect.h"

BaseEffect::BaseEffect()
{
}

void BaseEffect::Destroy()
{
	SAFE_RELEASE(m_pEffect);
	SAFE_RELEASE(m_pInputLayout);
}

void BaseEffect::ClearAndSetRenderTargets(ID3D11DeviceContext* pd3dImmediateContext, ID3D11RenderTargetView* pRTV, ID3D11DepthStencilView* pDSV, XMVECTORF32 clearColor)
{
	if (clearColor != nullptr)
		pd3dImmediateContext->ClearRenderTargetView(pRTV, clearColor);
	pd3dImmediateContext->ClearDepthStencilView(pDSV, D3D11_CLEAR_DEPTH, 1.0f, 0);
	pd3dImmediateContext->OMSetRenderTargets(1, &pRTV, pDSV);
}

void BaseEffect::Update(float delta)
{

}