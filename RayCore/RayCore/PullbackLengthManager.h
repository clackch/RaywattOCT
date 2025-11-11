#pragma once
#include <map>
#include <DataWriter.h>
#include <cmath>
#include <climits>
#include <sstream>

class PullbackLengthManager : public CDataWriter
{
private:
	struct MaskResult {
		std::vector<bool> keepMask;
		int skipCount = 0;
	};

	enum class PullbackType {
		HISH_20_60 = 0,  // Speed_Distance
		HILO_40_100,
		STSH_60_60,
		STLO_100_100,
		FAST_120_60
	};

	typedef struct PullbackImageSkipParameters {
		double a, b, c;
		double threshold;
		int frameNum;
	}PISP;

	PISP m_pisp[2][5];
	int m_SMProfile;
public:
	PullbackLengthManager();
	~PullbackLengthManager();

	void CutPullbackLength(int pullbackType, int rotationSpeed = 24038);
	void SetSMProfile(int SMProfile) { m_SMProfile = SMProfile; }

private:
	void ReadAccelDecelPofileParameter();
	void SkipFrames(int stopRecordedFrames, int pullbackType, int rotationRatio, int extraFrameNum);
	void CompactByKeepMask(const std::vector<bool>& keepMask);
	int QuantizeExtraFrames(int extraFrameNumRaw, double rotationRatio);
	MaskResult BuildMaskWithThresholdScale(
		int stopRecordedFrames,
		int pullbackType,
		int rotationRatio,
		int desiredSkipHalfLen,       // extraFrameNum/2 범위가 아니라 halfLen으로 사용
		double thresholdScale,
		double A, double B, double C, double threshold,
		int nSamples);
};

