#include "PullbackLengthManager.h"
#include "Config.h"
#include "fstream"
#undef min
#undef max
#include <algorithm>
#include <cstring>

PullbackLengthManager::PullbackLengthManager() {
	if(m_nNumOfSamples != 0)
		m_nNumOfSamples = 0;
	ReadAccelDecelPofileParameter();
}

PullbackLengthManager::~PullbackLengthManager() {

}


void PullbackLengthManager::CutPullbackLength(int pullbackType, int rotationSpeed) {
	PLOGI.printf("CutPullbackLength Start");

	int rotationRatio = (int)(24038.0/rotationSpeed);  // 1 : 400rps, 2 : 200rps, 4 : 100rps
	int stopFrames = 3 / rotationRatio; /*Default Stop Frames*/
	int maxFrames = 0;
	int extraFrameNum = 0;

	switch (pullbackType) {
	case (int)PullbackType::HISH_20_60:
		extraFrameNum = m_nNumOfSamples - 1200 /*400 rps * 3sec*/ / rotationRatio;
		break;
	case (int)PullbackType::HILO_40_100:
		extraFrameNum = m_nNumOfSamples - 1000 /*400 rps * 2.5sec*/ / rotationRatio;
		break;
	case (int)PullbackType::STSH_60_60:
		extraFrameNum = m_nNumOfSamples - 400 /*400 rps * 1sec*/ / rotationRatio;
		break;
	case (int)PullbackType::STLO_100_100:
		extraFrameNum = m_nNumOfSamples - 400 /*400 rps * 1sec*/ / rotationRatio;
		break;
	case (int)PullbackType::FAST_120_60:
		extraFrameNum = m_nNumOfSamples - 200 /*400 rps * 0.5sec*/ / rotationRatio;
		break;
	}

	extraFrameNum -= stopFrames * 2;

	if (extraFrameNum <= 0) {
		return;
	}

	SkipFrames(stopFrames, pullbackType, rotationRatio, extraFrameNum);
}

void PullbackLengthManager::SkipFrames(int stopRecordedFrames, int pullbackType, int rotationRatio, int extraFrameNum) {
	PLOGI.printf("SkipFrames");
	try {
		const double A = m_pisp[m_SMProfile][pullbackType].a;
		const double B = m_pisp[m_SMProfile][pullbackType].b;
		const double C = m_pisp[m_SMProfile][pullbackType].c;
		const double threshold = m_pisp[m_SMProfile][pullbackType].threshold;

		PLOGI.printf("A = %.4lf, B = %.4lf, C = %.4lf, Threshold = %.4lf, frameNum = %d",
			A, B, C, threshold, extraFrameNum);

		const int halfLen = std::max(0, extraFrameNum);
		const int accelStart = stopRecordedFrames / 2;
		const int accelEnd = accelStart + halfLen; // Next Index of accel range
		const int decelStart = m_nNumOfSamples - halfLen - stopRecordedFrames / 2;
		const int decelEnd = m_nNumOfSamples - stopRecordedFrames / 2; // [decelStart, decelEnd)
		PLOGI.printf("accelStart = %d, accelEnd = %d, decelStart = %d, decelEnd = %d",
			accelStart, accelEnd, decelStart, decelEnd);

		int numOfSkip = 0;
		double acc = 0.0;
		int count = 0;

		std::vector<bool> keepMask(static_cast<size_t>(m_nNumOfSamples), false);

		// Start-end Ragne to skip(false)
		for (int i = 0; i < stopRecordedFrames / 2 && i < m_nNumOfSamples; ++i) {
			keepMask[i] = false;
			++numOfSkip;
		}
		for (int i = std::max(0, m_nNumOfSamples - stopRecordedFrames / 2); i < m_nNumOfSamples; ++i) {
			keepMask[i] = false;
			++numOfSkip;
		}

		//가속 구간 패턴 생성
		std::vector<bool> accelKeep(halfLen, true); // true=keep, false=skip
		for (int k = 0; k < halfLen; ++k) {
			const int i = accelStart + k;
			if (i < 0 || i >= m_nNumOfSamples) break;

			const double x = static_cast<double>(k + 1);
			double y = 0.0;

			if (pullbackType <= 2) {
				double denom = x + B;
				if (std::abs(denom) < 1e-12) denom = (denom >= 0.0 ? 1e-12 : -1e-12);
				y = A / denom + C;
			}
			else {
				y = A * std::pow(x, B) + C;
			}

			acc += y;
			PLOGI.printf("acc = %.4lf, y = %.4lf", acc, y);

			if (acc >= threshold) {
				accelKeep[k] = false;  // skip
				keepMask[i] = false; // skip
				acc = 0.0;
				numOfSkip += 2;
				count++;
				if (count >= halfLen / 2) {
					break;
				}
				PLOGI.printf("skipped index = %d, reset acc; thr = %.4lf", i, threshold);
			}
			else {
				accelKeep[k] = true;   // keep
				keepMask[i] = true;  // keep
			}
		}

		// Stable Ragne to keep
		const int midStart = std::max(accelEnd, 0);
		const int midEnd = std::min(decelStart - 1, m_nNumOfSamples - 1);
		if (midStart <= midEnd) {
			for (int i = midStart; i <= midEnd; ++i) {
				keepMask[i] = true;
			}
		}

		// Decel Range
		for (int j = 0; j < halfLen; ++j) {
			const int i = decelStart + j;
			if (i < 0 || i >= m_nNumOfSamples) break;

			const bool keep = (j < static_cast<int>(accelKeep.size()))
				? accelKeep[halfLen - 1 - j]
				: true;
			keepMask[i] = keep;
		}

		PLOGI.printf("mask built. samples=%d, approx_gap=%d", m_nNumOfSamples, numOfSkip);

		CompactByKeepMask(keepMask);
	}
	catch (const std::exception& e) {
		PLOGI.printf("Exception: %s", e.what());
	}
}

