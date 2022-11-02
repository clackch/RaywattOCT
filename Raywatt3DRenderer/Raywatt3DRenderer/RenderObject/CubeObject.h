#pragma once

#include "BaseRenderObject.h"
#include "VolumeDataInfo.h"
#include "DXUTcamera.h"

struct CubeEdge
{
	XMFLOAT3 BegPos;
	XMFLOAT3 EndPos;
	XMFLOAT4 BegColor;
	XMFLOAT4 EndColor;
};

class CubeObject : public BaseRenderObject
{
public:
	static const int EDGE_COUNT = 12;

	struct VertexType
	{
		XMFLOAT3 Pos;
		XMFLOAT4 Color;
	};

	CubeObject();
	~CubeObject();

	HRESULT Initialize(ID3D11Device* pDevice, VolumeDataInfo& volmeDataInfo, CModelViewerCamera* camera);

	virtual HRESULT InitStride(ID3D11Device* pDevice) override;
	virtual HRESULT InitVertexBuffer(ID3D11Device* pDevice) override;
	virtual HRESULT InitIndexBuffer(ID3D11Device* pDevice) override;
	virtual void Update(float delta) override;
	virtual void OnKeyboard(UINT nChar, bool bKeyDown, bool bAltDown, void* pUserContext) override;
	virtual void MsgProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam,
		bool* pbNoFurtherProcessing, void* pUserContext) override;

protected:
	XMFLOAT3 m_Ratio;
	CubeEdge m_Edges[EDGE_COUNT];

	XMMATRIX m_mRT;
	XMFLOAT3 m_Pos;
	XMFLOAT3 m_RotAngle;
	
	XMFLOAT3 m_OriginPos[8];
	VertexType m_BoxVertices[8];

	std::vector<XMFLOAT3> m_NewPositions;
	std::vector<XMFLOAT4> m_NewColors;
	std::vector<int> m_NewIndexes;

	std::vector<XMFLOAT3> m_InterPositions;
	std::vector<XMFLOAT4> m_InterColors;

	CModelViewerCamera* m_Camera;
	CD3DArcBall m_Arcball;
	
	float m_fDeltaTime;
	XMMATRIX m_LocalTransMatrix;
	XMMATRIX m_LocalRotMatrix;

public:
	GET(XMFLOAT3, m_Ratio, Ratio);
	void UpdateIntersectionPlane();
	void DeltaRotate(XMFLOAT3 rot);
	void DeltaTranslate(XMFLOAT3 dist);
	void Translate(XMFLOAT3 dist);

protected:
	void MakeNewVertexInfo();
	float GetClockWiseValue(const XMVECTOR& v1, const XMVECTOR& v2, const XMVECTOR& v3);
};

