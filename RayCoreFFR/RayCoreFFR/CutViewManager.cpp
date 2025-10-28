#include "Config.h"
#include "CutViewManager.h"
#include "LookUpTable.h"

CCutViewManager::CCutViewManager() {
}
CCutViewManager::~CCutViewManager() {
	for (int i = 0; i < m_vRecords.size(); i++) {
		m_vRecords.at(i).release();
	}
	m_imgCutView.release();
	m_imgLongitude.release();
}

void CCutViewManager::Initialize(int nNumOfSamples, cv::Scalar backgroundColor) {
	for (int i = 0; i < m_vRecords.size(); i++) {
		m_vRecords.at(i).release();
	}
	m_vRecords.resize(nNumOfSamples);

	m_imgCutView.release();
	m_imgCutView.create(1024, nNumOfSamples, CV_8UC1);
	m_imgConvertedCutView.release();
	m_imgConvertedCutView.create(1024, nNumOfSamples, CV_8UC3);
	m_imgLongitude = m_imgConvertedCutView.clone();
	m_imgLongitude.setTo(backgroundColor);
}
void CCutViewManager::GenerateCutView(double degree) {

#pragma omp parallel for
	for (int i = 0; i < m_vRecords.size(); i++) {
		GenerateCutView(i, degree);
	}
}
void CCutViewManager::GenerateCutView(int nFrameIndex, double degree) {
	// generate cut view
	cv::Mat imgCircle = m_vRecords.at(nFrameIndex);
	if(!imgCircle.empty()) {
		const int centerX = imgCircle.cols / 2;
		const int centerY = imgCircle.rows / 2;
		int radius = imgCircle.rows / 2;
		double xDirection = cos(degree * CV_PI / 180.0f);
		double yDirection = sin(degree * CV_PI / 180.0f);
		for (int r = 0; r < radius; r++) {
			cv::Point point;
			point.x = (int)round(centerX + r * xDirection);
			point.y = (int)round(centerY + r * yDirection);
			int y = centerY - r;

			m_imgCutView.at<char>(y, nFrameIndex) = imgCircle.at<char>(point);
		}
		xDirection = cos((180 + degree) * CV_PI / 180.0f);
		yDirection = sin((180 + degree) * CV_PI / 180.0f);
		for (int r = 0; r < radius; r++) {
			cv::Point point;
			point.x = (int)round(centerX + r * xDirection);
			point.y = (int)round(centerY + r * yDirection);
			int y = centerY + r;

			m_imgCutView.at<char>(y, nFrameIndex) = imgCircle.at<char>(point);
		}
	}
}
void CCutViewManager::AddRecord(cv::Mat imgCircle, int nFrameIndex) {
	if (nFrameIndex >= 0 && nFrameIndex < m_vRecords.size()) {
		m_vRecords.at(nFrameIndex) = imgCircle.clone();
	}
}
cv::Mat CCutViewManager::DrawLongitudeImage(int nDrawSamples, double brightness, double contrast) {
	cv::Mat imgMask = cv::Mat(m_imgCutView.rows, m_imgCutView.cols, CV_8UC1);
	cv::Rect rectMask = cv::Rect(0, 0, nDrawSamples, imgMask.rows);
	memset(imgMask.data, 0x00, imgMask.cols * imgMask.rows);
	imgMask(rectMask) = 0x01;
	
	CLookUpTable& lut = CLookUpTable::GetInstance();
	cv::cvtColor(m_imgCutView, m_imgConvertedCutView, cv::COLOR_GRAY2RGB);
	if (lut.GetEnhancedLUT()) {
		lut.Apply(m_imgConvertedCutView, 3 /*LUT_enhanced.csv*/);
		lut.Apply(m_imgConvertedCutView, lut.GetCurrentColormap());
	}
	else {
		lut.Apply(m_imgConvertedCutView, lut.GetCurrentColormap());
	}

	cv::convertScaleAbs(m_imgConvertedCutView, m_imgConvertedCutView, contrast, brightness);
	cv::copyTo(m_imgConvertedCutView, m_imgLongitude, imgMask);
	
	return m_imgLongitude;
}
int CCutViewManager::GetNumOfGeneratedSamples() {
	int nFrames = 0;

	for ( ; nFrames < m_vRecords.size(); nFrames++) {
		if (m_vRecords.at(nFrames).empty()) return nFrames;
	}

	return nFrames;
}
void CCutViewManager::DrawCutViewGuideLine(cv::Mat& img, double degree) {
	double xDirection;
	double yDirection;
	int radius = img.rows / 2;
	int centerX = img.cols / 2;
	int centerY = img.rows / 2;

	cv::Point ptStart;
	xDirection = cos(degree * CV_PI / 180.0f);
	yDirection = sin(degree * CV_PI / 180.0f);
	ptStart.x = (int)round(centerX + radius * xDirection);
	ptStart.y = (int)round(centerY + radius * yDirection);

	cv::Point ptEnd;
	xDirection = cos((180 + degree) * CV_PI / 180.0f);
	yDirection = sin((180 + degree) * CV_PI / 180.0f);
	ptEnd.x = (int)round(centerX + radius * xDirection);
	ptEnd.y = (int)round(centerY + radius * yDirection);

	cv::line(img, ptStart, ptEnd, cv::Scalar(0xF5, 0xA5, 0x42, 0), 2);
}