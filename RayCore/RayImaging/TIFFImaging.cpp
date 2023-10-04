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
}
void CTIFFImaging::Process(char* fringes)
{
	m_end = std::chrono::system_clock::now();
	cv::Mat imgTIFF(cv::Size(m_setting.nBScan, m_setting.nAScan), CV_8UC4, fringes);

	cv::cvtColor(imgTIFF, imageConvert, cv::COLOR_BGRA2GRAY);
	cv::flip(imageConvert, imageConvert, 0);

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
void CTIFFImaging::CircularizeImage(cv::Mat& src, cv::Mat& dst)
{
	dst = src.clone();
	return;
}