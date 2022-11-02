#include "CubeObject.h"
#include "SDKmisc.h"

CubeObject::CubeObject()
	:BaseRenderObject(),
	m_Pos(0, 0, 0),
	m_RotAngle(0, 0, 0),
	m_fDeltaTime(0)
{

}

CubeObject::~CubeObject()
{
	Destroy();
}

HRESULT CubeObject::InitStride(ID3D11Device* pDevice)
{
	m_stride = sizeof(VertexType);
	return S_OK;
}

HRESULT CubeObject::Initialize(ID3D11Device* pDevice, VolumeDataInfo& volmeDataInfo, CModelViewerCamera* camera)
{
	HRESULT hr = S_OK;
	m_Camera = camera;

	float* dimention;
	volmeDataInfo.GetFloatDimention(&dimention);

	float w = dimention[0], h = dimention[1], d = dimention[2];
	float xRatio = 1.f, yRatio = 1.f, zRatio = (dimention[2] - 1) * 0.2f / 9.5f;

	m_Ratio.x = xRatio;
	m_Ratio.y = yRatio;
	m_Ratio.z = zRatio;

	m_OriginPos[0] = XMFLOAT3(-1.0f * xRatio, -1.0f * yRatio, -1.0f * zRatio); // - - -
	m_OriginPos[1] = XMFLOAT3(+1.0f * xRatio, -1.0f * yRatio, -1.0f * zRatio); // + - -
	m_OriginPos[2] = XMFLOAT3(+1.0f * xRatio, +1.0f * yRatio, -1.0f * zRatio); // + + -
	m_OriginPos[3] = XMFLOAT3(-1.0f * xRatio, +1.0f * yRatio, -1.0f * zRatio); // - + -
	
	m_OriginPos[4] = XMFLOAT3(-1.0f * xRatio, -1.0f * yRatio, +1.0f * zRatio); // - - +
	m_OriginPos[5] = XMFLOAT3(+1.0f * xRatio, -1.0f * yRatio, +1.0f * zRatio); // + - +
	m_OriginPos[6] = XMFLOAT3(+1.0f * xRatio, +1.0f * yRatio, +1.0f * zRatio); // + + +
	m_OriginPos[7] = XMFLOAT3(-1.0f * xRatio, +1.0f * yRatio, +1.0f * zRatio); // - + +

	// ---

	m_Edges[0].BegPos = m_OriginPos[0];
	m_Edges[0].EndPos = m_OriginPos[3];
	
	m_Edges[1].BegPos = m_OriginPos[3];
	m_Edges[1].EndPos = m_OriginPos[2];
	
	m_Edges[2].BegPos = m_OriginPos[2];
	m_Edges[2].EndPos = m_OriginPos[1];

	m_Edges[3].BegPos = m_OriginPos[1];
	m_Edges[3].EndPos = m_OriginPos[0];

	m_Edges[4].BegPos = m_OriginPos[7];
	m_Edges[4].EndPos = m_OriginPos[4];

	m_Edges[5].BegPos = m_OriginPos[4];
	m_Edges[5].EndPos = m_OriginPos[5];

	m_Edges[6].BegPos = m_OriginPos[5];
	m_Edges[6].EndPos = m_OriginPos[6];

	m_Edges[7].BegPos = m_OriginPos[6];
	m_Edges[7].EndPos = m_OriginPos[7];
	
	m_Edges[8].BegPos = m_OriginPos[2];
	m_Edges[8].EndPos = m_OriginPos[6];
	
	m_Edges[9].BegPos = m_OriginPos[5];
	m_Edges[9].EndPos = m_OriginPos[1];

	m_Edges[10].BegPos = m_OriginPos[0];
	m_Edges[10].EndPos = m_OriginPos[4];

	m_Edges[11].BegPos = m_OriginPos[7];
	m_Edges[11].EndPos = m_OriginPos[3];

	// ---

	m_BoxVertices[0] = { m_OriginPos[0], XMFLOAT4(0.0f, 0.0f, 0.0f, 1.0f) }; // 0 0 0
	m_BoxVertices[1] = { m_OriginPos[1], XMFLOAT4(1.0f, 0.0f, 0.0f, 1.0f) }; // 1 0 0
	m_BoxVertices[2] = { m_OriginPos[2], XMFLOAT4(1.0f, 1.0f, 0.0f, 1.0f) }; // 1 1 0
	m_BoxVertices[3] = { m_OriginPos[3], XMFLOAT4(0.0f, 1.0f, 0.0f, 1.0f) }; // 0 1 0

	m_BoxVertices[4] = { m_OriginPos[4], XMFLOAT4(0.0f, 0.0f, 1.0f, 1.0f) }; // 0 0 1
	m_BoxVertices[5] = { m_OriginPos[5], XMFLOAT4(1.0f, 0.0f, 1.0f, 1.0f) }; // 1 0 1
	m_BoxVertices[6] = { m_OriginPos[6], XMFLOAT4(1.0f, 1.0f, 1.0f, 1.0f) }; // 1 1 1
	m_BoxVertices[7] = { m_OriginPos[7], XMFLOAT4(0.0f, 1.0f, 1.0f, 1.0f) }; // 0 1 1
	
	m_pDevice = pDevice;

	InitStride(m_pDevice);

	m_mRT = m_LocalRotMatrix = m_LocalTransMatrix = XMMatrixIdentity();

	// [22.08.22] jeansu added.
	V_RETURN(BaseRenderObject::Initialize(pDevice));

	return hr;
}

