#pragma once
#include "BaseRenderObject.h"

class ScreenPlaneObject : public BaseRenderObject
{
public:
	struct VertexType
	{
		XMFLOAT3 Pos;
		XMFLOAT4 Color;
		XMFLOAT2 Tex;
	};

	ScreenPlaneObject();
	~ScreenPlaneObject();

	HRESULT Initialize(ID3D11Device* pDevice, const XMFLOAT4& topColor, const XMFLOAT4& downColor);

	virtual HRESULT InitStride(ID3D11Device* pDevice);
	virtual HRESULT InitVertexBuffer(ID3D11Device* pDevice);
	virtual HRESULT InitIndexBuffer(ID3D11Device* pDevice);

protected:
	XMFLOAT4	m_TopColor;
	XMFLOAT4	m_DownColor;
};

