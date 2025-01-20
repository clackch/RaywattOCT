#pragma once
#include "OCTImaging.h"

class CTIFFImaging : public COCTImaging
{
private:
	cv::Mat imageConvert;	// role of OCTImaging::imageResult
	std::chrono::system_clock::time_point m_start, m_end;
	cv::Mat imageOrigin;
	cv::Mat imageMask;
	cv::Mat inverseMatXMap; // circle image -> inverse circular -> Rotate CounterClock 90 -> Circluar -> Rotate_ClockWise 90
	cv::Mat inverseMatYMap;
	std::vector<cv::Point> inversedContourYPoints;

public:
	CTIFFImaging(Setting, CMessageService*);
	virtual ~CTIFFImaging();

	void Initialize();
	virtual void Process(char* fringes);
	virtual void PostProcess(cv::Mat image);
	virtual cv::Mat GetProcessedImage() { return imageConvert; }
	virtual void initCircularizeMap(int diameter, int srcHeight, int srcWidth, int dstHeight, int dstWidth, double scale);
	virtual void InverseCircularizeImage(cv::Mat& src, cv::Mat& dst);
	virtual void EraseStentOutLier(cv::Mat& stent);
	virtual void SetLumenContourOffset(std::vector<cv::Point> lumenContour);
	virtual void GetGuideWireCenterPoint(cv::Mat image, std::vector<cv::Rect2f> GuideWires, std::vector<cv::Point>& centerPoints, std::vector<double>& radius);

protected:
	void GetLumenOffsetPoints(std::vector<cv::Point>& lumenOffsetBoundary);
	void GetGuideWireCircleEdgePoints(cv::Mat image, std::vector<cv::Rect2f> GuideWires, std::vector<cv::Point>& edgePoints);
	void GetGuideWireShadowPointAngles(cv::Mat image, std::vector<cv::Point> edgePoints, std::vector<double>& theta);
	void RotatePoint90ClockWise(cv::Point& point);
	void GetCircularizeTransformPoint(cv::Point src, cv::Point& dst);
	void GetAcuteAngleToXAxis(cv::Vec2d vector1, cv::Vec2d vector2, double& angle);
};