HRESULT CubeObject::InitVertexBuffer(ID3D11Device* pDevice)
{
	HRESULT hr = S_OK;
	DWORD dwShaderFlags = Utility::GetShaderFlag();
	
	VertexType newVertices[64];

	int vertexCount = m_NewPositions.size();
	for (int i = 0; i < vertexCount; i++)
	{
		newVertices[i].Pos = m_NewPositions[i];
		newVertices[i].Color = m_NewColors[i];
	}

	D3D11_BUFFER_DESC bd;
	ZeroMemory(&bd, sizeof(bd));
	bd.Usage = D3D11_USAGE_DEFAULT;
	bd.ByteWidth = m_stride * 64;
	bd.BindFlags = D3D11_BIND_VERTEX_BUFFER;
	bd.CPUAccessFlags = 0;
	
	D3D11_SUBRESOURCE_DATA InitData;
	ZeroMemory(&InitData, sizeof(InitData));
	InitData.pSysMem = newVertices;
	SAFE_RELEASE(m_pVertexBuffer);
	V_RETURN(pDevice->CreateBuffer(&bd, &InitData, &m_pVertexBuffer));

	return hr;
}

HRESULT CubeObject::InitIndexBuffer(ID3D11Device* pDevice)
{
	HRESULT hr = S_OK;
	DWORD dwShaderFlags = Utility::GetShaderFlag();

	DWORD indices[64];
	int indexCount = m_NewIndexes.size();
	for (int i = 0; i < indexCount; i++)
	{
		indices[i] = m_NewIndexes[i];
	}

	D3D11_BUFFER_DESC bd;
	ZeroMemory(&bd, sizeof(bd));
	bd.Usage = D3D11_USAGE_DEFAULT;
	bd.ByteWidth = sizeof(DWORD) * 64;
	bd.BindFlags = D3D11_BIND_INDEX_BUFFER;
	bd.CPUAccessFlags = 0;
	bd.MiscFlags = 0;
	
	D3D11_SUBRESOURCE_DATA InitData;
	ZeroMemory(&InitData, sizeof(InitData));
	InitData.pSysMem = indices;
	SAFE_RELEASE(m_pIndexBuffer);
	V_RETURN(pDevice->CreateBuffer(&bd, &InitData, &m_pIndexBuffer));

	return hr;
}