void PullbackLengthManager::ReadAccelDecelPofileParameter()
{
	try {
		PLOGI.printf("start to read SMProfileParameters.txt");
		std::ifstream reader("./SMProfileParameters.txt");

		constexpr int kProfiles = 2, kItems = 5;

		if (reader.is_open()) {
			std::string line;
			int profile = -1;
			int index = 0;
			while (std::getline(reader, line)) {
				std::vector<std::string> parameter;
				std::stringstream ss(line);
				std::string token;

				while (std::getline(ss, token, ',')) {
					parameter.push_back(token);
				}

				if (parameter.size() == 2) {
					profile = stoi(parameter[1]);
					index = 0;
					if (profile < 0 || profile >= kProfiles) {
						PLOGI.printf("Profile index error");
						continue;
					}
				}

				if (parameter.size() == 5 && index < kItems && profile >= 0)
				{
					m_pisp[profile][index].a = stod(parameter[0]);
					m_pisp[profile][index].b = stod(parameter[1]);
					m_pisp[profile][index].c = stod(parameter[2]);
					m_pisp[profile][index].threshold = stod(parameter[3]);
					m_pisp[profile][index].frameNum = stoi(parameter[4]);
					PLOGI.printf("Profile = %d, index = %d", profile, index);
					PLOGI.printf("A = %.4f, B = %.4f, C = %.4f, Thr = %.4f, frameNum = %d", stod(parameter[0]), 
						stod(parameter[1]), stod(parameter[2]), stod(parameter[3]), stoi(parameter[4]));
					index++;
					
				}
			}
			reader.close();
			PLOGI.printf("end to read SMProfileParameters.txt");
		}
		else {
			PLOGI.printf("Cannot SMProfileParameters open .txt");
		}
	}
	catch (std::exception& e) {
		PLOGI.printf(e.what());
	}
}

void PullbackLengthManager::CompactByKeepMask(const std::vector<bool>& keepMask)
{
	if (m_isRecording) {
		PLOGI.printf("CompactByKeepMask: recording in progress; aborting.");
		return;
	}
	if (m_nNumOfSamples <= 0 || m_nElementSize <= 0 || m_pRecordBuffer == nullptr) {
		PLOGI.printf("CompactByKeepMask: nothing to do.");
		return;
	}
	if (static_cast<int>(keepMask.size()) != m_nNumOfSamples) {
		PLOGI.printf("CompactByKeepMask: mask size(%zu) != samples(%d)",
			keepMask.size(), m_nNumOfSamples);
		return;
	}

	const size_t elemSz = static_cast<size_t>(m_nElementSize);
	int writeIdx = 0;
	int i = 0;

	while (i < m_nNumOfSamples) {
		// skip 구간 건너뛰기
		while (i < m_nNumOfSamples && !keepMask[static_cast<size_t>(i)]) {
			++i;
		}
		if (i >= m_nNumOfSamples) break;

		// keep 연속 구간 파악
		const int runStart = i;
		while (i < m_nNumOfSamples && keepMask[static_cast<size_t>(i)]) {
			++i;
		}
		const int runLen = i - runStart; // [runStart, runStart+runLen)

		// 앞당기기
		if (runLen > 0 && runStart != writeIdx) {
			const size_t srcOff = static_cast<size_t>(runStart) * elemSz;
			const size_t dstOff = static_cast<size_t>(writeIdx) * elemSz;
			const size_t bytes = static_cast<size_t>(runLen) * elemSz;
			memmove(m_pRecordBuffer + dstOff, m_pRecordBuffer + srcOff, bytes);
		}
		writeIdx += runLen;
	}

	const int newCount = writeIdx;
	const int removed = m_nNumOfSamples - newCount;

	// 꼬리 0
	if (removed > 0) {
		const size_t tailOff = static_cast<size_t>(newCount) * elemSz;
		const size_t tailSz = static_cast<size_t>(removed) * elemSz;
		memset(m_pRecordBuffer + tailOff, 0, tailSz);
	}

	PLOGI.printf("CompactByKeepMask: kept=%d, removed=%d", newCount, removed);
	m_nNumOfSamples = newCount;
}
