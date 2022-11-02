#include "VolumeTexture.h"

VolumeTexture::VolumeTexture()
{
}

VolumeTexture::~VolumeTexture()
{
	Destroy();
}

HRESULT VolumeTexture::Initialize(ID3D11Device* pDevice, VolumeDataInfo& info, BYTE* pData)
{
	D3D11_SUBRESOURCE_DATA subRes;
	subRes.pSysMem = pData;
	subRes.SysMemPitch = info.width;
	subRes.SysMemSlicePitch = info.width * info.height;

	D3D11_TEXTURE3D_DESC texDesc;
	texDesc.Depth = info.depth;
	texDesc.Height = info.height;
	texDesc.Width = info.width;
	texDesc.BindFlags = D3D11_BIND_SHADER_RESOURCE;
	texDesc.CPUAccessFlags = 0;
	texDesc.Format = DXGI_FORMAT_R8_UNORM;
	texDesc.MipLevels = 1;
	texDesc.MiscFlags = 0;
	texDesc.Usage = D3D11_USAGE_IMMUTABLE;

	ID3D11Texture3D* pTex;
	pDevice->CreateTexture3D(&texDesc, &subRes, &pTex);

	SAFE_DELETE(pData)

	D3D11_SHADER_RESOURCE_VIEW_DESC srvDesc;
	srvDesc.Format = texDesc.Format;
	srvDesc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE3D;
	srvDesc.Texture3D.MipLevels = texDesc.MipLevels;
	srvDesc.Texture3D.MostDetailedMip = 0;

	pDevice->CreateShaderResourceView(pTex, &srvDesc, &m_pVolumeTexRV);

	SAFE_RELEASE(pTex);

	return S_OK;

}
HRESULT VolumeTexture::Initialize(ID3D11Device* pDevice, VolumeDataInfo& info)
{
	HRESULT hr;
	
	m_volumeInfo = info;

	BYTE* pData;
	V_RETURN(VolumeDataInfo::LoadRawBytes(info, &pData));

	return Initialize(pDevice, info, pData);
}

void VolumeTexture::Destroy()
{
	SAFE_RELEASE(m_pVolumeTexRV);
}