#include "RayYolo.h"

void RayYolo::LoadDLL()
{
	std::string path = "RayYolo.dll";

	hDll = LoadLibraryA(path.c_str());

	if (hDll == nullptr)
		return;

	InitializeSegment = (pInitializeSegment)GetProcAddress(hDll, "InitializeSegment");

	InitializeDetect = (pInitializeDetect)GetProcAddress(hDll, "InitializeDetect");

	GetSegmentObjects = (pGetSegmentObjects)GetProcAddress(hDll, "GetSegmentObjects");
	
	GetDetectObjects = (pGetDetectObjects)GetProcAddress(hDll, "GetDetectObjects");
}

void RayYolo::FreeDLL()
{
	FreeLibrary(hDll);
}






