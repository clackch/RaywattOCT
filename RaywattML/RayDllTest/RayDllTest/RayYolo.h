#pragma once
#include <Windows.h>
#include <iostream>
#include <opencv2/opencv.hpp>

typedef void* (*pInitializeSegment)();
typedef void* (*pInitializeDetect)();
typedef void* (*pInitializeCalciumSegment)();
typedef void* (*pGetSegmentObjects)(void*, cv::Mat);
typedef void* (*pGetDetectObjects)(void*, cv::Mat);
typedef void* (*pGetCalciumSegmentObjects)(void*, cv::Mat);

class RayYolo
{
public:
	void LoadDLL();
	void FreeDLL();
	pInitializeSegment InitializeSegment;
	pInitializeDetect InitializeDetect;
	pInitializeCalciumSegment InitializeCalciumSegment;
	pGetSegmentObjects GetSegmentObjects;
	pGetDetectObjects GetDetectObjects;
	pGetCalciumSegmentObjects GetCalciumSegmentObjects;

private:
	HMODULE hDll;
};
