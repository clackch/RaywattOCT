#pragma once

#include <opencv2/opencv.hpp>
class CMoriaImaging;
class CCutViewManager
{
private:
	cv::Mat m_imgCutView;
public:
	CCutViewManager();
	virtual ~CCutViewManager();

	void Initialize(int nNumOfSamples);
	void GenerateCutView(CMoriaImaging *pImaging, unsigned short *pBuffer, int nFrameIndex, double degree);
	cv::Mat GetCutView() { return m_imgCutView; }
	cv::Mat GetCutViewROI(int length);
	void DrawCutViewGuideLine(cv::Mat& img, double degree);
};

