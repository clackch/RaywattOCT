#include "TIFFImaging.h"
#include "LookUpTable.h"
#include <cmath>
#include <string>

CTIFFImaging::CTIFFImaging(Setting setting, CMessageService* pMsg)
	: COCTImaging(setting, pMsg) 
{
}
CTIFFImaging::~CTIFFImaging() 
{
}

void CTIFFImaging::Initialize()
{
	m_nWidth = m_setting.nAScan;
	m_nHeight = m_setting.nBScan;
	m_nChannels = 3;	// RGB

	imageCircle.create(m_setting.nBScan, m_setting.nAScan, CV_8UC3);
	imageConvert.create(m_setting.nBScan, m_setting.nAScan, CV_8UC1);
	imageOrigin.create(m_setting.nBScan, m_setting.nAScan, CV_8UC1);
	imageMask.create(m_setting.nBScan, m_setting.nAScan, CV_8UC1);
	memset(imageMask.data, 0x00, m_setting.nBScan * m_setting.nAScan);
	cv::circle(imageMask, cv::Point(imageMask.cols / 2, imageMask.rows / 2), imageMask.cols / 2, cv::Scalar(0xff, 0xff, 0xff), -1);

	releaseInversedCircularizeMap();
	initInversedCircularizeMap(m_setting.nAScan, m_setting.nBScan, m_setting.nAScan, m_setting.nBScan, m_setting.nAScan, 2.0f);
	releaseCircularizeMap();
	initCircularizeMap(m_setting.nAScan, m_setting.nBScan, m_setting.nAScan, m_setting.nBScan, m_setting.nAScan, 2.0f);
}

void CTIFFImaging::Process(char* fringes)
{
	cv::Mat imgTIFF(cv::Size(m_setting.nBScan, m_setting.nAScan), CV_8UC1, fringes);
	imageConvert = imgTIFF.clone();

	InverseCircularizeImage(imageConvert, imageConvert);

	imageResultWithoutCompensation = imageConvert.clone();
}

void CTIFFImaging::PostProcess(cv::Mat image)
{
	const bool bColor = m_bColor;

	findSheath(image);

	cv::cvtColor(image, imageCircle, cv::COLOR_GRAY2RGB);
	if (bColor) {
		CLookUpTable& lut = CLookUpTable::GetInstance();
		if (lut.GetEnhancedLUT()) {
			lut.Apply(imageCircle, 3 /*LUT_enhanced.csv*/);
			lut.Apply(imageCircle, lut.GetCurrentColormap());
		}
		else {
			lut.Apply(imageCircle, lut.GetCurrentColormap());
		}
	}

	cv::convertScaleAbs(imageCircle, imageCircle, m_setting.contrast, m_setting.brightness);

	CircularizeImage(imageCircle, imageCircle);
}

void CTIFFImaging::initCircularizeMap(int diameter, int srcHeight, int srcWidth, int dstHeight, int dstWidth, double scale) {
	COCTImaging::initCircularizeMap(diameter, srcHeight, srcWidth, dstHeight, dstWidth, scale);

	double radius = (diameter / 2) - 0.5f;
	inverseMatXMap.create(dstHeight, dstWidth, CV_32FC1);
	inverseMatYMap.create(dstHeight, dstWidth, CV_32FC1);

	inverseMatXMap.setTo(cv::Scalar::all(0));
	inverseMatYMap.setTo(cv::Scalar::all(0));

	for (int y = 0; y < dstHeight; y++)
	{
		for (int x = 0; x < dstWidth; x++)
		{
			float r = (float)(srcWidth - y) / scale;
			float theta = ((float)x / srcHeight) * 2 * CV_PI;

			float fx = r * cos(theta) + radius;
			float fy = r * sin(theta) + radius;

			inverseMatXMap.at<float>(y, x) = fx;
			inverseMatYMap.at<float>(y, x) = fy;
		}
	}
}

void CTIFFImaging::InverseCircularizeImage(cv::Mat& src, cv::Mat& dst) {
	dst = src.clone();
	cv::remap(dst, dst, inverseMatXMap, inverseMatYMap, cv::INTER_LINEAR);

	cv::rotate(dst, dst, cv::ROTATE_90_COUNTERCLOCKWISE);
}

