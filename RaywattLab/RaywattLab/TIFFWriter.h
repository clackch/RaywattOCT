#pragma once

#include <tiffio.h>

class COCTImaging;
class CTIFFWriter
{
private:
	TIFF* m_pImageTIFF;
public:
	CTIFFWriter(CString strTIFPath);
	virtual ~CTIFFWriter();

	bool SaveFrame(COCTImaging*pImaging, unsigned short* pBuffer);
};

