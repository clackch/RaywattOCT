#include "RayYolo.h"

void RayYolo::LoadDLL()
{
	std::string path = "RayYolo.dll";

	hDll = LoadLibraryA(path.c_str());

	if (hDll == nullptr)
		return;

	InitializeSegment = (pInitializeSegment)GetProcAddress(hDll, "InitializeSegment");

	InitializeDetect = (pInitializeDetect)GetProcAddress(hDll, "InitializeDetect");

	InitializeCalciumSegment = (pInitializeCalciumSegment)GetProcAddress(hDll, "InitializeCalciumSegment");

	GetSegmentObjects = (pGetSegmentObjects)GetProcAddress(hDll, "GetSegmentObjects");
	
	GetDetectObjects = (pGetDetectObjects)GetProcAddress(hDll, "GetDetectObjects");

	GetCalciumSegmentObjects = (pGetCalciumSegmentObjects)GetProcAddress(hDll, "GetCalciumSegmentObjects");
}

void RayYolo::FreeDLL()
{
	FreeLibrary(hDll);
}






