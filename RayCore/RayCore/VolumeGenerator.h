#pragma once

#include <opencv2/opencv.hpp>
class COCTImaging;
class CVolumeGenerator
{
private:
	std::vector<cv::Mat> m_vRecords;
	unsigned char* m_pVolumeData;

public:
	CVolumeGenerator();
	virtual ~CVolumeGenerator();

	void AddRecord(unsigned short* pBuffer, COCTImaging* pImaging, int nFrameIndex);
	unsigned char* GetVolumeData();

private:
	void processing(cv::Mat& image, int sizeCatheter);
};

