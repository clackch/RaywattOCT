#pragma once
#include "Config.h"
#include "CompNet.h"
#include <opencv2/opencv.hpp>

class CRayLearning {
private:
	CompNet m_compNet;
	bool m_useGPU;

public:
	CRayLearning();
	virtual ~CRayLearning();

	void Initialize(bool useGPU);
	vector<vector<cv::Point>> FindLumen(cv::Mat image);
};