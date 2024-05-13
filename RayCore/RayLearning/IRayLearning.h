#pragma once
#include "Config.h"
#include "CompNet.h"
#include <opencv2/opencv.hpp>
#include <vector>

#define interface class

interface IRayLearning {
protected:
	bool m_useGPU;

protected:
	IRayLearning();

public:
	static IRayLearning* GetInstance();
	virtual ~IRayLearning();
	virtual void Initialize(bool useGPU) = 0;
	virtual cv::Mat FindLumen(cv::Mat image) = 0;
	virtual cv::Mat FindSidebranch() = 0;
	virtual cv::Mat FindCalcium(cv::Mat image) = 0;
	virtual std::vector<cv::Rect2f> FindStent(cv::Mat image) = 0;
	virtual std::vector<cv::Rect2f> FindGuidewire() = 0;
};
