#pragma once
#include "OCTImaging.h"

class CTIFFImaging : public COCTImaging
{
private:
	cv::Mat imageConvert;	// role of OCTImaging::imageResult
	cv::Mat dematXMap;
	cv::Mat dematYMap;
	std::chrono::system_clock::time_point m_start, m_end;

public:
	CTIFFImaging(Setting, CMessageService*);
	virtual ~CTIFFImaging();

	void Initialize();
	virtual void Process(char* fringes);
	virtual cv::Mat GetProcessedImage() { return imageConvert; }
	virtual void CircularizeImage(cv::Mat& src, cv::Mat& dst);
	virtual void initCircularizeMap(int diameter, int srcHeight, int srcWidth, int dstHeight, int dstWidth, double scale);

	void InverseCircularizeImage(cv::Mat& src, cv::Mat& dst);
};

