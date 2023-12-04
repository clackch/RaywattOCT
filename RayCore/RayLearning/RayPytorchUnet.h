#pragma once
#include "IRayLearning.h"
#include <torch/script.h>
#include <memory>

class CRayTorchUnet : public IRayLearning {
private:
	torch::jit::script::Module m_model;

public:
	CRayTorchUnet();
	virtual ~CRayTorchUnet();
	void Initialize(bool useGPU) override;
	cv::Mat FindLumen(cv::Mat image) override;
};