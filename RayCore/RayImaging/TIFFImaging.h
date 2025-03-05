#pragma once
#include "OCTImaging.h"
#include <cmath>

class CTIFFImaging : public COCTImaging
{
private:
	cv::Mat imageConvert;	// role of OCTImaging::imageResult
	std::chrono::system_clock::time_point m_start, m_end;
	cv::Mat imageOrigin;
	cv::Mat imageMask;
	cv::Mat inverseMatXMap;
	cv::Mat inverseMatYMap;
	std::vector<cv::Point> inversedContourYPoints;

public:
	CTIFFImaging(Setting, CMessageService*);
	virtual ~CTIFFImaging();

	void Initialize();
	virtual void Process(char* fringes);
	virtual void PostProcess(cv::Mat image);
	virtual void CircularizeImage(cv::Mat& src, cv::Mat& dst);
	virtual cv::Mat GetProcessedImage() { return imageConvert; }
	virtual void SetCalciumAngle(std::vector<std::vector<cv::Point>> calciumContours, int& angleNum, std::vector<int>& startAngle, std::vector<int>& endAngle);
	virtual void initCircularizeMap(int diameter, int srcHeight, int srcWidth, int dstHeight, int dstWidth, double scale);
	virtual void InverseCircularizeImage(cv::Mat& src, cv::Mat& dst);
	virtual void EraseStentOutLier(cv::Mat& stent);
	virtual void SetLumenContourOffset(std::vector<cv::Point> lumenContour);

protected:
	void GetLumenOffsetPoints(std::vector<cv::Point>& lumenOffsetBoundary);
	double GetTheta(cv::Point vector1, cv::Point vector2);
	cv::Point2f RotatePoint(const cv::Point2f& point, const cv::Point2f& center);
};

