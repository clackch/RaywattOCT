#pragma once
#include "DataManager.h"

class CDataReader : public IDataManager
{
private:
	unsigned short** m_pReadSamples;
	HANDLE m_hFile;

	CRITICAL_SECTION m_csReadFrame;
public:
	CDataReader();
	virtual ~CDataReader();

	int Initialize(tstring strDataFilePath);
	virtual unsigned short* GetSample(int nIndex);

private:
	void finalize();
	bool readFrame(int nIndex);
};

