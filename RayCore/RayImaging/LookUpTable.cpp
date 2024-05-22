#include "LookUpTable.h"
#include "Config.h"

CLookUpTable::CLookUpTable()
{
}

CLookUpTable::~CLookUpTable()
{
}

CLookUpTable& CLookUpTable::GetInstance() {
	static CLookUpTable pInstance;
	return pInstance;
}

/*
* return: Size of LUT list or zero if loading from file is failed.
*/
int CLookUpTable::Load(const char* strLUTPath) {
	std::vector<cv::Vec3b> lut;

	FILE* fpLUT = fopen(strLUTPath, "r");
	if (fpLUT) {
		char strBuffer[MAX_PATH];
		bool bStartLUT = false;
		while (fscanf(fpLUT, "%s", strBuffer) != EOF) {
			if (strBuffer[0] == '0') {
				bStartLUT = true;
			}

			if (bStartLUT) {
				std::stringstream buffer(strBuffer);
				std::string token;
				cv::Vec3b color;

				std::getline(buffer, token, ',');	// index
				for (int i = 0; i < 3; i++) {
					std::getline(buffer, token, ',');
					color.val[i] = atoi(token.c_str());
				}
				lut.push_back(color);
			}
		}
		if (lut.size() == 256) {
			m_vLUT.clear();
			m_vLUT.push_back(lut);
			return m_vLUT.size();
		}
	}
	
	return 0;
}

/*
* image: CV_8U3C Format
* nIdxLUT: LUT Index
*/
void CLookUpTable::Apply(cv::Mat& image, uint nIdxLUT) {
	if (nIdxLUT >= m_vLUT.size()) return;

	for (int y = 0; y < image.rows; y++) {
		for (int x = 0; x < image.cols; x++) {
			cv::Vec3b color = image.at<cv::Vec3b>(y, x);
			cv::Vec3b bgrColor = m_vLUT.at(nIdxLUT).at(color.val[0]);
			cv::Vec3b cvtColor;

			cvtColor.val[0] = bgrColor.val[2];
			cvtColor.val[1] = bgrColor.val[1];
			cvtColor.val[2] = bgrColor.val[0];

			image.at<cv::Vec3b>(y, x) = cvtColor;
		}
	}
}