#pragma once
#include <libpq-fe.h>
#include "FGServer.h"
#include "FrameGrabber.h"

struct CropRegion {
    short left = 0;
    short top = 0;
    short width = 0;
    short height = 0;
};

class Repository
{
public:
    Repository();
    ~Repository();

    bool Connect();
    void Disconnect();

    bool GetCropRect(int roomId, int& left, int& top, int& right, int& bottom);
    bool ApplyCrop(const HDVID_HEADER* pVidHeader, const FrameGrabber& fg, std::vector<unsigned char>& outBuffer);
    bool InitCropRegion(FrameGrabber& fg);
    const CropRegion& GetCropRegion() const { return cropRegion; }
    bool PrintAllCropRooms();

private:
    PGconn* conn = nullptr;
    CropRegion cropRegion;
    bool isSetupFile = false;
};