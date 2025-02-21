#include "MessageService.h"

void CMessageService::postPriorMessage(int msg, WPARAM wParam, LPARAM lParam) {
	m_mutexQueue.lock();

	m_vPriorMessageQueue.push_back(std::make_tuple(msg, wParam, lParam));

	m_mutexQueue.unlock();
}
void CMessageService::postMessage(int msg, WPARAM wParam, LPARAM lParam) {
	PLOGI.printf("TT1");
	PLOGI.printf("CMessageService object address: %p\n", (void*)this);
	PLOGI.printf("m_mutexQueue address: %p\n", static_cast<void*>(&m_mutexQueue));
	if (m_mutexQueue.try_lock()) {
		PLOGI.printf("m_mutexQueue is currently unlocked.\n");
		m_mutexQueue.unlock();  // 상태 확인 후 다시 unlock
	}
	else {
		PLOGI.printf("m_mutexQueue is currently locked by another thread.\n");
	}

	m_mutexQueue.lock();
	PLOGI.printf("TT2");
	m_vMessageQueue.push_back(std::make_tuple(msg, wParam, lParam));
	PLOGI.printf("TT3");
	m_mutexQueue.unlock();
}

std::tuple<int, WPARAM, LPARAM> CMessageService::popMessage() {
	std::tuple<int, WPARAM, LPARAM> currMsg;

	{
		m_mutexQueue.lock();
		if (!m_vPriorMessageQueue.empty()) {
			currMsg = m_vPriorMessageQueue.at(0);
			m_vPriorMessageQueue.erase(m_vPriorMessageQueue.begin());
		}
		else if (!m_vMessageQueue.empty()) {
			currMsg = m_vMessageQueue.at(0);
			m_vMessageQueue.erase(m_vMessageQueue.begin());
		}
		m_mutexQueue.unlock();
	}

	return currMsg;
}