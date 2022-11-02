#pragma once

#include <DXUT.h>
#include <d3dx11effect.h>
#include <functional>
#include "CppGetSetMacros.h"
#include "Utility.h"

using namespace DirectX;

class BaseRenderObject {
protected:
	UINT m_IndexCount;
	UINT m_stride;
	ID3D11Device* m_pDevice;
	ID3D11Buffer* m_pVertexBuffer = nullptr;
	ID3D11Buffer* m_pIndexBuffer = nullptr;

public:
	BaseRenderObject();

	GET(ID3D11Buffer*, m_pVertexBuffer, VertexBuffer);
	GET(ID3D11Buffer*, m_pIndexBuffer, IndexBuffer);
	GET(UINT, m_stride, GetStride);
	int GetIndexCount();

	HRESULT Initialize(ID3D11Device* pDevice);
	void Destroy();
	
	void SetLayout(ID3D11DeviceContext* pd3dImmediateContext, ID3D11InputLayout* pInputLayout);
	void Draw(ID3D11DeviceContext* pd3dImmediateContext);
	virtual void Update(float delta);
	virtual void OnKeyboard(UINT nChar, bool bKeyDown, bool bAltDown, void* pUserContext);
	virtual void MsgProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam,
		bool* pbNoFurtherProcessing, void* pUserContext);

protected:
	virtual HRESULT InitStride(ID3D11Device* pDevice) = 0;
	virtual HRESULT InitVertexBuffer(ID3D11Device* pDevice) = 0;
	virtual HRESULT InitIndexBuffer(ID3D11Device* pDevice) = 0;
};


