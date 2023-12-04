#pragma once
#include "CompNet.h"
#include "IRayLearning.h"
#include <opencv2/opencv.hpp>

class CRayCompNet : public IRayLearning {
private:
	CompNet m_compNet;

public:
	CRayCompNet();
	virtual ~CRayCompNet();
	void Initialize(bool useGPU) override;
	cv::Mat FindLumen(cv::Mat image) override;
};