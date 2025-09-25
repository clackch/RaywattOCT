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
int CLookUpTable::Load(const char* strLUTPath, bool isTest) {
	FILE* fpLUT = fopen(strLUTPath, "r");
	if (fpLUT) {
		char tempBuffer[256] = { 0 };
		std::string strBuffer;
		bool bStartLUT = false;
		std::vector<cv::Vec3b> lut;

		while (fgets(tempBuffer, sizeof(tempBuffer), fpLUT)) {
			tempBuffer[sizeof(tempBuffer) - 1] = '\0';
			strBuffer = std::string(tempBuffer);
			if (!strBuffer.empty() && strBuffer.back() == '\n') {
				strBuffer.pop_back();
			}
			if (strBuffer.empty()) continue;

			if (strBuffer[0] == '0') {
				bStartLUT = true;
			}

			if (bStartLUT) {
				std::stringstream buffer(strBuffer);
				std::string token;
				cv::Vec3b color{0,0,0};

				std::getline(buffer, token, ',');  // index
				for (int i = 0; i < 3; i++) {
					if (!std::getline(buffer, token, ',')) break;

					try {
						int value = std::stoi(token);

						if (value < 0 || value > 255) {
							PLOGI.printf("Warning: LUT value out of uchar range (%d)\n", value);
							value = 0;
						}
						color[i] = static_cast<uchar>(value);
					}
					catch (...) {
						PLOGI.printf("Warning: LUT value is not a number (%s)\n", token.c_str());
						int value = 0;
						color[i] = static_cast<uchar>(value);
					}
					
				}
				lut.push_back(color);
			}
		}

		fclose(fpLUT);

		if (lut.size() == 256 && !isTest) {
			m_vLUT.push_back(lut);
			return static_cast<int>(m_vLUT.size());
		}
		else {
			if (m_vLUT.size() < 4 || lut.empty()) return 0;
			m_vLUT.at(3) = lut;
			return static_cast<int>(m_vLUT.size());
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

/*
* image: CV_8U3C Format
* nIdxLUT: LUT Index
* image1ch: empty or CV_8UC1 Format
*/
void CLookUpTable::Revert(cv::Mat image, uint nIdxLUT, cv::Mat& image1ch) {
	if (image1ch.empty()) {
		image1ch = cv::Mat(cv::Size(image.cols, image.rows), CV_8UC1);
	}

	for (int y = 0; y < image.rows; y++) {
		for (int x = 0; x < image.cols; x++) {
			cv::Vec3b color = image.at<cv::Vec3b>(y, x);

			char idxColor = 0;
			for (int idx = 0; idx < m_vLUT.at(nIdxLUT).size(); idx++) {
				cv::Vec3b bgrColor = m_vLUT.at(nIdxLUT).at(idx);

				if (bgrColor.val[0] == color.val[2] &&
					bgrColor.val[1] == color.val[1] &&
					bgrColor.val[2] == color.val[0]) {
					idxColor = idx;
					break;
				}
			}

			image1ch.at<char>(y, x) = idxColor;
		}
	}
}

void CLookUpTable::SetCurrentColormap(int colormapIndex) {
	this->m_fCurrentColorMapIndex = colormapIndex;
}

int CLookUpTable::GetCurrentColormap() {
	return this->m_fCurrentColorMapIndex;
}

void CLookUpTable::SetEnhancedLUT(bool applyOrNot) {
	this->m_enhancedLUTApplied = applyOrNot;
}

bool CLookUpTable::GetEnhancedLUT() {
	return this->m_enhancedLUTApplied;
}