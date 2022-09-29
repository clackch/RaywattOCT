#pragma once

#include <opencv2/opencv.hpp>
class COCTImaging;
class CCutViewManager
{
private:
	std::vector<cv::Mat> m_vRecords;
	cv::Mat m_imgCutView;
public:
	CCutViewManager();
	virtual ~CCutViewManager();

	void Initialize(int nNumOfSamples);
	void GenerateCutView(double degree);
	void GenerateCutView(int nFrameIndex, double degree);
	void AddRecord(unsigned short* pBuffer, COCTImaging* pImaging, int nFrameIndex);
	cv::Mat GetCutView() { return m_imgCutView; }
	cv::Mat GetCutViewROI(int length);
	int GetNumOfSamples() { return m_vRecords.size(); }
	void DrawCutViewGuideLine(cv::Mat& img, double degree);
};

