#pragma once

#include "DXUT.h"
#include "CppGetSetMacros.h"
#include "VolumeDataInfo.h"

class VolumeTexture
{
protected:
	ID3D11ShaderResourceView*	m_pVolumeTexRV;
	VolumeDataInfo				m_volumeInfo;

public:
	VolumeTexture();
	virtual ~VolumeTexture();
	
	HRESULT Initialize(ID3D11Device* pDevice, VolumeDataInfo& info, BYTE* pData);
	HRESULT Initialize(ID3D11Device* pDevice, VolumeDataInfo& info);
	void Destroy();

	GET(ID3D11ShaderResourceView*, m_pVolumeTexRV, ShaderResourceView);
	GET(VolumeDataInfo, m_volumeInfo, VolumeDataInfo);
};
