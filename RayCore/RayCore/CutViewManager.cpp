#include "Config.h"
#include "CutViewManager.h"
#include "Configuration.h"

CCutViewManager::CCutViewManager() {
}
CCutViewManager::~CCutViewManager() {
	for (int i = 0; i < m_vRecords.size(); i++) {
		m_vRecords.at(i).release();
	}
	m_imgCutView.release();
}

void CCutViewManager::Initialize(int nNumOfSamples, cv::Scalar backgroundColor) {
	for (int i = 0; i < m_vRecords.size(); i++) {
		m_vRecords.at(i).release();
	}
	m_vRecords.resize(nNumOfSamples);

	m_imgCutView.release();
	m_imgCutView.create(1024, nNumOfSamples, CV_8UC3);
	m_imgCutView.setTo(backgroundColor);
}
void CCutViewManager::GenerateCutView(double degree) {

#pragma omp parallel for
	for (int i = 0; i < m_vRecords.size(); i++) {
		GenerateCutView(i, degree);
	}
}
void CCutViewManager::GenerateCutView(int nFrameIndex, double degree) {
	CConfiguration& config = CConfiguration::GetInstance();
	const int centerX = config.nCircleSize / 2;
	const int centerY = config.nCircleSize / 2;

	// generate cut view
	cv::Mat imgCircle = m_vRecords.at(nFrameIndex);
	if(!imgCircle.empty()) {
		int radius = imgCircle.rows / 2;
		double xDirection = cos(degree * CV_PI / 180.0f);
		double yDirection = sin(degree * CV_PI / 180.0f);
		for (int r = 0; r < radius; r++) {
			cv::Point point;
			point.x = (int)round(centerX + r * xDirection);
			point.y = (int)round(centerY + r * yDirection);
			int y = centerY - r;

			m_imgCutView.at<cv::Vec3b>(y, nFrameIndex) = imgCircle.at<cv::Vec3b>(point);
		}
		xDirection = cos((180 + degree) * CV_PI / 180.0f);
		yDirection = sin((180 + degree) * CV_PI / 180.0f);
		for (int r = 0; r < radius; r++) {
			cv::Point point;
			point.x = (int)round(centerX + r * xDirection);
			point.y = (int)round(centerY + r * yDirection);
			int y = centerY + r;

			m_imgCutView.at<cv::Vec3b>(y, nFrameIndex) = imgCircle.at<cv::Vec3b>(point);
		}
	}
}
void CCutViewManager::AddRecord(cv::Mat imgCircle, int nFrameIndex) {
	if (nFrameIndex >= 0 && nFrameIndex < m_vRecords.size()) {
		m_vRecords.at(nFrameIndex) = imgCircle.clone();
	}
}
cv::Mat CCutViewManager::GetCutViewROI(int length) {
	cv::Rect rectROI;
	rectROI.x = 0;
	rectROI.width = m_imgCutView.cols;
	rectROI.height = length;
	rectROI.y = rectROI.height / 2;
	
	return m_imgCutView(rectROI);
}
int CCutViewManager::GetNumOfGeneratedSamples() {
	int nFrames = 0;

	for (nFrames = 0; nFrames < m_vRecords.size(); nFrames++) {
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