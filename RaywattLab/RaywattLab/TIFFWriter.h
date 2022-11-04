#pragma once

#include <tiffio.h>
#include <opencv2/opencv.hpp>

class COCTImaging;
class CTIFFWriter
{
private:
	TIFF* m_pImageTIFF;
public:
	CTIFFWriter(CString strTIFPath);
	virtual ~CTIFFWriter();

	bool SaveFrame(cv::Mat image);
};

