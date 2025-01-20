#include "TIFFImaging.h"
#include "LookUpTable.h"

CTIFFImaging::CTIFFImaging(Setting setting, CMessageService* pMsg)
	: COCTImaging(setting, pMsg) 
{
}
CTIFFImaging::~CTIFFImaging() 
{
}

void CTIFFImaging::Initialize()
{
	m_nWidth = m_setting.nAScan;
	m_nHeight = m_setting.nBScan;
	m_nChannels = 3;	// RGB

	imageCircle.create(m_setting.nBScan, m_setting.nAScan, CV_8UC3);
	imageConvert.create(m_setting.nBScan, m_setting.nAScan, CV_8UC1);
	imageOrigin.create(m_setting.nBScan, m_setting.nAScan, CV_8UC1);
	imageMask.create(m_setting.nBScan, m_setting.nAScan, CV_8UC1);
	memset(imageMask.data, 0x00, m_setting.nBScan * m_setting.nAScan);
	cv::circle(imageMask, cv::Point(imageMask.cols / 2, imageMask.rows / 2), imageMask.cols / 2, cv::Scalar(0xff, 0xff, 0xff), -1);

	initCircularizeMap(m_setting.nAScan, m_setting.nBScan, m_setting.nAScan, m_setting.nBScan, m_setting.nAScan, 2.0f);
}

void CTIFFImaging::Process(char* fringes)
{
	m_end = std::chrono::system_clock::now();
	CLookUpTable& lut = CLookUpTable::GetInstance();
	cv::Mat imgTIFF(cv::Size(m_setting.nBScan, m_setting.nAScan), CV_8UC1, fringes);
	imageConvert = imgTIFF.clone();

	m_start = m_end;

	InverseCircularizeImage(imageConvert, imageConvert);

	imageResultWithoutCompensation = imageConvert.clone();
}

void CTIFFImaging::PostProcess(cv::Mat image)
{
	const bool bColor = m_bColor;

	cv::cvtColor(image, imageCircle, cv::COLOR_GRAY2RGB);
	if (bColor) {
		CLookUpTable& lut = CLookUpTable::GetInstance();
		if (lut.GetEnhancedLUT()) {
			lut.Apply(imageCircle, 3 /*LUT_enhanced.csv*/);
			lut.Apply(imageCircle, lut.GetCurrentColormap());
		}
		else {
			lut.Apply(imageCircle, lut.GetCurrentColormap());
		}
	}

	cv::convertScaleAbs(imageCircle, imageCircle, m_setting.contrast, m_setting.brightness);

	CircularizeImage(imageCircle, imageCircle);
}


void CTIFFImaging::initCircularizeMap(int diameter, int srcHeight, int srcWidth, int dstHeight, int dstWidth, double scale) {
	COCTImaging::initCircularizeMap(diameter, srcHeight, srcWidth, dstHeight, dstWidth, scale);
	PLOGI.printf("TIFFImageing initCircularize Map Start");

	double radius = (diameter / 2) - 0.5f;
	inverseMatXMap.create(dstHeight, dstWidth, CV_32FC1);
	inverseMatYMap.create(dstHeight, dstWidth, CV_32FC1);

	inverseMatXMap.setTo(cv::Scalar::all(0));
	inverseMatYMap.setTo(cv::Scalar::all(0));

	for (int y = 0; y < dstHeight; y++)
	{
		for (int x = 0; x < dstWidth; x++)
		{
			float r = (float)(srcWidth - y) / scale;
			float theta = ((float)x / srcHeight) * 2 * CV_PI;

			float fx = r * cos(theta) + radius;
			float fy = r * sin(theta) + radius;

			inverseMatXMap.at<float>(y, x) = fx;
			inverseMatYMap.at<float>(y, x) = fy;
		}
	}

	PLOGI.printf("TIFFImageing initCircularize Map Done");
}

void CTIFFImaging::InverseCircularizeImage(cv::Mat& src, cv::Mat& dst) {
	dst = src.clone();
	cv::remap(dst, dst, inverseMatXMap, inverseMatYMap, cv::INTER_LINEAR);

	cv::rotate(dst, dst, cv::ROTATE_90_COUNTERCLOCKWISE);
}

