#pragma once
#include "OCTImaging.h"

class CTIFFImaging : public COCTImaging
{
private:
	cv::Mat imageConvert;	// role of OCTImaging::imageResult
	std::chrono::system_clock::time_point m_start, m_end;

public:
	CTIFFImaging(Setting, CMessageService*);
	virtual ~CTIFFImaging();

	void Initialize();
	virtual void Process(char* fringes);
	virtual void PostProcess(cv::Mat image);
	virtual cv::Mat GetProcessedImage() { return imageConvert; }
	virtual void CircularizeImage(cv::Mat& src, cv::Mat& dst);
};

