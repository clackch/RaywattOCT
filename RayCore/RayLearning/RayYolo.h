#pragma once
#include "IRayLearning.h"
#include <Windows.h>
#include <iostream>
#include <opencv2/opencv.hpp>

typedef void* (*pInitializeSegment)();
typedef void* (*pInitializeDetect)();
typedef void* (*pGetSegmentObjects)(void*, cv::Mat);
typedef void* (*pGetDetectObjects)(void*, cv::Mat);

class CRayYolo : public IRayLearning
{
public:
	CRayYolo();
	virtual ~CRayYolo();
	void Initialize(bool useGPU) override;
	cv::Mat FindLumen(cv::Mat image) override;
	cv::Mat FindSidebranch() override;
	std::vector<cv::Rect2f> FindStent(cv::Mat image) override;
	std::vector<cv::Rect2f> FindGuidewire() override;

private:
	HMODULE m_hDll;
	void* m_yoloSegment;
	void* m_yoloDetect;
	cv::Mat m_mapLumen;
	cv::Mat m_mapSidebranch;
	std::vector<cv::Rect2f> m_vStent;
	std::vector<cv::Rect2f> m_vGuidewire;
	pInitializeSegment InitializeSegment;
	pInitializeDetect InitializeDetect;
	pGetSegmentObjects GetSegmentObjects;
	pGetDetectObjects GetDetectObjects;
	void SegmentObjects(cv::Mat image);
	void DetectObjects(cv::Mat image);
};