void CTIFFImaging::EraseStentOutLier(cv::Mat& stent) {
	int width = m_nWidth;
	int height = m_nHeight;

	// Lumen Offset 설정
	std::vector<cv::Point> LumenOffsetPoints;
	for (int i = 0; i < height; i++) {
		LumenOffsetPoints.push_back(cv::Point(0, 0));
	}
	GetLumenOffsetPoints(LumenOffsetPoints);

	for (int i = 0; i < LumenOffsetPoints.size(); i++) {
		cv::Point point = LumenOffsetPoints[i];
	}
	
	cv::Mat blackImage = cv::Mat::zeros(height, width, CV_8UC1);

	for (int row = 0; row < stent.rows; row++) {
		cv::Point point = stent.at<cv::Point>(row, 0);
		if (point.x >= 0 && point.x < width && point.y >= 0 && point.y < height) {
			blackImage.at<uchar>(point.y, point.x) = 255;
		}
	}
	
	cv::Mat remappedImage;
	cv::remap(blackImage, remappedImage, inverseMatXMap, inverseMatYMap, cv::INTER_NEAREST);
	cv::rotate(remappedImage, remappedImage, cv::ROTATE_90_COUNTERCLOCKWISE);

	while (!stent.empty()) {
		stent.pop_back();
	}

	// Stent Outlier를 제외한 Stent Point만 Push
	for (int y = 0; y < height; y++) {
		for (int x = 0; x < width; x++) {
			if (remappedImage.at<uchar>(y, x) == 255 && LumenOffsetPoints[y].x < x) {
				stent.push_back(cv::Point(x, y));
			}
		}
	}

	blackImage = cv::Mat::zeros(height, width, CV_8UC1);
	for (int row = 0; row < stent.rows; row++) {
		cv::Point point = stent.at<cv::Point>(row, 0);
		if (point.x >= 0 && point.x < width && point.y >= 0 && point.y < height) {
			blackImage.at<uchar>(point.y, point.x) = 255;
		}
	}

	remappedImage = cv::Mat::zeros(height, width, CV_8UC1);
	cv::remap(blackImage, remappedImage, matXMap, matYMap, cv::INTER_NEAREST);
	cv::rotate(remappedImage, remappedImage, cv::ROTATE_90_CLOCKWISE);


	while (!stent.empty()) {
		stent.pop_back();
	}

	// 극좌표 변환된 Stent Push
	for (int y = 0; y < remappedImage.rows; y++) {
		for (int x = 0; x < remappedImage.cols; x++) {
			if (remappedImage.at<uchar>(y, x) == 255) {
				stent.push_back(cv::Point(x, y));
			}
		}
	}
	
	remappedImage.release();
	blackImage.release();
}

void CTIFFImaging::SetLumenContourOffset(std::vector<cv::Point> lumenContour) {
	if (lumenContour.empty()) {
		return;
	}

	int width = m_nWidth;
	int height = m_nHeight;

	std::vector<std::vector<cv::Point>> lumenContours;
	lumenContours.push_back(lumenContour);

	cv::Mat blackImage = cv::Mat::zeros(height, width, CV_8UC1);

	cv::drawContours(blackImage, lumenContours, -1, cv::Scalar(255), 1);

	cv::remap(blackImage, blackImage, inverseMatXMap, inverseMatYMap, cv::INTER_LINEAR);

	cv::rotate(blackImage, blackImage, cv::ROTATE_90_COUNTERCLOCKWISE);

	//Rectangle Contour의 각 Row에 해당하는 X좌표 설정 (평균값)
	inversedContourYPoints.clear();
	for (int y = 0; y < height; y++) {
		double sumOfx= 0;
		int count = 0;
		for (int x = 0; x < width; x++) {
			if (blackImage.at<uchar>(y, x) > 0) {
				sumOfx += x;
				count++;
			}
		}
		int avgX = (int)(sumOfx / count);
		if (avgX >= 0 && avgX < width) {
			inversedContourYPoints.push_back(cv::Point(avgX, y));
			cv::circle(blackImage, cv::Point(avgX, y), 1, cv::Scalar(200), - 1);
		}
		else {
			inversedContourYPoints.push_back(cv::Point(width - 1, y));
		}
	}
}

void CTIFFImaging::GetLumenOffsetPoints(std::vector<cv::Point>& lumenOffsetBoundary) {

	for (int i = 0; i < inversedContourYPoints.size(); i++) {
		cv::Point point = inversedContourYPoints[i];
		int x, y;
		x = point.x - 50;  //TODO - offset 값을 OCT Lumen 값 평균을 활용하여 그림자 영역 판별할 수 있는 Offset 만들기
		y = point.y;

		lumenOffsetBoundary[i] = cv::Point(x, y);
	}
}

