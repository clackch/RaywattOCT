#include "BaseRenderObject.h"

BaseRenderObject::BaseRenderObject()
{
	m_IndexCount = -1;
}

HRESULT BaseRenderObject::Initialize(ID3D11Device* pDevice)
{
	HRESULT hr = S_OK;
	m_pDevice = pDevice;
	V_RETURN(InitStride(pDevice));
	V_RETURN(InitVertexBuffer(pDevice));
	V_RETURN(InitIndexBuffer(pDevice));
	return hr;
}

void BaseRenderObject::Destroy()
{
	SAFE_RELEASE(m_pVertexBuffer);
	SAFE_RELEASE(m_pIndexBuffer);
}

int BaseRenderObject::GetIndexCount()
{
	if (m_IndexCount == -1) {
		D3D11_BUFFER_DESC bd;
		ZeroMemory(&bd, sizeof(bd));
		m_pIndexBuffer->GetDesc(&bd);
		m_IndexCount = bd.ByteWidth / sizeof(DWORD);
	}
	return m_IndexCount;
}

void BaseRenderObject::SetLayout(ID3D11DeviceContext* pd3dImmediateContext, ID3D11InputLayout* pInputLayout)
{
	pd3dImmediateContext->IASetInputLayout(pInputLayout);
	auto vertexBuffer = GetVertexBuffer();
	auto stride = GetGetStride();
	UINT offset = 0;
	pd3dImmediateContext->IASetVertexBuffers(0, 1, &vertexBuffer, &stride, &offset);
	auto indexBuffer = GetIndexBuffer();
	pd3dImmediateContext->IASetIndexBuffer(indexBuffer, DXGI_FORMAT_R32_UINT, 0);
	pd3dImmediateContext->IASetPrimitiveTopology(D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
}

void BaseRenderObject::Draw(ID3D11DeviceContext* pd3dImmediateContext)
{
	auto indexCount = GetIndexCount();
	pd3dImmediateContext->DrawIndexed(indexCount, 0, 0);
}

void BaseRenderObject::Update(float delta)
{

}

void BaseRenderObject::OnKeyboard(UINT nChar, bool bKeyDown, bool bAltDown, void* pUserContext)
{

}

void BaseRenderObject::MsgProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam, bool* pbNoFurtherProcessing, void* pUserContext)
{

}