void CTIFFImaging::EraseStentOutLier(cv::Mat& stent) {
	int width = m_nWidth;
	int height = m_nHeight;

	// Lumen Offset 설정
	std::vector<cv::Point> LumenOffsetPoints;
	for (int i = 0; i < height; i++) {
		LumenOffsetPoints.push_back(cv::Point(0, 0));
	}
	GetLumenOffsetPoints(LumenOffsetPoints);

	for (int i = 0; i < LumenOffsetPoints.size(); i++) {
		cv::Point point = LumenOffsetPoints[i];
	}
	
	cv::Mat blackImage = cv::Mat::zeros(height, width, CV_8UC1);

	for (int row = 0; row < stent.rows; row++) {
		cv::Point point = stent.at<cv::Point>(row, 0);
		if (point.x >= 0 && point.x < width && point.y >= 0 && point.y < height) {
			blackImage.at<uchar>(point.y, point.x) = 255;
		}
	}
	
	cv::Mat remappedImage;
	cv::remap(blackImage, remappedImage, inverseMatXMap, inverseMatYMap, cv::INTER_NEAREST);
	cv::rotate(remappedImage, remappedImage, cv::ROTATE_90_COUNTERCLOCKWISE);

	while (!stent.empty()) {
		stent.pop_back();
	}

	// Stent Outlier를 제외한 Stent Point만 Push
	for (int y = 0; y < height; y++) {
		for (int x = 0; x < width; x++) {
			if (remappedImage.at<uchar>(y, x) == 255 && LumenOffsetPoints[y].x < x) {
				stent.push_back(cv::Point(x, y));
			}
		}
	}

	blackImage = cv::Mat::zeros(height, width, CV_8UC1);
	for (int row = 0; row < stent.rows; row++) {
		cv::Point point = stent.at<cv::Point>(row, 0);
		if (point.x >= 0 && point.x < width && point.y >= 0 && point.y < height) {
			blackImage.at<uchar>(point.y, point.x) = 255;
		}
	}

	remappedImage = cv::Mat::zeros(height, width, CV_8UC1);
	cv::remap(blackImage, remappedImage, matXMap, matYMap, cv::INTER_NEAREST);
	cv::rotate(remappedImage, remappedImage, cv::ROTATE_90_CLOCKWISE);

	while (!stent.empty()) {
		stent.pop_back();
	}

	// 극좌표 변환된 Stent Push
	for (int y = 0; y < remappedImage.rows; y++) {
		for (int x = 0; x < remappedImage.cols; x++) {
			if (remappedImage.at<uchar>(y, x) == 255) {
				stent.push_back(cv::Point(x, y));
			}
		}
	}
	
	remappedImage.release();
	blackImage.release();
}

void CTIFFImaging::SetLumenContourOffset(std::vector<cv::Point> lumenContour) {
	if (lumenContour.empty()) {
		return;
	}

	int width = m_nWidth;
	int height = m_nHeight;

	std::vector<std::vector<cv::Point>> lumenContours;
	lumenContours.push_back(lumenContour);

	cv::Mat blackImage = cv::Mat::zeros(height, width, CV_8UC1);

	cv::drawContours(blackImage, lumenContours, -1, cv::Scalar(255), 1);

	cv::remap(blackImage, blackImage, inverseMatXMap, inverseMatYMap, cv::INTER_LINEAR);

	cv::rotate(blackImage, blackImage, cv::ROTATE_90_COUNTERCLOCKWISE);

	//Rectangle Contour의 각 Row에 해당하는 X좌표 설정 (평균값)
	inversedContourYPoints.clear();
	for (int y = 0; y < height; y++) {
		double sumOfx= 0;
		int count = 0;
		for (int x = 0; x < width; x++) {
			if (blackImage.at<uchar>(y, x) > 0) {
				sumOfx += x;
				count++;
			}
		}
		int avgX = (int)(sumOfx / count);
		if (avgX >= 0 && avgX < width) {
			inversedContourYPoints.push_back(cv::Point(avgX, y));
			cv::circle(blackImage, cv::Point(avgX, y), 1, cv::Scalar(200), - 1);
		}
		else {
			inversedContourYPoints.push_back(cv::Point(width - 1, y));
		}
	}
}

void CTIFFImaging::GetLumenOffsetPoints(std::vector<cv::Point>& lumenOffsetBoundary) {

	for (int i = 0; i < inversedContourYPoints.size(); i++) {
		cv::Point point = inversedContourYPoints[i];
		int x, y;
		x = point.x - 50;  //TODO - offset 값을 OCT Lumen 값 평균을 활용하여 그림자 영역 판별할 수 있는 Offset 만들기
		y = point.y;

		lumenOffsetBoundary[i] = cv::Point(x, y);
	}
}

