#pragma once
#include "DataManager.h"

class CDataReader : public IDataManager
{
private:
	int m_nDataSize;
	int m_nHeaderSize;

	unsigned short** m_pReadSamples;
	HANDLE m_hFile;

	CRITICAL_SECTION m_csReadFrame;
public:
	CDataReader();
	virtual ~CDataReader();

	int Initialize(tstring strDataFilePath, int nDataSize, int nHeaderSize);
	virtual unsigned short* GetSample(int nIndex);
	virtual void AddFrame(void* pFrame) {}

	static OCTHeader ReadHeader(tstring strFilePath);
private:
	void finalize();
	bool readFrame(int nIndex);
};

