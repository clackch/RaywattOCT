#ifndef NOMINMAX
#define NOMINMAX
#endif
#include <Windows.h>
#include <math.h>
#include <algorithm>
#include "Calibration.h"

static_assert(sizeof(complex_t) == sizeof(Ipp32fc), "complex_t layout must match Ipp32fc");

CCalibration::CCalibration(int nAScan, int nFFTLength)
    : nAScan(nAScan),
    nFFTLength(nFFTLength),
    isInit(false),
    data(nullptr),
    indexMap(nullptr),
    weightMap(nullptr),
    window(nullptr),
    dispersion(nullptr)
{
    allocateMemory();
    setWindow(Hanning); // 파일 win 읽으면 나중에 덮어씀
}

CCalibration::~CCalibration()
{
    releaseMemory();
}

bool CCalibration::Initialize(tstring calibFile)
{
    isInit = readCalibration(calibFile.c_str());
    return isInit;
}

// RWCLv1 메모리 버전은 미사용 (필요시 별도 포맷 정의)
bool CCalibration::Initialize(char* data)
{
    return false;
}

bool CCalibration::readCalibration(LPCTSTR calibrationFileName)
{
    HANDLE hFile = CreateFile(calibrationFileName, GENERIC_READ,
        FILE_SHARE_READ | FILE_SHARE_WRITE, nullptr, OPEN_EXISTING, 0, nullptr);
    if (hFile == INVALID_HANDLE_VALUE) return false;

    LARGE_INTEGER fsize{};
    if (!GetFileSizeEx(hFile, &fsize)) { CloseHandle(hFile); return false; }

    DWORD rb = 0;
    CalHeaderRWCL hdr{};
    if (!ReadFile(hFile, &hdr, sizeof(hdr), &rb, nullptr) || rb != sizeof(hdr)) {
        CloseHandle(hFile); return false;
    }

    const bool magic_ok = (memcmp(hdr.magic, "RWCLv1", 7) == 0) && (hdr.magic[7] == 0);
    const bool version_ok = (hdr.version == 1);
    if (!magic_ok || !version_ok) {
        PLOGE.printf("RWCL header mismatch (magic_ok=%d, version=%u)", (int)magic_ok, hdr.version);
        CloseHandle(hFile); return false;
    }

    // 기본 규약: demod/analytic 파이프라인은 N == nAScan을 권장
    useComplex = (hdr.flags & 0x1u) != 0;
    nSignal = (int)hdr.N;

    // 파일크기 교차검증: 20 + (idx:int32 N) + (w:float N) + (disp:complex N) [+ window?]
    const long long min_expect = 20ll + 4ll * hdr.N + 4ll * hdr.N + 8ll * hdr.N; // = 20 + 16N
    if (fsize.QuadPart < min_expect) {
        PLOGE.printf("File too small: size=%lld, expect>=%lld (N=%u)", fsize.QuadPart, min_expect, hdr.N);
        CloseHandle(hFile); return false;
    }

    PLOGI.printf("RWCL OK: N=%u, flags=0x%08X (%s)", hdr.N, hdr.flags, useComplex ? "complex" : "real");

    if (!reallocSignal((int)hdr.N)) { CloseHandle(hFile); return false; }

    const size_t szIdx = sizeof(int32_t) * nSignal;
    const size_t szW = sizeof(float) * nSignal;
    const size_t szDisp = sizeof(complex_t) * nSignal;

    if (!ReadFile(hFile, indexMap, (DWORD)szIdx, &rb, nullptr) || rb != szIdx) { CloseHandle(hFile); return false; }
    if (!ReadFile(hFile, weightMap, (DWORD)szW, &rb, nullptr) || rb != szW) { CloseHandle(hFile); return false; }
    if (!ReadFile(hFile, dispersion, (DWORD)szDisp, &rb, nullptr) || rb != szDisp) { CloseHandle(hFile); return false; }

    // 남은 바이트 → window 유무 판단
    LARGE_INTEGER cur{}; SetFilePointerEx(hFile, { 0 }, &cur, FILE_CURRENT);
    const LONGLONG consumed = sizeof(hdr) + (LONGLONG)szIdx + (LONGLONG)szW + (LONGLONG)szDisp;
    const LONGLONG remain = fsize.QuadPart - consumed;
    bool loadedWin = false;

    if (remain >= 0 && (remain % sizeof(float) == 0)) {
        const size_t winCount = (size_t)(remain / sizeof(float));
        // window 버퍼 크기 보장: nFFTLength는 반드시 nAScan 이상이어야 안전
        if (nFFTLength < nAScan) {
            PLOGW.printf("nFFTLength(%d) < nAScan(%d). Fixing window to nAScan.", nFFTLength, nAScan);
            nFFTLength = nAScan;  // 필요시 멤버/세팅도 함께 업데이트
            if (window) delete[] window;
            window = new float[nFFTLength];
        }

        if (winCount == (size_t)nFFTLength) {
            if (ReadFile(hFile, window, (DWORD)(sizeof(float) * winCount), &rb, nullptr) && rb == sizeof(float) * winCount) loadedWin = true;
        }
        else if (winCount == (size_t)nAScan) {
            std::unique_ptr<float[]> tmp(new float[winCount]);
            if (ReadFile(hFile, tmp.get(), (DWORD)(sizeof(float) * winCount), &rb, nullptr) && rb == sizeof(float) * winCount) {
                memcpy(window, tmp.get(), sizeof(float) * nAScan);
                ippsZero_32f(window + nAScan, nFFTLength - nAScan);
                loadedWin = true;
            }
        }
        else {
            // 의미 다른 window → 읽어서 버리기
            std::unique_ptr<float[]> skip(new float[winCount]);
            ReadFile(hFile, skip.get(), (DWORD)(sizeof(float) * winCount), &rb, nullptr);
        }
    }

    CloseHandle(hFile);
    if (!loadedWin) setWindow(Hanning);

    // k-보간에서 fcAnalytic[k]를 읽으므로 indexMap은 [0..nAScan-2]로 클램프
    for (int j = 0; j < nSignal; ++j) {
        indexMap[j] = std::min(std::max(indexMap[j], 0), nAScan - 2);
        weightMap[j] = std::min(std::max(weightMap[j], 0.0f), 1.0f);
    }

    // 참고 로그
    const int expectedN = nAScan;  // demod/analytic 기준
    if ((int)hdr.N != expectedN) {
        PLOGW.printf("[Calibration] N(%u) != nAScan(%d). Make sure your MATLAB exporter writes N = nAScan.", hdr.N, nAScan);
    }
    return true;
}


