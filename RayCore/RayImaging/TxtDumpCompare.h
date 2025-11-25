#pragma once
#include <vector>
#include <string>

struct TxtMeta {
    int nSamples = 0;
    int nLines = 0;
    int nFFTLength1 = 0;
    int nFFTLength2 = 0;
    int Nout = 0;
    int bscanIdx = 1;
    std::string chan;
};

bool LoadMetaTxt(const char* metaPath, TxtMeta& meta);

// 실수 한 행(rowIndex) 로드 (행/열 정보는 파일 첫 줄 #rows= cols= 로부터 확인 후 검증)
bool LoadRealRow(const char* path, int rowIndex, int expectedCols, std::vector<float>& out);

// 복소 한 행(rowIndex) 로드: re/im 파일 두 개 읽음
bool LoadComplexRow(const char* prefix, int rowIndex, int expectedCols,
    std::vector<float>& re, std::vector<float>& im);

// 비교 로그: max|diff|/mean|diff|
void LogCompareReal(const char* tag, const float* data, int cols,
    const std::vector<float>& ref, float* outMax = nullptr);

void LogCompareComplex(const char* tag, const float* re, const float* im, int cols,
    const std::vector<float>& refRe, const std::vector<float>& refIm,
    float* outMax = nullptr);