void CubeObject::Update(float delta)
{
	m_fDeltaTime = delta;
	
	m_mRT = XMMatrixIdentity();
	m_mRT = XMMatrixMultiply(m_mRT, m_LocalRotMatrix);
	m_mRT = XMMatrixMultiply(m_mRT, m_Arcball.GetTranslationMatrix());
	m_mRT = XMMatrixMultiply(m_mRT, m_LocalTransMatrix);

	for (int i = 0; i < 8; i++)
	{
		XMVECTOR t = XMVector3TransformCoord(XMLoadFloat3(&m_OriginPos[i]), m_mRT);
		XMStoreFloat3(&m_BoxVertices[i].Pos, t);
	}

	UpdateIntersectionPlane();
	
	MakeNewVertexInfo();

	InitVertexBuffer(m_pDevice);
	InitIndexBuffer(m_pDevice);
}

void CubeObject::DeltaRotate(XMFLOAT3 rotAngle)
{
	XMMATRIX xRotMat = XMMatrixRotationX(rotAngle.x * m_fDeltaTime);
	XMMATRIX yRotMat = XMMatrixRotationY(rotAngle.y * m_fDeltaTime);
	XMMATRIX zRotMat = XMMatrixRotationZ(rotAngle.z * m_fDeltaTime);
	XMMATRIX rotMat = xRotMat * yRotMat * zRotMat;
	m_LocalRotMatrix = m_Arcball.GetRotationMatrix();
	m_LocalRotMatrix = XMMatrixMultiply(m_LocalRotMatrix, rotMat);
	m_Arcball.SetQuatNow(XMQuaternionRotationMatrix(m_LocalRotMatrix));
}

void CubeObject::DeltaTranslate(XMFLOAT3 inc)
{
	XMMATRIX tranMat = XMMatrixTranslation(inc.x * m_fDeltaTime, inc.y * m_fDeltaTime, inc.z * m_fDeltaTime);
	m_LocalTransMatrix = XMMatrixMultiply(m_LocalTransMatrix, tranMat);
}

void CubeObject::Translate(XMFLOAT3 dist)
{
	XMMATRIX tranMat = XMMatrixTranslation(dist.x, dist.y, dist.z);
	m_LocalTransMatrix = XMMatrixMultiply(m_LocalTransMatrix, tranMat);
}

void CubeObject::MsgProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam,
	bool* pbNoFurtherProcessing, void* pUserContext)
{
	int zDelta = GET_WHEEL_DELTA_WPARAM(wParam);
	m_Arcball.HandleMessages(hWnd, uMsg, wParam, lParam);

	if (m_Arcball.IsBeingDragged())
		m_LocalRotMatrix = m_Arcball.GetRotationMatrix();

	switch (uMsg)
	{
	case WM_MOUSEWHEEL:
		XMMATRIX tranMat = XMMatrixTranslation(0, 0, -zDelta * m_fDeltaTime);
		m_LocalTransMatrix = XMMatrixMultiply(m_LocalTransMatrix, tranMat);
		break;
	}
}

void CubeObject::OnKeyboard(UINT nChar, bool bKeyDown, bool bAltDown, void* pUserContext)
{
	if (bKeyDown)
	{
		switch (nChar)
		{
		case 'W':
		case 'w':
			DeltaTranslate(XMFLOAT3(0, 0, -10.f));
			break;
		case 'S':
		case 's':
			DeltaTranslate(XMFLOAT3(0, 0, 10.f));
			break;
		case 38: // UP
			DeltaRotate(XMFLOAT3(10.f, 0, 0));
			break;
		case 40: // DOWN
			DeltaRotate(XMFLOAT3(-10.f, 0, 0));
			break;
		case 37: // LEFT
			DeltaRotate(XMFLOAT3(0, 10.f, 0));
			break;
		case 39: // RIGHT
			DeltaRotate(XMFLOAT3(0, -10.f, 0));
			break;
		default:
			break;
		}
	}
}


