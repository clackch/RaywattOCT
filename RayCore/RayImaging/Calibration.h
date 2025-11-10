#pragma once
#include <ipp.h>
#include <cstdint>
#include <memory>
#include "Config.h"

enum Windows { None, Hanning, Hamming, Gauss1 };

struct complex_t {
    float re;
    float im;
};

// Calibration.h (or the same cpp top)
#pragma pack(push, 1)
struct CalHeaderRWCL {
    unsigned char magic[8];   // "RWCLv1" + '\0'
    uint32_t      version;    // 1
    uint32_t      flags;      // bit0: useComplex
    uint32_t      N;          // nSignal
};
#pragma pack(pop)
static_assert(sizeof(CalHeaderRWCL) == 20, "RWCL header must be 20 bytes");

class CCalibration
{
private:
    int  nAScan;
    int  nFFTLength;
    bool isInit;

public:
    // from calibration file
    char* data;
    int* indexMap;
    float* weightMap;
    complex_t* dispersion;
    float* window;

    int  nSignal = 0;        // map/disp ±Ê¿Ã
    bool useComplex = true;  // flags bit0

public:
    CCalibration(int nAScan, int nFFTLength);
    ~CCalibration();

    bool Initialize(tstring calibFile);
    bool Initialize(char* data);
    bool IsInit() { return isInit; }

private:
    void setWindow(enum Windows eWindow);
    void allocateMemory();
    void releaseMemory();
    bool reallocSignal(int newN);
    bool readCalibration(LPCTSTR calibrationFileName);
};
