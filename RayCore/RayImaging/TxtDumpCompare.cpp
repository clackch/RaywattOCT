#include "TxtDumpCompare.h"
#include <cstdio>
#include <cstring>
#include <cstdlib>
#include <cctype>
#include <cmath>
#include <limits>
#include <Config.h>

static bool parse_kv_int(const char* s, const char* key, int& out) {
    const size_t n = std::strlen(key);
    if (std::strncmp(s, key, n) == 0 && s[n] == '=') {
        out = std::atoi(s + n + 1);
        return true;
    }
    return false;
}

bool LoadMetaTxt(const char* metaPath, TxtMeta& meta) {
    FILE* fp = std::fopen(metaPath, "rb");
    if (!fp) return false;
    char line[1024];
    while (std::fgets(line, sizeof(line), fp)) {
        // trim left
        char* p = line; while (*p && std::isspace(*p)) ++p;
        if (*p == '#' || *p == '\0') continue;
        int v;
        if (parse_kv_int(p, "nSamples", meta.nSamples)) continue;
        if (parse_kv_int(p, "nLines", meta.nLines)) continue;
        if (parse_kv_int(p, "nFFTLength1", meta.nFFTLength1)) continue;
        if (parse_kv_int(p, "nFFTLength2", meta.nFFTLength2)) continue;
        if (parse_kv_int(p, "Nout", meta.Nout)) continue;
        if (parse_kv_int(p, "bscanIdx", meta.bscanIdx)) continue;
        if (std::strncmp(p, "chan=", 5) == 0) {
            meta.chan = std::string(p + 5);
            // strip newline
            if (!meta.chan.empty() && (meta.chan.back() == '\n' || meta.chan.back() == '\r'))
                meta.chan.pop_back();
        }
    }
    std::fclose(fp);
    return (meta.nSamples > 0 && meta.nLines > 0);
}

// 내부: 첫 줄의 "# rows=.. cols=.."를 읽어 rows/cols를 얻는다.
static bool read_rows_cols_from_header(FILE* fp, int& rows, int& cols) {
    long pos = std::ftell(fp);
    char line[1024];
    if (!std::fgets(line, sizeof(line), fp)) {
        std::fseek(fp, pos, SEEK_SET);
        return false;
    }
    if (line[0] != '#') {
        std::fseek(fp, pos, SEEK_SET);
        return false;
    }
    // 예상 포맷: "# rows=%d cols=%d"
    int r = 0, c = 0;
    if (std::sscanf(line, "# rows=%d cols=%d", &r, &c) == 2) {
        rows = r; cols = c; return true;
    }
    return false;
}

static bool read_one_row(FILE* fp, int cols, std::vector<float>& rowOut) {
    // 한 줄에서 cols개의 실수 파싱
    // 줄 길이가 길 수 있으니 반복 읽기/누적 파싱
    rowOut.clear(); rowOut.reserve(cols);
    const size_t BUFSZ = 1 << 20; // 1MB
    std::vector<char> buf(BUFSZ);
    std::string acc;
    while (true) {
        char* s = std::fgets(buf.data(), (int)buf.size(), fp);
        if (!s) return false;
        acc.append(s);
        if (acc.size() && acc.back() != '\n') {
            // 줄이 너무 길면 다음 fgets 이어붙인다
            continue;
        }
        // 이제 acc에 한 줄 완성
        const char* p = acc.c_str();
        char* endp = nullptr;
        while (*p) {
            // strtod는 공백/줄바꿈 무시
            float v = (float)std::strtod(p, &endp);
            if (endp == p) break;
            rowOut.push_back(v);
            p = endp;
            if ((int)rowOut.size() == cols) break;
        }
        break;
    }
    return ((int)rowOut.size() == cols);
}

bool LoadRealRow(const char* path, int rowIndex, int expectedCols, std::vector<float>& out) {
    FILE* fp = std::fopen(path, "rb");
    if (!fp) return false;
    int rows = 0, cols = 0;
    if (!read_rows_cols_from_header(fp, rows, cols)) { std::fclose(fp); return false; }
    if (expectedCols > 0 && cols != expectedCols) { std::fclose(fp); return false; }
    // skip to rowIndex (0-based)
    for (int r = 0; r < rowIndex; ++r) {
        int ch;
        while ((ch = std::fgetc(fp)) != '\n' && ch != EOF) {}
        if (ch == EOF) { std::fclose(fp); return false; }
    }
    bool ok = read_one_row(fp, cols, out);
    std::fclose(fp);
    return ok;
}

bool LoadComplexRow(const char* prefix, int rowIndex, int expectedCols,
    std::vector<float>& re, std::vector<float>& im) {
    std::string rePath = std::string(prefix) + "_re.txt";
    std::string imPath = std::string(prefix) + "_im.txt";
    bool ok1 = LoadRealRow(rePath.c_str(), rowIndex, expectedCols, re);
    bool ok2 = LoadRealRow(imPath.c_str(), rowIndex, expectedCols, im);
    return ok1 && ok2;
}

void LogCompareReal(const char* tag, const float* data, int cols,
    const std::vector<float>& ref, float* outMax) {
    double maxd = 0.0, meand = 0.0;
    for (int c = 0; c < cols; ++c) {
        double d = std::fabs((double)data[c] - (double)ref[c]);
        maxd = (d > maxd) ? d : maxd;
        meand += d;
    }
    meand /= (cols > 0 ? cols : 1);
    PLOGI.printf("[CMP] %-14s max|diff|=%g  mean|diff|=%g\n", tag, maxd, meand);
    if (outMax) *outMax = (float)maxd;
}

void LogCompareComplex(const char* tag, const float* re, const float* im, int cols,
    const std::vector<float>& refRe, const std::vector<float>& refIm,
    float* outMax) {
    double maxd = 0.0, meand = 0.0;
    for (int c = 0; c < cols; ++c) {
        double dr = (double)re[c] - (double)refRe[c];
        double di = (double)im[c] - (double)refIm[c];
        double d = std::sqrt(dr * dr + di * di);
        maxd = (d > maxd) ? d : maxd;
        meand += d;
    }
    meand /= (cols > 0 ? cols : 1);
    PLOGI.printf("[CMP] %-14s max|diff|=%g  mean|diff|=%g\n", tag, maxd, meand);
    if (outMax) *outMax = (float)maxd;
}
