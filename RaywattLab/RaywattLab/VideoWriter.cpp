#include "pch.h"
#include "VideoWriter.h"
#include "Utility.h"

CVideoWriter::CVideoWriter() {
	m_pThread = NULL;

	m_nVideoWidth = 0;
	m_nVideoHeight = 0;

	m_pFrameQueue = NULL;
	m_nQueueCount = 10;
	m_nQueueStart = 0;
	m_nQueueEnd = 0;
}
CVideoWriter::~CVideoWriter() {
	StopRecording();
	finalize();
}

int CVideoWriter::StartRecording(CString strFilePath, int nWidth, int nHeight) {
	if (m_pThread == NULL) {
		char strVideoFilePath[MAX_PATH];
		int nFrameRate = 30;
		wcstombs(strVideoFilePath, strFilePath.GetBuffer(), MAX_PATH);

		bool result = m_videoWriter.open(strVideoFilePath, cv::CAP_ANY,	cv::VideoWriter::fourcc('A','V','I','1'), nFrameRate, cv::Size(nWidth, nHeight), true);

		if (result) {
			m_nVideoWidth = nWidth;
			m_nVideoHeight = nHeight;

			initialize();
			CUtility::StartThread(threadWriteVideo, m_pThread, this);
		}
		else {
			return -1;
		}
	}
	return NOERROR;
}
void CVideoWriter::StopRecording() {
	CUtility::StopThread(m_pThread);
	m_videoWriter.release();
}

void CVideoWriter::PushToBuffer(cv::Mat image) {
	if (m_pThread == nullptr || !m_pThread->isRun) return;

	if (image.rows != m_nVideoHeight || image.cols != m_nVideoWidth) {
		cv::resize(image, m_pFrameQueue[m_nQueueEnd], m_pFrameQueue[m_nQueueEnd].size());
	}
	else {
		image.copyTo(m_pFrameQueue[m_nQueueEnd]);
	}
	m_nQueueEnd++;
	m_nQueueEnd = (m_nQueueEnd >= m_nQueueCount) ? 0 : m_nQueueEnd;
}

void CVideoWriter::initialize() {
	finalize();
	
	m_pFrameQueue = new cv::Mat[m_nQueueCount];
	for (int i = 0; i < m_nQueueCount; i++) {
		m_pFrameQueue[i].create(m_nVideoHeight, m_nVideoWidth, CV_8UC3);
	}	
}
void CVideoWriter::finalize() {
	if (m_pFrameQueue != NULL) {
		for (int i = 0; i < m_nQueueCount; i++) {
			m_pFrameQueue[i].release();
		}
		delete[] m_pFrameQueue;
		m_pFrameQueue = NULL;
	}
}
bool CVideoWriter::popFromBuffer(int& nPopIndex) {
	if (m_nQueueStart != m_nQueueEnd) {
		m_nQueueStart++;
		m_nQueueStart = (m_nQueueStart >= m_nQueueCount) ? 0 : m_nQueueStart;

		nPopIndex = m_nQueueStart;

		return true;
	}

	return false;
}
UINT CVideoWriter::threadWriteVideo(LPVOID param) {
	CVideoWriter* pWriter = (CVideoWriter*)param;
	cv::Mat* pMatQueue = pWriter->m_pFrameQueue;
	int nPopIndex = 0;

	while (pWriter->m_pThread->isRun) {
		if (pWriter->popFromBuffer(nPopIndex)) {
			pWriter->m_videoWriter.write(pMatQueue[nPopIndex]);
		}
	}
	while (pWriter->popFromBuffer(nPopIndex)) {
		pWriter->m_videoWriter.write(pMatQueue[nPopIndex]);
	}

	return NOERROR;
}