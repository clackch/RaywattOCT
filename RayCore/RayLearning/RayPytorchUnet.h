#pragma once
#include "IRayLearning.h"
#include <torch/script.h>
#include <memory>

class CRayTorchUnet : public IRayLearning {
private:
	torch::jit::script::Module m_model;

public:
	CRayTorchUnet();
	~CRayTorchUnet();
	virtual void Initialize(bool useGPU);
	virtual cv::Mat FindLumen(cv::Mat image);
};