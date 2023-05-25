#pragma once
#include "DataManager.h"

class CDataReader : public IDataManager
{
protected:
	int m_nDataSize;
	int m_nHeaderSize;

	char** m_pReadSamples;
	HANDLE m_hFile;

	CRITICAL_SECTION m_csReadFrame;
public:
	CDataReader();
	virtual ~CDataReader();

	int Initialize(tstring strDataFilePath, int nDataSize);
	virtual char* GetSample(int nIndex);
	virtual void AddFrame(void* pFrame) {}

	OCTHeader ReadHeader(tstring strFilePath);
protected:
	virtual void finalize();
	virtual bool readFrame(int nIndex);
	bool readExtraData(HANDLE hFile, OCTHeader::ExtraData extraData, int nSize);
};

