#pragma once
#include "DataManager.h"

class CDataReader : public IDataManager
{
private:
	int m_nDataSize;

	unsigned short** m_pReadSamples;
	HANDLE m_hFile;

	CRITICAL_SECTION m_csReadFrame;
public:
	CDataReader();
	virtual ~CDataReader();

	int Initialize(tstring strDataFilePath, int nDataSize);
	virtual unsigned short* GetSample(int nIndex);
	virtual void AddFrame(void* pFrame) {}

private:
	void finalize();
	bool readFrame(int nIndex);
};

