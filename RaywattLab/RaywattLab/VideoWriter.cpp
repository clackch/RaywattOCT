#include "pch.h"
#include "VideoWriter.h"
#include "Utility.h"

CVideoWriter::CVideoWriter() {
	m_pThread = NULL;

	m_nVideoWidth = 0;
	m_nVideoHeight = 0;
	
	m_nSavedFrame = 0;
}
CVideoWriter::~CVideoWriter() {
	StopRecording();
	finalize();
}

int CVideoWriter::StartRecording(CString strFilePath, int nWidth, int nHeight) {
	if (m_pThread == NULL) {
		char strVideoFilePath[MAX_PATH];
		int nFrameRate = 10;
		wcstombs(strVideoFilePath, strFilePath.GetBuffer(), MAX_PATH);

		bool result = m_videoWriter.open(strVideoFilePath, cv::CAP_ANY,	cv::VideoWriter::fourcc('A','V','I','1'), nFrameRate, cv::Size(nWidth, nHeight), true);

		if (result) {
			m_nVideoWidth = nWidth;
			m_nVideoHeight = nHeight;

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

	cv::Mat imgCopy;
	if (image.rows != m_nVideoHeight || image.cols != m_nVideoWidth) {
		cv::resize(image, imgCopy, cv::Size(m_nVideoWidth, m_nVideoHeight));
	}
	else {
		imgCopy = image.clone();
	}
	m_vFrameQueue.push_back(imgCopy);
}

void CVideoWriter::finalize() {
	for (int i = 0; i < m_vFrameQueue.size(); i++) {
		m_vFrameQueue[i].release();
	}
	m_vFrameQueue.clear();
	m_nSavedFrame = 0; 
}
bool CVideoWriter::popFromBuffer(int& nPopIndex) {
	if (m_nSavedFrame != m_vFrameQueue.size()) {
		nPopIndex = m_nSavedFrame;
		m_nSavedFrame++;

		return true;
	}

	return false;
}
UINT CVideoWriter::threadWriteVideo(LPVOID param) {
	CVideoWriter* pWriter = (CVideoWriter*)param;
	int nPopIndex = 0;

	while (pWriter->m_pThread->isRun) {
		if (pWriter->popFromBuffer(nPopIndex)) {
			pWriter->m_videoWriter.write(pWriter->m_vFrameQueue[nPopIndex]);
		}
	}
	while (pWriter->popFromBuffer(nPopIndex)) {
		pWriter->m_videoWriter.write(pWriter->m_vFrameQueue[nPopIndex]);
	}

	return NOERROR;
}