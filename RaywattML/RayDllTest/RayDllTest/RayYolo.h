#pragma once
#include <Windows.h>
#include <iostream>
#include <opencv2/opencv.hpp>

typedef void* (*pInitializeSegment)();
typedef void* (*pInitializeDetect)();
typedef void* (*pGetSegmentObjects)(void*, cv::Mat);
typedef void* (*pGetDetectObjects)(void*, cv::Mat);

class RayYolo
{
public:
	void LoadDLL();
	void FreeDLL();
	pInitializeSegment InitializeSegment;
	pInitializeDetect InitializeDetect;
	pGetSegmentObjects GetSegmentObjects;
	pGetDetectObjects GetDetectObjects;

private:
	HMODULE hDll;
};
