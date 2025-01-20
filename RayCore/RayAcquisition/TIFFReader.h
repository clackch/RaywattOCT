#pragma once
#include "DataReader.h"
#include <tiffio.h>

class CTIFFReader : public CDataReader
{
private:
	TIFF* m_pTif;
	int m_nWidth, m_nHeight, m_nChannels;
public:
	CTIFFReader();
	virtual ~CTIFFReader();

	int Initialize(const char* strDataFilePath);
	void GetImageSize(int& width, int& height);

protected:
	virtual void finalize();
	virtual bool readFrame(int nIndex);
};

