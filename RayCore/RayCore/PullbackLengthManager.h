#pragma once
#include <map>
#include <DataWriter.h>

class PullbackLengthManager : public CDataWriter
{
private:
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

	void CutPullbackLength(int pullbackType);
	void SetSMProfile(int SMProfile) { m_SMProfile = SMProfile; }
	
private:
	void ReadAccelDecelPofileParameter();
	void SkipFrames(int stopRecordedFrames, int pullbackType, int rotationRatio);
	void CompactByKeepMask(const std::vector<bool>& keepMask);

};

