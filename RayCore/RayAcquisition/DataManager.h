#pragma once

class IDataManager
{
protected:
	int m_nNumOfSamples;
public:
	IDataManager() { m_nNumOfSamples = 0; }
	virtual ~IDataManager() {}

	int GetNumOfSamples() { return m_nNumOfSamples; }
	virtual unsigned short* GetSample(int nIndex) = 0;
	virtual void AddFrame(void* pFrame) = 0;
};

