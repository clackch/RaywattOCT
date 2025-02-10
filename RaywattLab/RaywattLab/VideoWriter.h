#pragma once
#include <opencv2/opencv.hpp>

class CThread;
class CVideoWriter
{
private:
	CThread* m_pThread;
	CRITICAL_SECTION m_csQueue;

	cv::VideoWriter m_videoWriter;
	int m_nVideoWidth;
	int m_nVideoHeight;

	std::vector<cv::Mat> m_vFrameQueue;
	unsigned int m_nSavedFrame;
public:
	CVideoWriter();
	virtual ~CVideoWriter();

	int StartRecording(CString strFilePath, int nWidth, int nHeight);
	void StopRecording();
	void PushToBuffer(cv::Mat image);

private:
	void finalize();
	bool popFromBuffer(int& nPopIndex);
	static UINT threadWriteVideo(LPVOID param);
};

