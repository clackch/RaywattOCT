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
	
	initCircularizeMap(m_setting.nAScan, m_setting.nBScan, m_setting.nAScan, m_setting.nBScan, m_setting.nAScan, 2.0f);
}

void CTIFFImaging::initCircularizeMap(int diameter, int srcHeight, int srcWidth, int dstHeight, int dstWidth, double scale) {
	COCTImaging::initCircularizeMap(diameter, srcHeight, srcWidth, dstHeight, dstWidth, scale);

	double radius = (diameter/2) - 0.5f;
	dematXMap.create(dstHeight, dstWidth, CV_32FC1);
	dematYMap.create(dstHeight, dstWidth, CV_32FC1);

	dematXMap.setTo(cv::Scalar::all(0));
	dematYMap.setTo(cv::Scalar::all(0));

	for (int y = 0; y < dstHeight; y++)
	{
		for (int x = 0; x < dstWidth; x++)
		{
			float r = (float)(srcWidth - y) / scale;
			float theta = ((float)x / srcHeight) * 2 * CV_PI - CV_PI / 2;

			float fx = r * cos(theta) + radius;
			float fy = r * sin(theta) + radius;

			dematXMap.at<float>(y, x) = fx;
			dematYMap.at<float>(y, x) = fy;
		}
	}
}

void CTIFFImaging::Process(char* fringes)
{
	
	m_end = std::chrono::system_clock::now();
	cv::Mat imgTIFF(cv::Size(m_setting.nBScan, m_setting.nAScan), CV_8UC4, fringes);

	cv::cvtColor(imgTIFF, imageConvert, cv::COLOR_BGRA2GRAY);
	cv::flip(imageConvert, imageConvert, 0);
	
	InverseCircularizeImage(imageConvert, imageConvert);

	std::chrono::milliseconds total_time = std::chrono::duration_cast<std::chrono::milliseconds>(m_end - m_start);
	long long msec = total_time.count();
	if (msec < 30)
	{
		Sleep(30 - msec);
	}

	m_start = m_end;
}

void CTIFFImaging::InverseCircularizeImage(cv::Mat& src, cv::Mat& dst)
{
	cv::remap(src, dst, dematXMap, dematYMap, cv::INTER_LINEAR);
	cv::rotate(dst, dst, cv::ROTATE_90_COUNTERCLOCKWISE);
}