void CCalibration::setWindow(enum Windows eWindow)
{
    const float fAScan = (float)nAScan;

    ippsSet_32f(1.0f, window, nFFTLength);

    switch (eWindow)
    {
    case None:
        break;
    case Hanning:
        ippsWinHann_32f_I(window, nAScan);
        break;
    case Hamming:
        ippsWinHamming_32f_I(window, nAScan);
        break;
    case Gauss1:
        for (int i = 0; i < nAScan; i++) {
            float x = (i - fAScan * 0.5f);
            window[i] = expf(-(2.0f / fAScan) * (2.0f / fAScan) * x * x);
        }
        break;
    }
    // tail zero-pad
    if (nFFTLength > nAScan)
        ippsZero_32f(window + nAScan, nFFTLength - nAScan);
}

void CCalibration::allocateMemory()
{
    if (nSignal <= 0) nSignal = nAScan; // 기본(복소 경로)
    if (nSignal < 1024 * 1024) {
        indexMap = new int[nSignal];
        weightMap = new float[nSignal];
        dispersion = new complex_t[nSignal];
    }
    if (nFFTLength < 1024 * 1024) {
        window = new float[nFFTLength];
    }
}

bool CCalibration::reallocSignal(int newN)
{
    if (newN <= 0 || newN > 1024 * 1024) return false;
    if (indexMap) { delete[] indexMap;   indexMap = nullptr; }
    if (weightMap) { delete[] weightMap;  weightMap = nullptr; }
    if (dispersion) { delete[] dispersion; dispersion = nullptr; }

    nSignal = newN;
    indexMap = new int[nSignal];
    weightMap = new float[nSignal];
    dispersion = new complex_t[nSignal];
    return true;
}

void CCalibration::releaseMemory()
{
    if (data) { delete[] data; data = nullptr; }
    if (indexMap) { delete[] indexMap; indexMap = nullptr; }
    if (weightMap) { delete[] weightMap; weightMap = nullptr; }
    if (dispersion) { delete[] dispersion; dispersion = nullptr; }
    if (window) { delete[] window; window = nullptr; }
}
