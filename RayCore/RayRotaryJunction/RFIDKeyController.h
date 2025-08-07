#pragma once

#include <vector>
#include <list>
#include <fstream>
#include <sstream>
#include <iomanip>
#define keyFilePath "./keys.txt"
#define KEY_LEN 6

typedef unsigned char       BYTE;

class RFIDKeyController
{
private:
	static std::vector<std::vector<BYTE>> keys;
	static bool isDuplicate(BYTE* arr, size_t len);

public:
	void static readKeys();
	void static addKey(BYTE* key);
	void static loadFirstKey(BYTE* key);
	std::vector<std::vector<BYTE>> static getKeys();
};