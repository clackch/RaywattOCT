#pragma once
#include "IRayLearning.h"
#include <Windows.h>
#include <iostream>
#include <opencv2/opencv.hpp>

typedef void* (*pInitializeSegment)();
typedef void* (*pInitializeDetect)();
typedef void* (*pInitializeCalciumSegment)();
typedef void* (*pGetSegmentObjects)(void*, cv::Mat);
typedef void* (*pGetDetectObjects)(void*, cv::Mat);
typedef void* (*pGetCalciumSegmentObjects)(void*, cv::Mat);

class CRayYolo : public IRayLearning
{
public:
	CRayYolo();
	virtual ~CRayYolo();
	void Initialize(bool useGPU) override;
	cv::Mat FindLumen(cv::Mat image) override;
	cv::Mat FindSidebranch() override;
	cv::Mat FindCalcium(cv::Mat image) override;
	std::vector<cv::Rect2f> FindStent(cv::Mat image) override;
	std::vector<cv::Rect2f> FindGuidewire() override;

private:
	HMODULE m_hDll;
	void* m_yoloSegment;
	void* m_yoloDetect;
	void* m_yoloCalciumSegment;
	cv::Mat m_mapLumen;
	cv::Mat m_mapSidebranch;
	cv::Mat m_mapCalcium;
	std::vector<cv::Rect2f> m_vStent;
	std::vector<cv::Rect2f> m_vGuidewire;
	pInitializeSegment InitializeSegment;
	pInitializeDetect InitializeDetect;
	pInitializeCalciumSegment InitializeCalciumSegment;
	pGetSegmentObjects GetSegmentObjects;
	pGetDetectObjects GetDetectObjects;
	pGetCalciumSegmentObjects GetCalciumSegmentObjects;
	void SegmentObjects(cv::Mat image);
	void DetectObjects(cv::Mat image);
	void CalciumSegmentObjects(cv::Mat image);
};
