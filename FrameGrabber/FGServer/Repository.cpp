#include "Repository.h"
#include <libpq-fe.h>
#include <iostream>
#include <string>

Repository::Repository() {}

Repository::~Repository() {
    Disconnect();
}

bool Repository::Connect() {
    const std::string& conninfo = "host=localhost user=rv_user password=raywatt dbname=rv_database";
    conn = PQconnectdb(conninfo.c_str());

    if (PQstatus(conn) != CONNECTION_OK) {
        PLOGI.printf("[DB] Connection failed: %s", PQerrorMessage(conn)); 
        return false;
    }
    return true;
}

void Repository::Disconnect() {
    if (conn) {
        PQfinish(conn);
        conn = nullptr;
    }
}

bool Repository::ApplyCrop(const HDVID_HEADER* pVidHeader, const FrameGrabber& fg, std::vector<unsigned char>& outBuffer)
{
    if (!pVidHeader || !pVidHeader->pBuffer) return false;
    if (isSetupFile) {
        size_t fullSize = fg.lHeight * fg.lWidth * fg.wBitsPerPixel / 8;

        if (fullSize > 0) {
            outBuffer.resize(fullSize);
            memcpy(outBuffer.data(), pVidHeader->pBuffer, fullSize);
        }
        else {
            outBuffer.clear();
        }

        return true;
    }
    const int origWidth = fg.m_LiveStreamInfo.nDestinationWidth;
    //const int origHeight = fg.m_LiveStreamInfo.nDestinationHeight;
    const int bytesPerPixel = fg.wBitsPerPixel / 8;

    const CropRegion& region = GetCropRegion();
    const int left = region.left;
    const int top = fg.lHeight - region.top - region.height;
    const int cropWidth = region.width;
    const int cropHeight = region.height;

    size_t requiredSize = cropWidth * cropHeight * bytesPerPixel;

    if (outBuffer.size() < requiredSize)
        outBuffer.resize(requiredSize);

    if (pVidHeader->pBuffer && !outBuffer.empty() && outBuffer.data() != nullptr && cropWidth > 0 && cropHeight > 0) {
        for (int y = 0; y < cropHeight; ++y) {
            const unsigned char* srcLine = static_cast<const unsigned char*>(pVidHeader->pBuffer)
                + ((top + y) * origWidth + left) * bytesPerPixel;
            unsigned char* dstLine = outBuffer.data() + y * cropWidth * bytesPerPixel;
            memcpy(dstLine, srcLine, cropWidth * bytesPerPixel);
        }
    }

    return true;
}

short ClampToShort(int value) {
    if (value < SHRT_MIN) return SHRT_MIN;
    if (value > SHRT_MAX) return SHRT_MAX;
    return static_cast<short>(value);
}

bool Repository::InitCropRegion(FrameGrabber& fg)
{
    if (!conn) return false;

    std::string query = "SELECT rect_left, rect_top, rect_right, rect_bottom ""FROM rv_schema.cath_room WHERE app_chp = '" + std::string(fg.chpFileName.c_str()) + "';";
    int cropWidth, cropHeight;

    PGresult* res = PQexec(conn, query.c_str());

    if (PQresultStatus(res) != PGRES_TUPLES_OK) {
        PLOGI.printf("[Crop] SQL query failed: %s", PQerrorMessage(conn));
        PQclear(res);
        return false;
    }

    if (PQntuples(res) != 1) {
        if (strncmp(fg.chpFileName.c_str(), "setup\\", 6) == 0)
        {
            cropRegion.width = fg.lWidth, cropRegion.height = fg.lHeight;
            isSetupFile = true;

            PLOGI.printf("This is a setup file.");
            PQclear(res); 
            return true;
        }
        PLOGI.printf("[Crop] No matching crop data found for '%s'", fg.chpFileName.c_str());
        PQclear(res);
        return false;
    }
    
    int left = static_cast<int>(std::stoi(PQgetvalue(res, 0, 0)));
    int top = static_cast<int>(std::stoi(PQgetvalue(res, 0, 1)));
    int right = static_cast<int>(std::stoi(PQgetvalue(res, 0, 2)));
    int bottom = static_cast<int>(std::stoi(PQgetvalue(res, 0, 3)));
    PQclear(res);

    cropWidth = right - left;
    cropHeight = bottom - top;

    if (cropWidth <= 0 || cropHeight <= 0 || left < 0 || top < 0) {
        PLOGI.printf("[Crop] Invalid crop region: left=%hd, top=%hd, right=%hd, bottom=%hd", left, top, right, bottom);

        return false;
    }

    cropRegion.left = ClampToShort(left);
    cropRegion.top = ClampToShort(top);
    cropRegion.height = ClampToShort(cropHeight);
    cropRegion.width = ClampToShort(cropWidth);

    PLOGI.printf("%hd %hd", cropRegion.width, cropRegion.height);
    PLOGI.printf("%hd %hd", cropRegion.width, cropRegion.height);
    return true;
}
