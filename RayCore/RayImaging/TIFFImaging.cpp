#include "TIFFImaging.h"
#include "LookUpTable.h"

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

	initCircularizeMap(m_setting.nAScan, m_setting.nBScan, m_setting.nAScan, m_setting.nBScan, m_setting.nAScan, 2.0f);
}

void CTIFFImaging::Process(char* fringes)
{
	m_end = std::chrono::system_clock::now();
	cv::Mat imgTIFF(cv::Size(m_setting.nBScan, m_setting.nAScan), CV_8UC4, fringes);

	cv::cvtColor(imgTIFF, imageOrigin, cv::COLOR_BGRA2GRAY);
	cv::flip(imageOrigin, imageOrigin, 0);

	// remove indicator
	cv::copyTo(imageOrigin, imageConvert, imageMask);

	for (int y = 955; y <= 970; y++) {
		for (int x = 740; x <= 750; x++) {
			if (x < imageConvert.cols && y < imageConvert.rows) {
				imageConvert.at<char>(y, x) = 0x00;
			}
		}
	}

	std::chrono::milliseconds total_time = std::chrono::duration_cast<std::chrono::milliseconds>(m_end - m_start);
	long long msec = total_time.count();
	if (msec < 30)
	{
		Sleep(30 - msec);
	}

	m_start = m_end;
}

void CTIFFImaging::PostProcess(cv::Mat image)
{
	const bool bColor = m_bColor;

	cv::cvtColor(image, imageCircle, cv::COLOR_GRAY2RGB);
	if (bColor) {
		CLookUpTable& lut = CLookUpTable::GetInstance();
		lut.Apply(imageCircle, 0);
	}

	cv::convertScaleAbs(imageCircle, imageCircle, m_setting.contrast, m_setting.brightness);
}

void CTIFFImaging::CircularizeImage(cv::Mat& src, cv::Mat& dst) {
	dst = src.clone();
	return;
}

void CTIFFImaging::initCircularizeMap(int diameter, int srcHeight, int srcWidth, int dstHeight, int dstWidth, double scale) {
	COCTImaging::initCircularizeMap(diameter, srcHeight, srcWidth, dstHeight, dstWidth, scale);
	PLOGI.printf("TIFFImageing initCircularize Map Start");

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

	PLOGI.printf("TIFFImageing initCircularize Map Done");
}

void CTIFFImaging::InverseCircularizeImage(cv::Mat& src, cv::Mat& dst) {
	dst = src.clone();
	cv::remap(dst, dst, inverseMatXMap, inverseMatYMap, cv::INTER_LINEAR);
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