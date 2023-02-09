#pragma once

#include <opencv2/opencv.hpp>
class CCutViewManager
{
private:
	std::vector<cv::Mat> m_vRecords;
	cv::Mat m_imgCutView;
	cv::Mat m_imgLongitude;

public:
	CCutViewManager();
	virtual ~CCutViewManager();

	void Initialize(int nNumOfSamples, cv::Scalar backgroundColor);
	
	void GenerateCutView(double degree);
	void GenerateCutView(int nFrameIndex, double degree);
	void AddRecord(cv::Mat imgCircle, int nFrameIndex);

	cv::Mat DrawLongitudeImage(int nDrawSamples, int pxFOV);
	cv::Mat GetCutView() { return m_imgCutView; }
	cv::Mat GetCutViewROI(int length);
	
	int GetNumOfSamples() { return m_vRecords.size(); }
	int GetNumOfGeneratedSamples();
	
	void DrawCutViewGuideLine(cv::Mat& img, double degree);
};