float CubeObject::GetClockWiseValue(const XMVECTOR& v1, const XMVECTOR& v2, const XMVECTOR& v3)
{
	XMFLOAT3 p1, p2, p3;
	XMStoreFloat3(&p1, v1);
	XMStoreFloat3(&p2, v2);
	XMStoreFloat3(&p3, v3);
	return (p2.x - p1.x) * (p3.y - p2.y) - (p2.y - p1.y) * (p3.x - p2.x);
}

void CubeObject::MakeNewVertexInfo()
{
	m_NewPositions.clear();
	m_NewColors.clear();
	m_NewIndexes.clear();

	// -- front
	m_NewPositions.push_back(m_BoxVertices[0].Pos);
	m_NewColors.push_back(m_BoxVertices[0].Color);
	m_NewPositions.push_back(m_BoxVertices[3].Pos);
	m_NewColors.push_back(m_BoxVertices[3].Color);
	m_NewPositions.push_back(m_BoxVertices[2].Pos);
	m_NewColors.push_back(m_BoxVertices[2].Color);
	m_NewIndexes.push_back(0);
	m_NewIndexes.push_back(1);
	m_NewIndexes.push_back(2);

	m_NewPositions.push_back(m_BoxVertices[2].Pos);
	m_NewColors.push_back(m_BoxVertices[2].Color);
	m_NewPositions.push_back(m_BoxVertices[1].Pos);
	m_NewColors.push_back(m_BoxVertices[1].Color);
	m_NewPositions.push_back(m_BoxVertices[0].Pos);
	m_NewColors.push_back(m_BoxVertices[0].Color);
	m_NewIndexes.push_back(0 + 3);
	m_NewIndexes.push_back(1 + 3);
	m_NewIndexes.push_back(2 + 3);

	// -- right
	m_NewPositions.push_back(m_BoxVertices[1].Pos);
	m_NewColors.push_back(m_BoxVertices[1].Color);
	m_NewPositions.push_back(m_BoxVertices[2].Pos);
	m_NewColors.push_back(m_BoxVertices[2].Color);
	m_NewPositions.push_back(m_BoxVertices[6].Pos);
	m_NewColors.push_back(m_BoxVertices[6].Color);
	m_NewIndexes.push_back(0 + 3 * 2);
	m_NewIndexes.push_back(1 + 3 * 2);
	m_NewIndexes.push_back(2 + 3 * 2);

	m_NewPositions.push_back(m_BoxVertices[6].Pos);
	m_NewColors.push_back(m_BoxVertices[6].Color);
	m_NewPositions.push_back(m_BoxVertices[5].Pos);
	m_NewColors.push_back(m_BoxVertices[5].Color);
	m_NewPositions.push_back(m_BoxVertices[1].Pos);
	m_NewColors.push_back(m_BoxVertices[1].Color);
	m_NewIndexes.push_back(0 + 3 * 3);
	m_NewIndexes.push_back(1 + 3 * 3);
	m_NewIndexes.push_back(2 + 3 * 3);

	// -- back 
	m_NewPositions.push_back(m_BoxVertices[5].Pos);
	m_NewColors.push_back(m_BoxVertices[5].Color);
	m_NewPositions.push_back(m_BoxVertices[6].Pos);
	m_NewColors.push_back(m_BoxVertices[6].Color);
	m_NewPositions.push_back(m_BoxVertices[7].Pos);
	m_NewColors.push_back(m_BoxVertices[7].Color);
	m_NewIndexes.push_back(0 + 3 * 4);
	m_NewIndexes.push_back(1 + 3 * 4);
	m_NewIndexes.push_back(2 + 3 * 4);

	m_NewPositions.push_back(m_BoxVertices[7].Pos);
	m_NewColors.push_back(m_BoxVertices[7].Color);
	m_NewPositions.push_back(m_BoxVertices[4].Pos);
	m_NewColors.push_back(m_BoxVertices[4].Color);
	m_NewPositions.push_back(m_BoxVertices[5].Pos);
	m_NewColors.push_back(m_BoxVertices[5].Color);
	m_NewIndexes.push_back(0 + 3 * 5);
	m_NewIndexes.push_back(1 + 3 * 5);
	m_NewIndexes.push_back(2 + 3 * 5);

	// -- left
	m_NewPositions.push_back(m_BoxVertices[4].Pos);
	m_NewColors.push_back(m_BoxVertices[4].Color);
	m_NewPositions.push_back(m_BoxVertices[7].Pos);
	m_NewColors.push_back(m_BoxVertices[7].Color);
	m_NewPositions.push_back(m_BoxVertices[3].Pos);
	m_NewColors.push_back(m_BoxVertices[3].Color);
	m_NewIndexes.push_back(0 + 3 * 6);
	m_NewIndexes.push_back(1 + 3 * 6);
	m_NewIndexes.push_back(2 + 3 * 6);

	m_NewPositions.push_back(m_BoxVertices[3].Pos);
	m_NewColors.push_back(m_BoxVertices[3].Color);
	m_NewPositions.push_back(m_BoxVertices[0].Pos);
	m_NewColors.push_back(m_BoxVertices[0].Color);
	m_NewPositions.push_back(m_BoxVertices[4].Pos);
	m_NewColors.push_back(m_BoxVertices[4].Color);
	m_NewIndexes.push_back(0 + 3 * 7);
	m_NewIndexes.push_back(1 + 3 * 7);
	m_NewIndexes.push_back(2 + 3 * 7);

	// -- top
	m_NewPositions.push_back(m_BoxVertices[3].Pos);
	m_NewColors.push_back(m_BoxVertices[3].Color);
	m_NewPositions.push_back(m_BoxVertices[7].Pos);
	m_NewColors.push_back(m_BoxVertices[7].Color);
	m_NewPositions.push_back(m_BoxVertices[6].Pos);
	m_NewColors.push_back(m_BoxVertices[6].Color);
	m_NewIndexes.push_back(0 + 3 * 8);
	m_NewIndexes.push_back(1 + 3 * 8);
	m_NewIndexes.push_back(2 + 3 * 8);

	m_NewPositions.push_back(m_BoxVertices[6].Pos);
	m_NewColors.push_back(m_BoxVertices[6].Color);
	m_NewPositions.push_back(m_BoxVertices[2].Pos);
	m_NewColors.push_back(m_BoxVertices[2].Color);
	m_NewPositions.push_back(m_BoxVertices[3].Pos);
	m_NewColors.push_back(m_BoxVertices[3].Color);
	m_NewIndexes.push_back(0 + 3 * 9);
	m_NewIndexes.push_back(1 + 3 * 9);
	m_NewIndexes.push_back(2 + 3 * 9);

	// -- down
	m_NewPositions.push_back(m_BoxVertices[4].Pos);
	m_NewColors.push_back(m_BoxVertices[4].Color);
	m_NewPositions.push_back(m_BoxVertices[0].Pos);
	m_NewColors.push_back(m_BoxVertices[0].Color);
	m_NewPositions.push_back(m_BoxVertices[1].Pos);
	m_NewColors.push_back(m_BoxVertices[1].Color);
	m_NewIndexes.push_back(0 + 3 * 10);
	m_NewIndexes.push_back(1 + 3 * 10);
	m_NewIndexes.push_back(2 + 3 * 10);

	m_NewPositions.push_back(m_BoxVertices[1].Pos);
	m_NewColors.push_back(m_BoxVertices[1].Color);
	m_NewPositions.push_back(m_BoxVertices[5].Pos);
	m_NewColors.push_back(m_BoxVertices[5].Color);
	m_NewPositions.push_back(m_BoxVertices[4].Pos);
	m_NewColors.push_back(m_BoxVertices[4].Color);
	m_NewIndexes.push_back(0 + 3 * 11);
	m_NewIndexes.push_back(1 + 3 * 11);
	m_NewIndexes.push_back(2 + 3 * 11);

	// -- else

	std::vector<XMVECTOR> addElsePositions;
	std::vector<XMVECTOR> addElseColors;

	if (m_InterPositions.size() > 0) {
		XMVECTOR cv = XMVectorSet(0, 0, 0, 0);
		for (int i = 0; i < m_InterPositions.size(); i++)
		{
			XMVECTOR interPositionVector = XMLoadFloat3(&m_InterPositions[i]);
			cv += interPositionVector;
		}

		cv = cv / m_InterPositions.size();

		int minIdx = 0;
		while (m_InterPositions.size() > 0) 
		{
			XMVECTOR v1 = XMLoadFloat3(&m_InterPositions[minIdx]);

			addElsePositions.push_back(v1);
			m_InterPositions.erase(m_InterPositions.begin() + minIdx);

			XMVECTOR color = XMLoadFloat4(&m_InterColors[minIdx]);
			addElseColors.push_back(color);
			m_InterColors.erase(m_InterColors.begin() + minIdx);

			float maxDot = -3.402823466e+38F;

			for (int j = 0; j < m_InterColors.size(); j++) {
				XMVECTOR v2 = XMLoadFloat3(&m_InterPositions[j]);

				float clockWise = GetClockWiseValue(v1, cv, v2);
				if (clockWise > 0) 
				{ // dir > 0 clock
					XMVECTOR dir1Vector = v1 - cv;
					XMVECTOR dir2Vector = v2 - cv;
					
					XMFLOAT3 dir1;
					XMFLOAT3 dir2;
					XMStoreFloat3(&dir1, dir1Vector);
					XMStoreFloat3(&dir2, dir2Vector);

					float dot = (dir1.x * dir2.x) + (dir1.y * dir2.y);
					if (dot > maxDot) 
					{
						minIdx = j;
						maxDot = dot;
					}
				}
			}
		}
	}

	std::vector<int> addElseIdxVector;
	int strIdx = 0;
	if (addElsePositions.size() > 0)
		for (int i = 0; i < addElsePositions.size() - 2; i++)
		{
			addElseIdxVector.push_back(strIdx);
			addElseIdxVector.push_back(strIdx + 1 + i);
			addElseIdxVector.push_back(strIdx + 2 + i);
		}

	if (addElsePositions.size() > 0)
		for (int i = 0; i < addElsePositions.size(); i++)
		{
			XMFLOAT3 pos;
			XMStoreFloat3(&pos, addElsePositions.at(i));
			m_NewPositions.push_back(pos);
			XMFLOAT4 color;
			XMStoreFloat4(&color,addElseColors.at(i));
			m_NewColors.push_back(color);
		}

	int offSize = m_NewIndexes.size();
	if (addElseIdxVector.size() > 0)
		for (int i = 0; i < addElseIdxVector.size(); i++)
		{
			m_NewIndexes.push_back(addElseIdxVector.at(i) + offSize);
		}
}

