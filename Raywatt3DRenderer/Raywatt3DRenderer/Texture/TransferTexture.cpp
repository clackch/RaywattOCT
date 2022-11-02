#include "TransferTexture.h"

TransferTexture::TransferTexture()
{
	m_pShaderResourceView = nullptr;
}

TransferTexture::~TransferTexture()
{
	Destroy();
}

HRESULT TransferTexture::Initialize(ID3D11Device* pDevice, const std::vector<std::shared_ptr<ControlNode>>& controlNodes)
{
	HRESULT hr = S_OK;

	TransferFunc transfunc;
	for (int i = 0; i < controlNodes.size(); i++)
		transfunc.InsertControlNode(controlNodes[i]);

	BYTE* table;
	int tableSize;
	transfunc.Create1DTextureSourceData(&table, &tableSize);

	D3D11_SUBRESOURCE_DATA subResDesc;
	ZeroMemory(&subResDesc, sizeof(subResDesc));
	subResDesc.pSysMem = table;

	D3D11_TEXTURE1D_DESC tex1dDesc;
	ZeroMemory(&tex1dDesc, sizeof(tex1dDesc));
	tex1dDesc.Width = tableSize;
	tex1dDesc.MipLevels = 1;
	tex1dDesc.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
	tex1dDesc.Usage = D3D11_USAGE_DEFAULT;
	tex1dDesc.BindFlags = D3D11_BIND_SHADER_RESOURCE;
	tex1dDesc.CPUAccessFlags = 0;
	tex1dDesc.MiscFlags = 0;
	tex1dDesc.ArraySize = 1;

	ID3D11Texture1D* pTex;
	pDevice->CreateTexture1D(&tex1dDesc, &subResDesc, &pTex);
	
	D3D11_SHADER_RESOURCE_VIEW_DESC srvDesc;
	ZeroMemory(&srvDesc, sizeof(srvDesc));
	srvDesc.Format = tex1dDesc.Format;
	srvDesc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE1D;
	srvDesc.Texture1D.MipLevels = tex1dDesc.MipLevels;
	srvDesc.Texture1D.MostDetailedMip = 0;

	pDevice->CreateShaderResourceView(pTex, &srvDesc, &m_pShaderResourceView);

	SAFE_RELEASE(pTex);

	return hr;
}

void TransferTexture::Destroy()
{
	SAFE_RELEASE(m_pShaderResourceView);
}