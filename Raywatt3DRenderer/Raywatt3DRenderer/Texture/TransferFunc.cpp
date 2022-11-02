#include "TransferFunc.h"
#include "Constants.h"
#include "Utility.h"

ControlNode::ControlNode() 
{
	NorPos = R = G = B = 0.f;
}

ControlNode::ControlNode(float NorPos, BYTE R, BYTE G, BYTE B, BYTE A)
{
	this->NorPos = NorPos;
	this->R = R;
	this->G = G; 
	this->B = B;
	this->A = A;
}


TransferFunc::TransferFunc()
{
}


TransferFunc::~TransferFunc()
{
	m_NodeList.clear();
}

void TransferFunc::InsertControlNode(std::shared_ptr<ControlNode> controlNode)
{
	// ADDING IMPLEMENT NEEDED
	m_NodeList.push_back(controlNode);
}

int GetClipedPosition(float val, int range) 
{
	return Utility::clip<int>((int)(val * range), 0, range);
}

HRESULT TransferFunc::Create1DTextureSourceData(BYTE** out, int* outSize)
{
	const int CHANNEL_SIZE = 4;
	const int RANGE = 255;
	const int TABLE_SIZE = CHANNEL_SIZE * (RANGE + 1);
	(*out) = new BYTE[TABLE_SIZE] {0, };
	auto nodeCount = m_NodeList.size();
	*outSize = RANGE + 1;
	
	std::shared_ptr<ControlNode> cursor;
	std::list<std::shared_ptr<ControlNode>>::iterator iterPos = m_NodeList.begin();
	std::list<std::shared_ptr<ControlNode>>::iterator iterEnd = m_NodeList.end();
	
	if (nodeCount <= 1) {
		return S_FALSE;
	}

	cursor = (*iterPos);

	int pos1;
	BYTE r1, g1, b1, a1;
	int pos2;
	BYTE r2, g2, b2, a2;
	
	pos1 = GetClipedPosition(cursor->NorPos, RANGE);
	r1 = cursor->R;
	g1 = cursor->G;
	b1 = cursor->B;
	a1 = cursor->A;
	
	++iterPos;
	for (; iterPos != iterEnd; ++iterPos)
	{
		cursor = (*iterPos);
		pos2 = GetClipedPosition(cursor->NorPos, RANGE);
		r2 = cursor->R;
		g2 = cursor->G;
		b2 = cursor->B;
		a2 = cursor->A;
		float len = static_cast<float>(pos2 - pos1);
		for (int i = pos1; i <= pos2; i++) {
			float d = (float)(i-pos1) / len;
			BYTE b = Utility::lerp<BYTE>(b1, b2, d);
			BYTE g = Utility::lerp<BYTE>(g1, g2, d);
			BYTE r = Utility::lerp<BYTE>(r1, r2, d);
			BYTE a = Utility::lerp<BYTE>(a1, a2, d);
			(*out)[i * 4 + 0] = b;
			(*out)[i * 4 + 1] = g;
			(*out)[i * 4 + 2] = r;
			(*out)[i * 4 + 3] = a;
		}
		pos1 = pos2;
		r1 = r2;
		g1 = g2;
		b1 = b2;
		a1 = a2;
	}
	return S_OK;
}