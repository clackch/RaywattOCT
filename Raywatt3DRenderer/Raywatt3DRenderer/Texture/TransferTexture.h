#pragma once

#include <DXUT.h>
#include "CppGetSetMacros.h"
#include "TransferFunc.h"

using namespace DirectX;

class TransferTexture
{
protected:
	ID3D11ShaderResourceView*	m_pShaderResourceView;

public:
	TransferTexture();
	~TransferTexture();

	HRESULT Initialize(ID3D11Device* pDevice, const std::vector<std::shared_ptr<ControlNode>>& controlNodes);
	void Destroy();
	GET(ID3D11ShaderResourceView*, m_pShaderResourceView, ShaderResourceView);
};

