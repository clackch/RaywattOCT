#include <ipp.h>
#include <vector>
#include <mutex>

struct CFFTSpecKey {
    int order;
    int flag;
    IppHintAlgorithm hint;

    bool operator==(const CFFTSpecKey& other) const;
};

struct CFFTSpecR {
    CFFTSpecKey key;
    IppsFFTSpec_R_32f* pSpec;
    //Ipp8u* pBuffer;
	int bufferSize;
    Ipp8u* pSpecMem;
};

struct CFFTSpecC {
    CFFTSpecKey key;
    IppsFFTSpec_C_32fc* pSpec;
    //Ipp8u* pBuffer;
    int bufferSize;
    Ipp8u* pSpecMem;
};

class CFFTSpecFactory {
public:
    static CFFTSpecFactory& Instance();

    IppsFFTSpec_R_32f* GetSpecR(int order, int flag, IppHintAlgorithm hint);
    IppsFFTSpec_C_32fc* GetSpecC(int order, int flag, IppHintAlgorithm hint);

    /*Ipp8u* GetBufferR(const IppsFFTSpec_R_32f* pSpec);
    Ipp8u* GetBufferC(const IppsFFTSpec_C_32fc* pSpec);*/
    int GetBufferR(const IppsFFTSpec_R_32f* pSpec);
    int GetBufferC(const IppsFFTSpec_C_32fc* pSpec);

    ~CFFTSpecFactory();

private:
    CFFTSpecFactory() = default;
    CFFTSpecFactory(const CFFTSpecFactory&) = delete;
    CFFTSpecFactory& operator=(const CFFTSpecFactory&) = delete;

    std::vector<CFFTSpecR> specListR;
    std::vector<CFFTSpecC> specListC;
    std::mutex mutexR;
    std::mutex mutexC;
};