void CTIFFImaging::SetCalciumAngle(std::vector<std::vector<cv::Point>> calciumContours, int& angleNum, std::vector<int>& startAngle, std::vector<int>& endAngle, int frameNum) {
	
	if (calciumContours.empty())return;

	// 컨투어 그리기
	cv::Mat contourImage = cv::Mat::zeros(imageCircle.size(), CV_8UC1);
	cv::drawContours(contourImage, calciumContours, -1, cv::Scalar(255), cv::FILLED);
	
	cv::Mat contourRectImg;
	InverseCircularizeImage(contourImage, contourRectImg); 

	std::vector<std::vector<cv::Point>> rectangleCalciumContours;
	cv::findContours(contourRectImg, rectangleCalciumContours, cv::RETR_EXTERNAL, cv::CHAIN_APPROX_SIMPLE);
	std::vector<cv::Point> startAnglePoint; 
	std::vector<cv::Point> endAnglePoint;   
	for (const auto& contour : rectangleCalciumContours) {
		cv::Rect rect = cv::boundingRect(contour);
		if (rect.height * rect.width <= 500)continue;
		startAnglePoint.push_back(cv::Point(rect.x + rect.width / 2, rect.y + rect.height));
		endAnglePoint.push_back(cv::Point(rect.x + rect.width / 2, rect.y));  
	}

	cv::Point center(m_nWidth / 2, m_nHeight / 2);
	cv::Point standard(m_nWidth / 2, 0);

	cv::Mat tissue = imageCircle.clone();

	if (tissue.channels() == 1) {
		cv::cvtColor(tissue, tissue, cv::COLOR_GRAY2BGR);
	}
	cv::Point test;
	PLOGI.printf("startAnglePoint.size() = %d", startAnglePoint.size());
	for (int i = 0; i < startAnglePoint.size(); i++) {
		PLOGI.printf("startAnglePointX = %d, startAnglePointY = %d", startAnglePoint[i].x, startAnglePoint[i].y);  
		startAnglePoint[i] = matXY(startAnglePoint[i], m_nWidth, m_nHeight);
		test = startAnglePoint[i];
		endAnglePoint[i] = matXY(endAnglePoint[i], m_nWidth, m_nHeight);
		PLOGI.printf("tempX = %d, tmpY = %d", startAnglePoint[i].x, startAnglePoint[i].y); 
		
	}
	for (int i = 0; i < startAnglePoint.size(); i++) {
		double sAngle = GetTheta(startAnglePoint[i], center);
		double eAngle = GetTheta(endAnglePoint[i], center);
		PLOGI.printf("Start_Angle = %lf, end_Angle = %lf", sAngle, eAngle);
		startAngle.push_back(sAngle);
		endAngle.push_back(eAngle);
	}

	angleNum = startAnglePoint.size();
}

double CTIFFImaging::GetTheta(cv::Point point, cv::Point center) {

	int dx = point.x - center.x;
	int dy = point.y - center.y;
	double angle = std::atan2(dx, -dy) * 180.0 / CV_PI;
	if (angle < 0) {
		angle += 360.0;
	}
	return angle;
}

// 주어진 점을 반시계 방향으로 90도 회전시키는 함수
cv::Point2f  CTIFFImaging::RotatePoint(const cv::Point2f& point, const cv::Point2f& center) {
	// 점을 중심점 기준으로 이동
	float translatedX = point.x - center.x;
	float translatedY = point.y - center.y;

	// 시계 방향으로 90도 회전 변환 적용
	float rotatedX = translatedY;
	float rotatedY = -translatedX;

	// 회전된 점을 원래 위치로 이동
	return cv::Point2f(rotatedX + center.x, rotatedY + center.y);
}

cv::Point2f CTIFFImaging::matXY(const cv::Point2f& srcPt, int m_nWidth, int m_nHeight)
{
	float bestDist = std::numeric_limits<float>::max();
	cv::Point2f bestXY(0.f, 0.f);
	for (int y = 0; y < m_nHeight; y++)
	{
		const float* xMapRow = matXMap.ptr<float>(y);
		const float* yMapRow = matYMap.ptr<float>(y);

		for (int x = 0; x < m_nWidth; x++)
		{
			float dx = xMapRow[x] - srcPt.x;
			float dy = yMapRow[x] - srcPt.y;
			float distSq = dx * dx + dy * dy;

			if (distSq < bestDist)
			{
				bestDist = distSq;
				bestXY = cv::Point2f((float)x, (float)y);
			}
		}
	}
	float cx = 0.5f * m_nWidth;
	float cy = 0.5f * m_nHeight;
	float xShift = bestXY.x - cx;
	float yShift = bestXY.y - cy;
	float rx = yShift;
	float ry = -xShift;
	rx += cx;
	ry += cy;
	cv::Point test;
	test.x = rx; test.y = ry;
	cv::Point center(m_nWidth / 2, m_nHeight / 2);
	test = rotatePoint_CCW90(test, center); 
	test = rotatePoint_CCW90(test, center); 
	return cv::Point2f(test.x, test.y); 
}

// 시계방향 회전
cv::Point2f CTIFFImaging::rotatePoint_CCW90(const cv::Point2f& point, const cv::Point2f& center) 
{
	float translatedX = static_cast<float>(point.x) - center.x;
	float translatedY = static_cast<float>(point.y) - center.y;
	float rotatedX = -translatedY;
	float rotatedY = translatedX;
	int finalX = static_cast<int>(std::round(rotatedX + center.x));
	int finalY = static_cast<int>(std::round(rotatedY + center.y));
	return cv::Point(finalX, finalY);
}