#pragma once

#include <opencv2/opencv.hpp>
class COCTImaging;
class CVolumeGenerator
{
private:
	std::vector<cv::Mat> m_vRecords;
	unsigned char* m_pVolumeData;
	cv::Rect m_rectROI;

public:
	CVolumeGenerator();
	virtual ~CVolumeGenerator();

	void Initialize(int srcWidth, int srcHeight, int roiWidth, int roiHeight);
	void AddRecord(unsigned short* pBuffer, COCTImaging* pImaging, int nFrameIndex);
	unsigned char* GetVolumeData();
	int GetVolumeWidth() { return m_rectROI.width; }
	int GetVolumeHeight() { return m_rectROI.height; }
	int GetVolumeDepth() { return m_vRecords.size(); }

private:
	void processing(cv::Mat& image, int sizeCatheter);
};