void CTIFFImaging::GetGuideWireCenterPoint(cv::Mat image, std::vector<cv::Rect2f> GuideWires, std::vector<cv::Point>& centerPoints, std::vector<double>& radius) {
	if (GuideWires.empty()) {
		return;
	}
	centerPoints.clear();
	radius.clear();

	std::vector<cv::Point> edgePoints;
	std::vector<double> theta;

	cv::Mat grayImage;
	switch (image.channels()) {
	case 3:
		cv::cvtColor(image, grayImage, cv::COLOR_BGR2GRAY);
		break;
	case 4:
		cv::cvtColor(image, grayImage, cv::COLOR_BGRA2GRAY);
		break;
	default:
		grayImage = image.clone();
	}

	GetGuideWireCircleEdgePoints(grayImage, GuideWires, edgePoints);
	GetGuideWireShadowPointAngles(grayImage, edgePoints, theta);

	if (theta[0] == 0) {
		centerPoints.push_back(cv::Point(-1, -1));
		radius.push_back(-1);
		return;
	}

	// edgePoints와 theta는 같은 인덱스끼리 매칭
	int centerX = image.cols / 2;
	int centerY = image.rows / 2;

	for (int i = 0; i < edgePoints.size(); i++) {
		cv::Vec2d edgeVector = cv::Vec2d(edgePoints[i].x - centerX, edgePoints[i].y - centerY);
		double centerToEdgePointDistance = std::sqrt((centerX - edgePoints[i].x) * (centerX - edgePoints[i].x) + (centerY - edgePoints[i].y) * (centerY - edgePoints[i].y));
		double guideWireRadius = std::abs(centerToEdgePointDistance * std::sin(theta[i]) / (1 - std::sin(theta[i]))); // radius = magnitude
		
		cv::Vec2d unitVector;
		if (edgeVector[0]/* X축 +방향 벡터 */ > 0) {
			unitVector = cv::Vec2d(1, 0);
		}
		else if (edgeVector[0]/* X축 -방향 벡터 */ < 0) {
			unitVector = cv::Vec2d(-1, 0);
		}
		else {
			PLOGI.printf("guideWire Edge Vector Abnormal");
			continue;
		}

		double angle;
		GetAcuteAngleToXAxis(edgeVector, unitVector, angle);

		int XDirection, YDirection;
		XDirection = edgeVector[0] / std::abs(edgeVector[0]);
		YDirection = edgeVector[1] / std::abs(edgeVector[1]);

		centerPoints.push_back(cv::Point((int)(edgePoints[i].x + XDirection * guideWireRadius * std::cos(angle)),
			(int)(edgePoints[i].y + YDirection * guideWireRadius * std::sin(angle))));
		radius.push_back(guideWireRadius);
	}
}

void CTIFFImaging::GetGuideWireCircleEdgePoints(cv::Mat grayImage, std::vector<cv::Rect2f> GuideWires, std::vector<cv::Point>& edgePoints) {
	int paddingSize = 1;

	for (const auto& rect : GuideWires) {
		// top 3 pixels에 대한 Mask 작업을 위한 Roi Padding 설정
		if (rect.x - paddingSize < 0 || rect.y - paddingSize < 0 || rect.x + rect.width + paddingSize > grayImage.cols || rect.y + rect.height + paddingSize > grayImage.rows) {
			continue;
		}

		cv::Rect roiRect(rect.x - paddingSize, rect.y - paddingSize, rect.width + paddingSize*2, rect.height + paddingSize*2);
		cv::Mat roi = grayImage(roiRect);

		cv::Mat roiInt;
		if (roi.type() != CV_8U) {
			roi.convertTo(roiInt, CV_8U);
		}
		else {
			roiInt = roi;
		}

		// Roi Padding 없는 기존 GuideWire Rectangle Roi
		cv::Rect originalRoiRect(paddingSize, paddingSize, rect.width, rect.height);
		cv::Mat originalRoi = roiInt(originalRoiRect);

		std::vector<std::pair<int, cv::Point>> pixelValues;
		for (int y = 0; y < originalRoi.rows; y++) {
			for (int x = 0; x < originalRoi.cols; x++) {
				pixelValues.emplace_back(originalRoi.at<unsigned char>(y, x), cv::Point(x, y));
			}
		}
		std::sort(pixelValues.begin(), pixelValues.end(), [](const std::pair<int, cv::Point>& a, const std::pair<int, cv::Point>& b) {
			return a.first > b.first;
			});

		std::vector<cv::Point> top3Points;
		for (int i = 0; i < 30 && i < pixelValues.size(); i++) {
			top3Points.push_back(pixelValues[i].second);
		}

		// top 3 pixels에 대한 3x3 분류 작업
		std::vector<std::pair<int, cv::Point>> avgValues;
		for (const auto& pt : top3Points) {
			int startX = pt.x;
			int startY = pt.y;
			int width = paddingSize * 2 + 1;
			int height = paddingSize * 2 + 1;

			cv::Rect region(startX, startY, width, height);
			cv::Mat regionMat = roiInt(region);

			int sum = cv::sum(regionMat)[0];
			int regionAvg = (int)((double)sum / (region.width * region.height));

			avgValues.emplace_back(regionAvg, pt);
		}

		auto maxAvgIt = std::max_element(avgValues.begin(), avgValues.end(),
			[](const std::pair<int, cv::Point>& a, const std::pair<int, cv::Point>& b) {
				return a.first < b.first;
			});

		if (maxAvgIt != avgValues.end()) {
			cv::Point maxAvgPoint = maxAvgIt->second + cv::Point(rect.x, rect.y);
			edgePoints.push_back(maxAvgPoint);
		}
	}
}

