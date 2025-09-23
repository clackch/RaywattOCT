#include "RFIDKeyController.h"

std::vector<std::vector<BYTE>> RFIDKeyController::keys;

bool RFIDKeyController::isDuplicate(BYTE* arr, size_t len) {
	for (const auto& existing : keys) {
		if (existing.size() == len && std::equal(existing.begin(), existing.end(), arr)) {
			return true;
		}
	}
	return false;
}

void RFIDKeyController::readKeys() {
	std::ifstream keyFile(keyFilePath, std::ios::in);
	if (!keyFile.is_open()) return;
	std::string line;
	while (std::getline(keyFile, line)) {
		if (line.empty()) continue;
		std::vector<BYTE> key;
		bool valid = true;
		for (int i = 0; i < KEY_LEN; ++i) {
			std::string hexByte = line.substr(i * 2, 2); 
			try {
				int intByte = std::stoi(hexByte, nullptr, 16);
				if (intByte < 0 || intByte > 255) {
					valid = false;
					break;
				}
				key.push_back(static_cast<BYTE>(intByte));
			}
			catch (const std::exception&) {
				valid = false;
				break;
			}
		}
		if (!valid) continue;
		keys.push_back(key);
	}
	keyFile.close();
}

void RFIDKeyController::addKey(BYTE* key) {
	if (isDuplicate(key, KEY_LEN)) return;
	std::ofstream logFile(keyFilePath, std::ios::app);
	if (!logFile.is_open()) {
		return;
	}
	std::stringstream keyStr;
	for (int i = 0; i < KEY_LEN; i++) {
		keyStr << std::uppercase << std::hex << std::setw(2) << std::setfill('0') << static_cast<int>(key[i]);
	}
	logFile << keyStr.str() << "\n";

	std::vector<BYTE> tnsKey;
	for (int i = 0; i < KEY_LEN; ++i) {
		tnsKey.push_back(key[i]);
	}
	keys.push_back(tnsKey);
}

void RFIDKeyController::loadFirstKey(BYTE* key) {
	if (!key) return;

	std::fill_n(key, KEY_LEN, static_cast<BYTE>(0));

	if (keys.empty()) {
		readKeys();
	}
	if (keys.empty()) {
		return;
	}
	const std::vector<BYTE>& firstKey = keys.front();
	if (firstKey.size() != KEY_LEN) return;
	std::copy_n(firstKey.data(), KEY_LEN, key);
}

const std::vector<std::vector<BYTE>>& RFIDKeyController::getKeys() {
	return keys;
}
