#pragma once

#include "DXUT.h"
#include <list>

class ControlNode
{
public:
	float NorPos;
	BYTE R;
	BYTE G;
	BYTE B;
	BYTE A;

public:
	ControlNode();
	ControlNode(float NorPos, BYTE R, BYTE G, BYTE B, BYTE A);
};

class TransferFunc
{
public:
	TransferFunc();
	~TransferFunc();

	void InsertControlNode(std::shared_ptr<ControlNode> controlNode);
	HRESULT Create1DTextureSourceData(BYTE** outArr, int* outSize);

protected:
	std::list<std::shared_ptr<ControlNode>> m_NodeList;
};

