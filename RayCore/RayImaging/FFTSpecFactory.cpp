#include "FFTSpecFactory.h"
#include "MessageService.h"
#include <algorithm>

bool CFFTSpecKey::operator==(const CFFTSpecKey& other) const {
    return order == other.order && flag == other.flag && hint == other.hint;
}

CFFTSpecFactory& CFFTSpecFactory::Instance() {
    static CFFTSpecFactory instance;
    return instance;
}

IppsFFTSpec_R_32f* CFFTSpecFactory::GetSpecR(int order, int flag, IppHintAlgorithm hint) {
    CFFTSpecKey key{ order, flag, hint };

    std::lock_guard<std::mutex> lock(mutexR);
    auto it = std::find_if(specListR.begin(), specListR.end(), [&](const CFFTSpecR& s) {
        return s.key == key;
        });

    if (it != specListR.end()) {
        return it->pSpec;
    }

    PLOGI.printf("Allocate new IppsFFTSpec_R_32f instance! \n");
    int specSize = 0, initSize = 0, bufferSize = 0;
    ippsFFTGetSize_R_32f(order, flag, hint, &specSize, &initSize, &bufferSize);

    Ipp8u* pSpecMem = ippsMalloc_8u(specSize);
    Ipp8u* pInitMem = ippsMalloc_8u(initSize);
    IppsFFTSpec_R_32f* pSpec = nullptr;

    ippsFFTInit_R_32f(&pSpec, order, flag, hint, pSpecMem, pInitMem);
    ippsFree(pInitMem);
    specListR.push_back({ key, pSpec, bufferSize, pSpecMem });
    return pSpec;
}

IppsFFTSpec_C_32fc* CFFTSpecFactory::GetSpecC(int order, int flag, IppHintAlgorithm hint) {
    CFFTSpecKey key{ order, flag, hint };

    std::lock_guard<std::mutex> lock(mutexC);
    auto it = std::find_if(specListC.begin(), specListC.end(), [&](const CFFTSpecC& s) {
        return s.key == key;
        });

    if (it != specListC.end()) {
        return it->pSpec;
    }

    PLOGI.printf("Allocate new IppsFFTSpec_C_32fc instance! \n");
    int specSize = 0, initSize = 0, bufferSize = 0;
    ippsFFTGetSize_C_32fc(order, flag, hint, &specSize, &initSize, &bufferSize);

    Ipp8u* pSpecMem = ippsMalloc_8u(specSize);
    Ipp8u* pInitMem = ippsMalloc_8u(initSize);
    IppsFFTSpec_C_32fc* pSpec = nullptr;

    ippsFFTInit_C_32fc(&pSpec, order, flag, hint, pSpecMem, pInitMem);
    ippsFree(pInitMem);
    specListC.push_back({ key, pSpec, bufferSize, pSpecMem });

    return pSpec;
}

int CFFTSpecFactory::GetBufferR(const IppsFFTSpec_R_32f* pSpec) {
    std::lock_guard<std::mutex> lock(mutexR);
    auto it = std::find_if(specListR.begin(), specListR.end(), [&](const CFFTSpecR& s) {
        return s.pSpec == pSpec;
        });
    return (it != specListR.end()) ? it->bufferSize : 0;
}
int CFFTSpecFactory::GetBufferC(const IppsFFTSpec_C_32fc* pSpec) {
    std::lock_guard<std::mutex> lock(mutexC);
    auto it = std::find_if(specListC.begin(), specListC.end(), [&](const CFFTSpecC& s) {
        return s.pSpec == pSpec;
        });
    return (it != specListC.end()) ? it->bufferSize : 0;
}


CFFTSpecFactory::~CFFTSpecFactory() {
    PLOGI.printf("Delete all lnstance! \n");
    for (auto& s : specListR) {
        ippsFree(s.pSpecMem);
    }
    for (auto& s : specListC) {
        ippsFree(s.pSpecMem);
    }
}