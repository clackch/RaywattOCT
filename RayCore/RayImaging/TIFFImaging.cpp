#include "TIFFImaging.h"

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
	imageConvert.create(m_setting.nBScan, m_setting.nAScan, CV_8UC3);
}
void CTIFFImaging::Process(char* fringes)
{
	cv::Mat imgTIFF(cv::Size(m_setting.nBScan, m_setting.nAScan), CV_8UC4, fringes);

	cv::cvtColor(imgTIFF, imageConvert, cv::COLOR_BGRA2RGB);
	cv::flip(imageConvert, imageConvert, 0);

	cv::copyTo(imageConvert, imageCircle, cv::Mat());
}