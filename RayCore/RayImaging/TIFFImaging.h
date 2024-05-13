#pragma once
#include "OCTImaging.h"

class CTIFFImaging : public COCTImaging
{
private:
	cv::Mat imageConvert;	// role of OCTImaging::imageResult
	std::chrono::system_clock::time_point m_start, m_end;
	cv::Mat imageOrigin;
	cv::Mat imageMask;

	struct Calcium {
		int angleNum = 0;
		double* startAngle = nullptr;
		double* endAngle = nullptr;
	};
	Calcium* calciumData;

public:
	CTIFFImaging(Setting, CMessageService*);
	virtual ~CTIFFImaging();

	void Initialize();
	virtual void Process(char* fringes);
	virtual void PostProcess(cv::Mat image);
	virtual void CircularizeImage(cv::Mat& src, cv::Mat& dst);
	virtual cv::Mat GetProcessedImage() { return imageConvert; }
	virtual void SetCalciumAngle(std::vector<std::vector<cv::Point>> calciumContours, int currFrame);
};

