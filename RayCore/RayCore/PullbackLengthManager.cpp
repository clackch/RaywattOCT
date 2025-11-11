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

	int rotationRatio = (int)(24038.0 / rotationSpeed);  // 1:400rps, 2:200rps, 4:100rps
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
		extraFrameNum = m_nNumOfSamples - 400  /*400 rps * 1sec*/ / rotationRatio;
		break;
	case (int)PullbackType::STLO_100_100:
		extraFrameNum = m_nNumOfSamples - 400  /*400 rps * 1sec*/ / rotationRatio;
		break;
	case (int)PullbackType::FAST_120_60:
		extraFrameNum = m_nNumOfSamples - 200  /*400 rps * 0.5sec*/ / rotationRatio;
		break;
	}

	// 경계 프레임(좌/우 stop) 제외
	extraFrameNum -= stopFrames * 2;
	if (extraFrameNum <= 0) return;

	// 400rps 기준 배율 (예: 200rps면 0.5)
	double rotationFactor = (double)rotationSpeed / 24038.0;
	extraFrameNum = QuantizeExtraFrames(extraFrameNum, rotationFactor);

	SkipFrames(stopFrames, pullbackType, rotationRatio, extraFrameNum);
}

void PullbackLengthManager::SkipFrames(int stopRecordedFrames, int pullbackType, int rotationRatio, int extraFrameNum) {
	PLOGI.printf("SkipFrames");
	try {
		const double A = m_pisp[m_SMProfile][pullbackType].a;
		const double B = m_pisp[m_SMProfile][pullbackType].b;
		const double C = m_pisp[m_SMProfile][pullbackType].c;
		const double threshold = m_pisp[m_SMProfile][pullbackType].threshold;

		PLOGI.printf("A = %.4lf, B = %.4lf, C = %.4lf, Threshold = %.4lf, extraFrameNum  = %d",
			A, B, C, threshold, extraFrameNum);

		const int n = m_nNumOfSamples;
		const int halfLenForRange = std::max(0, extraFrameNum);

		// 1) thresholdScale 이분탐색으로 skip 수를 target에 근접
		double lo = 0.0, hi = 1.0;     // 경험적 범위 (필요시 조정)
		MaskResult bestRes;
		int bestDiff = INT_MAX;

		for (int it = 0; it < 22; ++it) {
			double mid = 0.5 * (lo + hi);
			auto res = BuildMaskWithThresholdScale(
				stopRecordedFrames, pullbackType, rotationRatio, halfLenForRange,
				mid, A, B, C, threshold, n
			);
			int diff = std::abs(res.skipCount - extraFrameNum);
			if (diff < bestDiff) {
				bestDiff = diff;
				bestRes = std::move(res);
			}
			if (res.skipCount < extraFrameNum) {
				// skip 부족 -> threshold 더 낮춰야 함
				hi = mid;
			}
			else {
				// skip 과다 -> threshold 더 높임
				lo = mid;
			}
		}

		// 2) 균등 간격 보정으로 정확히 targetSkip 달성
		int curSkip = bestRes.skipCount;
		auto& mask = bestRes.keepMask;

		const int leftGuard = stopRecordedFrames / 2;
		const int rightGuard = n - stopRecordedFrames / 2;

		auto toggle_to_skip = [&](int idx) {
			if (idx >= leftGuard && idx < rightGuard && mask[(size_t)idx]) {
				mask[(size_t)idx] = false;
				++curSkip;
			}
			};
		auto toggle_to_keep = [&](int idx) {
			if (idx >= leftGuard && idx < rightGuard && !mask[(size_t)idx]) {
				mask[(size_t)idx] = true;
				--curSkip;
			}
			};

		auto distribute_toggle = [&](int need, bool toSkip) {
			if (need <= 0) return;
			std::vector<int> cand;
			cand.reserve(n);
			for (int i = leftGuard; i < rightGuard; ++i) {
				if (toSkip) { if (mask[(size_t)i]) cand.push_back(i); }   // keep -> skip
				else { if (!mask[(size_t)i]) cand.push_back(i); }  // skip -> keep
			}
			if (cand.empty()) return;
			for (int k = 0; k < need && !cand.empty(); ++k) {
				size_t pos = (size_t)((1.0 * (k + 1) / (need + 1)) * cand.size());
				if (pos >= cand.size()) pos = cand.size() - 1;
				int idx = cand[(size_t)pos];
				if (toSkip) toggle_to_skip(idx);
				else        toggle_to_keep(idx);
				if (curSkip == extraFrameNum) break;
			}
			};

		if (curSkip < extraFrameNum) {
			distribute_toggle(extraFrameNum - curSkip, /*toSkip=*/true);
		}
		else if (curSkip > extraFrameNum) {
			distribute_toggle(curSkip - extraFrameNum, /*toSkip=*/false);
		}

		// 안전 마무리(혹시 잔차가 남으면 선형으로 맞춤)
		if (curSkip != extraFrameNum) {
			if (curSkip < extraFrameNum) {
				for (int i = leftGuard; i < rightGuard && curSkip < extraFrameNum; ++i) toggle_to_skip(i);
			}
			else {
				for (int i = leftGuard; i < rightGuard && curSkip > extraFrameNum; ++i) toggle_to_keep(i);
			}
		}

		PLOGI.printf("mask built. samples=%d, final_skip=%d (target=%d)", n, curSkip, extraFrameNum);
		CompactByKeepMask(mask);
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

int PullbackLengthManager::QuantizeExtraFrames(int extraFrameNumRaw, double rotationRatio) {
	const int baseSet[] = { 200, 400, 1000, 1200 };
	std::vector<int> allowed;
	allowed.reserve(4);
	for (int v : baseSet) {
		int q = (int)std::round(v * rotationRatio); // rotationRatio == rotationFactor(400rps 기준 배율)
		if (q > 0) allowed.push_back(q);
	}
	if (allowed.empty()) return extraFrameNumRaw;

	int best = allowed[0];
	int bestDiff = std::abs(best - extraFrameNumRaw);
	for (int a : allowed) {
		int d = std::abs(a - extraFrameNumRaw);
		if (d < bestDiff) { best = a; bestDiff = d; }
	}
	return best;
}

PullbackLengthManager::MaskResult PullbackLengthManager::BuildMaskWithThresholdScale(
	int stopRecordedFrames,
	int pullbackType,
	int rotationRatio,
	int desiredSkipHalfLen,
	double thresholdScale,
	double A, double B, double C, double threshold,
	int nSamples)
{
	MaskResult res;
	res.keepMask.assign((size_t)nSamples, false);

	const int halfLen = std::max(0, desiredSkipHalfLen);
	const int accelStart = stopRecordedFrames / 2;
	const int accelEnd = accelStart + halfLen;
	const int decelStart = nSamples - halfLen - stopRecordedFrames / 2;
	const int decelEnd = nSamples - stopRecordedFrames / 2;

	// 경계부는 keep=false만 하고 skipCount는 증가시키지 않음
	int skipCount = 0;
	for (int i = 0; i < stopRecordedFrames / 2 && i < nSamples; ++i) {
		res.keepMask[(size_t)i] = false;
	}
	for (int i = std::max(0, nSamples - stopRecordedFrames / 2); i < nSamples; ++i) {
		res.keepMask[(size_t)i] = false;
	}

	// 가속 구간 (원 패턴 유지)
	std::vector<bool> accelKeep((size_t)halfLen, true);
	double acc = 0.0;
	const double thr = threshold * thresholdScale;

	for (int k = 0; k < halfLen; ++k) {
		const int i = accelStart + k;
		if (i < 0 || i >= nSamples) break;

		const double x = double(k + 1);
		double y = (pullbackType <= 2)
			? ([&, x] { double denom = x + B; if (std::abs(denom) < 1e-12) denom = (denom >= 0 ? 1e-12 : -1e-12); return A / denom + C; }())
			: (A * std::pow(x, B) + C);

		acc += y;
		if (acc >= thr) {
			accelKeep[(size_t)k] = false;
			res.keepMask[(size_t)i] = false;
			acc = 0.0;
			++skipCount; // 내부 샘플링만 카운트
		}
		else {
			accelKeep[(size_t)k] = true;
			res.keepMask[(size_t)i] = true;
		}
	}

	// 중앙 구간 keep
	const int midStart = std::max(accelEnd, 0);
	const int midEnd = std::min(decelStart - 1, nSamples - 1);
	if (midStart <= midEnd) {
		for (int i = midStart; i <= midEnd; ++i) res.keepMask[(size_t)i] = true;
	}

	// 감속 구간: 미러링
	for (int j = 0; j < halfLen; ++j) {
		const int i = decelStart + j;
		if (i < 0 || i >= nSamples) break;
		const bool keep = (j < (int)accelKeep.size()) ? accelKeep[(size_t)(halfLen - 1 - j)] : true;
		if (!keep) ++skipCount;                         // 내부 샘플링만 카운트
		res.keepMask[(size_t)i] = keep;
	}

	res.skipCount = skipCount;
	return res;
}
