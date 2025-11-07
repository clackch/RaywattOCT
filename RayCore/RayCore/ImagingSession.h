#pragma once

#include "define.h"
#include "Config.h"
#include "Imaging.h"
#include <opencv2/opencv.hpp>
#include <map>
#include <numeric>

#define FILE_EXTENSION_RAW	"bin"
#define FILE_EXTENSION_OCT	"oct"
#define FILE_EXTENSION_TIF	"tif"

enum class ImagingType
{
	OCTImaging,
	LabImaging,
	TIFFImaging,
	Default = LabImaging
};

class CMessageService;
class COCTImaging;
class CCalibration;
class IDataManager;
class CThread;
class CCutViewManager;
class IRayLearning;
class CImagingSession
{
private:
	CMessageService* m_pMsg;
	int m_nSession;

	ImagingType m_imagingType;
	COCTImaging* m_pImaging;
	IDataManager* m_pDataManager;

	CThread* m_pThreadImaging;
	std::map<int, cv::Mat> m_mapImage;
	std::map<int, cv::Mat> m_mapImageWithoutCompensation;

	bool m_deleteData;

	CThread* m_pThreadUpdateCutView;
	CThread* m_pThreadObjectDetection;
	CThread* m_pThreadVolumeGeneration;;
	CCutViewManager* m_pCutView;

	struct Calcium {
		int angleNum = 0;
		std::vector<int> startAngle;
		std::vector<int> endAngle;
	};

	std::vector<std::vector<cv::Mat>> m_vLumen;
	std::vector<std::vector<cv::Mat>> m_vSidebranch;
	std::vector<cv::Mat> m_vStent;
	std::vector<cv::Mat> m_vGuidewire;
	std::vector<std::vector<float>> m_vGuidewireRadius;
	std::vector<Calcium> m_vCalcium;
	char* m_pVolumeData;

	int m_zOffset;
	std::vector<int> m_vZOffset;

private:
	CImagingSession(CMessageService* pMsg, int nSession, bool deleteData = true);
public:
	virtual ~CImagingSession();

	static CImagingSession* CreateSession(CMessageService* pMsg, int nSession, IImaging::Setting setting, IDataManager *pWriter);
	static CImagingSession* CreateSession(CMessageService* pMsg, int nSession, const char* strFilePath, double imageResolution);
	static COCTImaging* CreateColorImaging(CMessageService* msg, IImaging::Setting setting, IDataManager* pData, ImagingType type);

	ImagingType GetImagingType() { return m_imagingType; }
	IDataManager* GetDataManager() { return m_pDataManager; }
	COCTImaging* GetImaging() { return m_pImaging; }
	CCutViewManager* GetCutView() { return m_pCutView; }
	char* GetVolumeData() { return m_pVolumeData; }

	// Asynchronous functions
	RayError Start();
	RayError Stop();
	void StopThreadForRestart();
	void StartCutViewUpdate(cv::Scalar backgroundColor);
	void StartObjectDetection();
	void StartVolumeGeneration();
	bool IsProcessed(int nFrame);

	// Synchronous functions
	cv::Mat PostProcess(int nFrame);
	UINT GetImageWidth();
	UINT GetImageHeight();
	UINT GetImageChannels();
	UINT GetImageDepth();
	void* GetImageData(int nFrame);
	void InitCutView(cv::Scalar backgroundColor);
	UINT GetCutViewWidth();
	UINT GetCutViewHeight();
	UINT GetCutViewChannels();
	void AddFramesIntoCutView();
	void* GetLumenContour(int nFrame);
	int GetNumOfLumenContourPoints(int nFrame);
	int GetNumOfSidebranchContourSize(int nFrame);
	void* GetSidebranchContour(int nFrame, int nSb);
	int GetNumOfSidebranchContourPoints(int nFrame, int nSb);
	void* GetStentPoints(int nFrame);
	int GetNumOfStentPoints(int nFrame);
	void* GetGuidewirePoints(int nFrame);
	int GetNumOfGuidewirePoints(int nFrame);
	void* GetGuidewireRadius(int nFrame);
	void* GetCalciumAngles(int nFrame);
	int GetCalciumLength(int nFrame);

	bool LoadZOffset(const char* strDataFilePath);
	void SetZOffset(int zOffset) { m_zOffset = zOffset; }
	int GetZOffset() { return m_zOffset; }
	int GetZOffset(int nFrame);

	static std::vector<cv::Point> GetValidLumenContour(const cv::Mat& imageResultWithoutCompensation, int imgSize, const cv::Mat& centerMask, const cv::Ptr<cv::CLAHE>& clahe, COCTImaging *pImaging);
	static int IsLumenNormal(cv::Mat image, std::vector<cv::Point> contour, double lumenThresholdMin, double lumenThresholdMax, double lumenSnrThreshold, bool showLumenGuide);

private:
	static CImagingSession* createSession(CMessageService* pMsg, IImaging::Setting setting, int nSession, IDataManager* pData, bool deleteData, ImagingType type);
	static UINT threadImaging(LPVOID param);
	static UINT threadUpdateCutView(LPVOID param);	
	static UINT threadDetectObject(LPVOID param);
	static UINT threadGenerateVolume(LPVOID param);
	static USHORT* readBackground(const char* strBackgroundFile, IImaging::Setting setting);
};

