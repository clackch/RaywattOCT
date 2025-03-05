#pragma once
#include <opencv2/opencv.hpp>
#include <vector>

class CLookUpTable
{
private:
	std::vector<std::vector<cv::Vec3b>> m_vLUT;
	int m_fCurrentColorMapIndex = 0;
	bool m_enhancedLUTApplied = false;

private:
	CLookUpTable();
	CLookUpTable(const CLookUpTable& ref) {};
	CLookUpTable& operator=(const CLookUpTable& ref) {};
	~CLookUpTable();

public:
	static CLookUpTable& GetInstance();
	int Load(const char* strLUTPath, bool isTest = false);
	void Apply(cv::Mat& image, uint nIdxLUT);
	void Revert(cv::Mat image, uint nIdxLUT, cv::Mat& image1ch);
	void SetCurrentColormap(int colomapIndex);
	int GetCurrentColormap();
	void SetEnhancedLUT(bool applyOrNot);
	bool GetEnhancedLUT();
};