void CTIFFImaging::GetGuideWireShadowPointAngles(cv::Mat grayImage, std::vector<cv::Point> edgePoints, std::vector<double>& theta) {
	int height = m_nHeight;
	int width = m_nWidth;
	theta.clear();
	static int myint = 0;

	for (const auto& edgePoint : edgePoints) {
		cv::Mat cloneImage = grayImage.clone();

		cv::Mat mask = (cloneImage == 255);
		cloneImage.setTo(0, mask);
		cloneImage.at<uchar>(edgePoint.y, edgePoint.x) = 255;

		cv::Mat inversedImage;
		cv::remap(cloneImage, inversedImage, inverseMatXMap, inverseMatYMap, cv::INTER_NEAREST);
		cv::rotate(inversedImage, inversedImage, cv::ROTATE_90_COUNTERCLOCKWISE);

		cv::Point inversededgePoint;
		for (int y = 0; y < height; y++) {
			for (int x = 0; x < width; x++) {
				if (inversedImage.at<uchar>(y, x) == 255) {
					inversededgePoint = cv::Point(x, y);
					break;
				}
			}
		}
		int startY, startX, endX, direction;
		direction = inversededgePoint.y - 100 < 0 ? 1 : -1;

		startY = inversededgePoint.y;
		startX = 0;
		endX = inversededgePoint.x - 100 < 0 ? inversededgePoint.x / 2 : inversededgePoint.x - 100;

		//GuideWire 중심점 row에 대한 pixel Value 합
		double sumOfStandardValue = 0;
		for (int x = startX; x <= endX; x++) {
			sumOfStandardValue += inversedImage.at<uchar>(inversededgePoint.y, x);
		}

		double gap = 0;
		int series = 0;
		for (int y = startY; y > 0 && y < height; y+= direction, gap+= 1.0 ) {
			int sumOfPixelValues = 0;
			for (int x = startX; x <= endX; x++) {
				sumOfPixelValues += inversedImage.at<uchar>(y, x);
			}

			if (sumOfPixelValues >= sumOfStandardValue * 2.5) {
				series++;
			}

			if (series == 3) {
				theta.push_back((360.0 / m_nHeight * gap) * CV_PI / 180);
				return;
			}
		}

		theta.push_back(0);
	}
}

void CTIFFImaging::GetCircularizeTransformPoint(cv::Point src, cv::Point& dst) {
	int diameter = m_setting.nAScan;

	// 평행이동 값
	int dx = diameter / 2;
	int dy = diameter / 2;
	
	// 좌표 변환 값
	double r = (diameter - src.x) / 2;
	double theta = 360 / diameter * src.y;
	double scale = 1 / std::abs(std::cos(theta));
	
	// 좌표변환 식
	int fx = (int)std::round(scale * r * std::cos(theta) + dx);
	int fy = (int)std::round(scale * r * std::sin(-theta) + dy);
	
	dst = cv::Point(fx, fy);
}

void CTIFFImaging::GetAcuteAngleToXAxis(cv::Vec2d vector1, cv::Vec2d vector2, double& angle) {
	// 벡터 크기 계산
	double vector1Magnitude = std::sqrt(vector1[0] * vector1[0] + vector1[1] * vector1[1]);
	double vector2Magnitude = std::sqrt(vector2[0] * vector2[0] + vector2[1] * vector2[1]);

	// 벡터와 X축 간의 내적 계산
	double dotProduct = vector1[0] * vector2[0] + vector1[1] * vector2[1];

	// 코사인 각도 계산
	double cosTheta = dotProduct / (vector1Magnitude* vector2Magnitude);

	// 각도 계산 (라디안)
	angle = std::acos(cosTheta);
}
