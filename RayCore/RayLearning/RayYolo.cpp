#include "RayYolo.h"

CRayYolo::CRayYolo() {
	m_hDll = LoadLibrary(L"RayYolo.dll");
}

CRayYolo::~CRayYolo() {
	if (m_hDll) {
		FreeLibrary(m_hDll);
	}
}

void CRayYolo::Initialize(bool useGPU){
	if (m_hDll) {
		InitializeSegment = (pInitializeSegment)GetProcAddress(m_hDll, "InitializeSegment");
		InitializeDetect = (pInitializeDetect)GetProcAddress(m_hDll, "InitializeDetect");
		GetSegmentObjects = (pGetSegmentObjects)GetProcAddress(m_hDll, "GetSegmentObjects");
		GetDetectObjects = (pGetDetectObjects)GetProcAddress(m_hDll, "GetDetectObjects");

		m_yoloSegment = InitializeSegment();
		m_yoloDetect = InitializeDetect();
	}
}

cv::Mat CRayYolo::FindLumen(cv::Mat image){

	SegmentObjects(image);

	return m_mapLumen;
}

cv::Mat CRayYolo::FindSidebranch(){

	return m_mapSidebranch;
}

std::vector<cv::Rect2f> CRayYolo::FindStent(cv::Mat image)
{
	DetectObjects(image);

	return m_vStent;
}

std::vector<cv::Rect2f> CRayYolo::FindGuidewire()
{
	return m_vGuidewire;
}

void CRayYolo::SegmentObjects(cv::Mat image)
{
	std::map<int, cv::Mat>* mapSegment = static_cast<std::map<int, cv::Mat>*>(GetSegmentObjects(m_yoloSegment, image));

	m_mapLumen.release();
	m_mapSidebranch.release();

	std::map<int, cv::Mat>::iterator iterSegment;
	for (iterSegment = mapSegment->begin(); iterSegment != mapSegment->end(); iterSegment++) {
		
		if (iterSegment->first == 0) {//lumen
			m_mapLumen = iterSegment->second;
		}
		else if (iterSegment->first == 1) {//sidebranch
			m_mapSidebranch = iterSegment->second;
		}
	}
}

void CRayYolo::DetectObjects(cv::Mat image)
{
	std::map<int, std::vector<cv::Rect2f>>* mapObject = static_cast<std::map<int, std::vector<cv::Rect2f>>*>(GetDetectObjects(m_yoloDetect, image));

	m_vStent.clear();
	m_vGuidewire.clear();

	std::map<int, std::vector<cv::Rect2f>>::iterator iterDetect;
	for (iterDetect = mapObject->begin(); iterDetect != mapObject->end(); iterDetect++) {
		
		if (iterDetect->first == 0) {
			m_vStent = iterDetect->second;
		}
		else if (iterDetect->first == 1) {
			m_vGuidewire = iterDetect->second;
		}
	}
}
