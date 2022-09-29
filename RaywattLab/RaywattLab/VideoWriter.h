#pragma once
#include <opencv2/opencv.hpp>

class CThread;
class CVideoWriter
{
private:
	CThread* m_pThread;

	cv::VideoWriter m_videoWriter;
	int m_nVideoWidth;
	int m_nVideoHeight;

	cv::Mat* m_pFrameQueue;
	unsigned int m_nQueueCount;
	unsigned int m_nQueueStart;
	unsigned int m_nQueueEnd;
public:
	CVideoWriter();
	virtual ~CVideoWriter();

	int StartRecording(CString strFilePath, int nWidth, int nHeight);
	void StopRecording();
	void PushToBuffer(cv::Mat image);

private:
	void initialize();
	void finalize();
	bool popFromBuffer(int& nPopIndex);
	static UINT threadWriteVideo(LPVOID param);
};