void CubeObject::UpdateIntersectionPlane()
{
	m_Edges[0].BegPos = m_BoxVertices[0].Pos;
	m_Edges[0].EndPos = m_BoxVertices[3].Pos;
	m_Edges[0].BegColor = m_BoxVertices[0].Color;
	m_Edges[0].EndColor = m_BoxVertices[3].Color;

	m_Edges[1].BegPos = m_BoxVertices[3].Pos;
	m_Edges[1].EndPos = m_BoxVertices[2].Pos;
	m_Edges[1].BegColor = m_BoxVertices[3].Color;
	m_Edges[1].EndColor = m_BoxVertices[2].Color;

	m_Edges[2].BegPos = m_BoxVertices[2].Pos;
	m_Edges[2].EndPos = m_BoxVertices[1].Pos;
	m_Edges[2].BegColor = m_BoxVertices[2].Color;
	m_Edges[2].EndColor = m_BoxVertices[1].Color;

	m_Edges[3].BegPos = m_BoxVertices[1].Pos;
	m_Edges[3].EndPos = m_BoxVertices[0].Pos;
	m_Edges[3].BegColor = m_BoxVertices[1].Color;
	m_Edges[3].EndColor = m_BoxVertices[0].Color;

	m_Edges[4].BegPos = m_BoxVertices[7].Pos;
	m_Edges[4].EndPos = m_BoxVertices[4].Pos;
	m_Edges[4].BegColor = m_BoxVertices[7].Color;
	m_Edges[4].EndColor = m_BoxVertices[4].Color;

	m_Edges[5].BegPos = m_BoxVertices[4].Pos;
	m_Edges[5].EndPos = m_BoxVertices[5].Pos;
	m_Edges[5].BegColor = m_BoxVertices[4].Color;
	m_Edges[5].EndColor = m_BoxVertices[5].Color;

	m_Edges[6].BegPos = m_BoxVertices[5].Pos;
	m_Edges[6].EndPos = m_BoxVertices[6].Pos;
	m_Edges[6].BegColor = m_BoxVertices[5].Color;
	m_Edges[6].EndColor = m_BoxVertices[6].Color;

	m_Edges[7].BegPos = m_BoxVertices[6].Pos;
	m_Edges[7].EndPos = m_BoxVertices[7].Pos;
	m_Edges[7].BegColor = m_BoxVertices[6].Color;
	m_Edges[7].EndColor = m_BoxVertices[7].Color;

	m_Edges[8].BegPos = m_BoxVertices[2].Pos;
	m_Edges[8].EndPos = m_BoxVertices[6].Pos;
	m_Edges[8].BegColor = m_BoxVertices[2].Color;
	m_Edges[8].EndColor = m_BoxVertices[6].Color;

	m_Edges[9].BegPos = m_BoxVertices[5].Pos;
	m_Edges[9].EndPos = m_BoxVertices[1].Pos;
	m_Edges[9].BegColor = m_BoxVertices[5].Color;
	m_Edges[9].EndColor = m_BoxVertices[1].Color;

	m_Edges[10].BegPos = m_BoxVertices[0].Pos;
	m_Edges[10].EndPos = m_BoxVertices[4].Pos;
	m_Edges[10].BegColor = m_BoxVertices[0].Color;
	m_Edges[10].EndColor = m_BoxVertices[4].Color;

	m_Edges[11].BegPos = m_BoxVertices[7].Pos;
	m_Edges[11].EndPos = m_BoxVertices[3].Pos;
	m_Edges[11].BegColor = m_BoxVertices[7].Color;
	m_Edges[11].EndColor = m_BoxVertices[3].Color;

	m_InterPositions.clear();
	m_InterColors.clear();

	const float EPSILON = 0.000001f;

	XMFLOAT3 t = XMFLOAT3(0, 0, m_Camera->GetNearClip() + 0.000001f);
	XMVECTOR centerVector = XMLoadFloat3(&t);
	t = XMFLOAT3(0.0f, 0.0f, 1.f);
	XMVECTOR normalVector = XMLoadFloat3(&t);

	for (int i = 0; i < EDGE_COUNT; i++)
	{
		XMVECTOR begPosVector = XMLoadFloat3(&m_Edges[i].BegPos);
		XMVECTOR endPosVector = XMLoadFloat3(&m_Edges[i].EndPos);
		XMVECTOR subPosVector = begPosVector - centerVector;
		XMVECTOR begColorVector = XMLoadFloat4(&m_Edges[i].BegColor);
		XMVECTOR endColorVector = XMLoadFloat4(&m_Edges[i].EndColor);

		float begPosSign = XMVectorGetX(XMVector3Dot(subPosVector, normalVector));
		subPosVector = endPosVector - centerVector;
		float endPosSign = XMVectorGetX(XMVector3Dot(subPosVector, normalVector));
		if (begPosSign * endPosSign >= 0)
		{
			continue;
		}

		XMVECTOR dirVector = XMVector3Normalize(endPosVector - begPosVector);
		subPosVector = begPosVector - centerVector;
		float dividen = -XMVectorGetX(XMVector3Dot(subPosVector, normalVector));
		float divisor = XMVectorGetX(XMVector3Dot(dirVector, normalVector));
		float t = 0;

		if (divisor < EPSILON && divisor > -EPSILON) {
			continue;
		}
		else 
		{
			t = dividen / divisor;
		}

		XMVECTOR interPosVector = (dirVector * t) + begPosVector;
		XMFLOAT3 interPos;
		XMStoreFloat3(&interPos, interPosVector);

		float rx = m_Edges[i].EndPos.x >  m_Edges[i].BegPos.x ? m_Edges[i].EndPos.x : m_Edges[i].BegPos.x;
		float lx = m_Edges[i].EndPos.x <= m_Edges[i].BegPos.x ? m_Edges[i].EndPos.x : m_Edges[i].BegPos.x;
		float uy = m_Edges[i].EndPos.y >  m_Edges[i].BegPos.y ? m_Edges[i].EndPos.y : m_Edges[i].BegPos.y;
		float dy = m_Edges[i].EndPos.y <= m_Edges[i].BegPos.y ? m_Edges[i].EndPos.y : m_Edges[i].BegPos.y;

		if ((interPos.x <= rx && interPos.x >= lx && interPos.y <= uy && interPos.y >= dy))
		{
			interPos.z = XMVectorGetZ(centerVector);
			m_InterPositions.push_back(interPos);
		}
		else
		{
			continue;
		}

		if (begPosSign > 0)
		{
			subPosVector = endPosVector - begPosVector;
			float orinLength = XMVectorGetX(XMVector3Length(subPosVector));

			m_Edges[i].BegPos = interPos;
			begPosVector = XMLoadFloat3(&m_Edges[i].BegPos);

			subPosVector = endPosVector - begPosVector;
			float cutLength = XMVectorGetX(XMVector3Length(subPosVector));

			float rate = !(orinLength < EPSILON && orinLength > -EPSILON) ? 1.0f - (cutLength / orinLength) : 1.0f;

			XMVECTOR colorDirVector = endColorVector - begColorVector;
			float colorLength = XMVectorGetX(XMVector3Length(colorDirVector));
			colorDirVector = XMVector3Normalize(colorDirVector);

			XMVECTOR colorVector = begColorVector + colorLength * colorDirVector * rate;
			XMFLOAT4 color;
			XMStoreFloat4(&color, colorVector);
			m_Edges[i].BegColor = color;

			m_InterColors.push_back(m_Edges[i].BegColor);
		}
		else
		{
			subPosVector = endPosVector - begPosVector;
			float orinLength = XMVectorGetX(XMVector3Length(subPosVector));

			m_Edges[i].EndPos = interPos;
			endPosVector = XMLoadFloat3(&m_Edges[i].EndPos);

			subPosVector = begPosVector - endPosVector;
			float cutLength = XMVectorGetX(XMVector3Length(subPosVector));

			float rate = !(orinLength < EPSILON && orinLength > -EPSILON) ? 1.0f - (cutLength / orinLength) : 1.0f;

			XMVECTOR colorDirVector = begColorVector - endColorVector;
			float colorLength = XMVectorGetX(XMVector3Length(colorDirVector));
			colorDirVector = XMVector3Normalize(colorDirVector);

			XMVECTOR colorVector = endColorVector + colorLength * colorDirVector * rate;
			XMFLOAT4 color;
			XMStoreFloat4(&color, colorVector);
			m_Edges[i].EndColor = color;

			m_InterColors.push_back(m_Edges[i].EndColor);
		}
	}
}