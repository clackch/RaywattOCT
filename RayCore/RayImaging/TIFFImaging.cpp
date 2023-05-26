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
	m_end = std::chrono::system_clock::now();
	cv::Mat imgTIFF(cv::Size(m_setting.nBScan, m_setting.nAScan), CV_8UC4, fringes);

	cv::cvtColor(imgTIFF, imageConvert, cv::COLOR_BGRA2RGB);
	cv::flip(imageConvert, imageConvert, 0);

	cv::copyTo(imageConvert, imageCircle, cv::Mat());

	std::chrono::milliseconds total_time = std::chrono::duration_cast<std::chrono::milliseconds>(m_end - m_start);
	long long msec = total_time.count();
	if (msec < 30)
	{
		Sleep(30 - msec);
	}

	m_start = m_end;
}