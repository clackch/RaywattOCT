#include "VolumeGenerator.h"
#include "OCTImaging.h"
#include "Configuration.h"


CVolumeGenerator::CVolumeGenerator() {
	m_vRecords.clear();
	m_pVolumeData = nullptr;
}
CVolumeGenerator::~CVolumeGenerator() {
	m_vRecords.clear();
	if (m_pVolumeData != nullptr) {
		delete[] m_pVolumeData;
		m_pVolumeData = nullptr;
	}
}

void CVolumeGenerator::Initialize(int srcWidth, int srcHeight, int roiWidth, int roiHeight) {
	int x = (srcWidth - roiWidth) / 2;
	int y = (srcHeight - roiHeight) / 2;
	
	m_rectROI = cv::Rect(x, y, roiWidth, roiHeight);
}

void CVolumeGenerator::AddRecord(unsigned short* pBuffer, COCTImaging* pImaging, int nFrameIndex) {
	CConfiguration& config = CConfiguration::GetInstance();
	const int sizeCatheter = (int)((double)config.measurement.nSheathPosition * 1.1f);

	pImaging->Process(pBuffer);
	
	cv::Mat imgCircle = pImaging->GetCircleImage();
	cv::Mat imgGray;

	cv::cvtColor(imgCircle(m_rectROI), imgGray, cv::COLOR_BGR2GRAY);
	processing(imgGray, sizeCatheter);

	m_vRecords.push_back(imgGray);
}

unsigned char* CVolumeGenerator::GetVolumeData() {
	if (m_vRecords.empty()) return nullptr;

	if (m_pVolumeData != nullptr) {
		delete[] m_pVolumeData;
		m_pVolumeData = nullptr;
	}

	const long long width = m_vRecords.at(0).cols;
	const long long height = m_vRecords.at(0).rows;
	const long long frameSize = width * height;
	const long long depth = m_vRecords.size();

	m_pVolumeData = new unsigned char[frameSize * depth];

	for (int i = 0; i < depth; i++) {
		memcpy(m_pVolumeData + frameSize * i, m_vRecords.at(i).data, frameSize);
	}

	return m_pVolumeData;
}

void CVolumeGenerator::processing(cv::Mat& image, int sizeCatheter) {
	CConfiguration& config = CConfiguration::GetInstance();
	const int width = image.cols;
	const int height = image.rows;
	const int centerX = width / 2 - 1;
	const int centerY = height / 2 - 1;

	cv::circle(image, cv::Point(centerX, centerY), sizeCatheter, cv::Scalar(0, 0, 0), cv::FILLED); // remove catheter
	cv::rectangle(image, cv::Rect(0, 0, centerX, height-1), cv::Scalar(0, 0, 0), cv::FILLED); // cut longitude
	
#pragma omp parallel for
	for (int i = 0; i < image.cols * image.rows; i++) {
		image.data[i] = image.data[i] < config.volume.threshold ? 0 : image.data[i];
	}
}