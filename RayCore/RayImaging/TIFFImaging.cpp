#include "TIFFImaging.h"
#include "LookUpTable.h"
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