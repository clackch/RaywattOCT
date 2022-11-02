#include "ScreenPlaneObject.h"
#include "Utility.h"

ScreenPlaneObject::ScreenPlaneObject()
	:BaseRenderObject()
{

}

ScreenPlaneObject::~ScreenPlaneObject()
{
	Destroy();
}

HRESULT ScreenPlaneObject::Initialize(ID3D11Device* pDevice, const XMFLOAT4& topColor, const XMFLOAT4& downColor)
{
	HRESULT hr = S_OK;
	m_TopColor = topColor; m_DownColor = downColor;
	V_RETURN(BaseRenderObject::Initialize(pDevice));
	return hr;
}

HRESULT ScreenPlaneObject::InitStride(ID3D11Device* pDevice)
{
	HRESULT hr = S_OK;
	m_stride = sizeof(VertexType);
	return hr;
}

HRESULT ScreenPlaneObject::InitVertexBuffer(ID3D11Device* pDevice)
{
	HRESULT hr = S_OK;
	DWORD dwShaderFlags = Utility::GetShaderFlag();

	VertexType vertices[] =
	{
		{ XMFLOAT3(-1.0f, -1.0f, 0.0f), m_DownColor, XMFLOAT2(0.0f, 1.0f) }, // D 153
		{ XMFLOAT3(-1.0f, +1.0f, 0.0f), m_TopColor, XMFLOAT2(0.0f, 0.0f) }, // T 80
		{ XMFLOAT3(+1.0f, +1.0f, 0.0f), m_TopColor, XMFLOAT2(1.0f, 0.0f) }, // T 80
		{ XMFLOAT3(+1.0f, -1.0f, 0.0f), m_DownColor, XMFLOAT2(1.0f, 1.0f) }, // D 153
	};

	auto vertCount = sizeof(vertices) / sizeof(VertexType);

	D3D11_BUFFER_DESC bd;
	ZeroMemory(&bd, sizeof(bd));
	bd.Usage = D3D11_USAGE_DEFAULT;
	bd.ByteWidth = m_stride * vertCount;
	bd.BindFlags = D3D11_BIND_VERTEX_BUFFER;
	bd.CPUAccessFlags = 0;

	D3D11_SUBRESOURCE_DATA InitData;
	ZeroMemory(&InitData, sizeof(InitData));
	InitData.pSysMem = vertices;
	V_RETURN(pDevice->CreateBuffer(&bd, &InitData, &m_pVertexBuffer));
	
	return hr;
}

HRESULT ScreenPlaneObject::InitIndexBuffer(ID3D11Device* pDevice)
{
	HRESULT hr = S_OK;

	DWORD dwShaderFlags = Utility::GetShaderFlag();

	DWORD indices[] =
	{
		0,1,2,
		0,2,3,
	};

	auto indexCount = sizeof(indices) / sizeof(DWORD);

	D3D11_BUFFER_DESC bd;
	ZeroMemory(&bd, sizeof(bd));
	bd.Usage = D3D11_USAGE_DEFAULT;
	bd.ByteWidth = sizeof(DWORD) * indexCount;
	bd.BindFlags = D3D11_BIND_INDEX_BUFFER;
	bd.CPUAccessFlags = 0;
	bd.MiscFlags = 0;

	D3D11_SUBRESOURCE_DATA InitData;
	ZeroMemory(&InitData, sizeof(InitData));
	InitData.pSysMem = indices;
	V_RETURN(pDevice->CreateBuffer(&bd, &InitData, &m_pIndexBuffer));

	return hr;
}