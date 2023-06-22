#include "MessageService.h"

void CMessageService::postPriorMessage(int msg, WPARAM wParam, LPARAM lParam) {
	m_mutexQueue.lock();

	m_vPriorMessageQueue.push_back(std::make_tuple(msg, wParam, lParam));

	m_mutexQueue.unlock();
}
void CMessageService::postMessage(int msg, WPARAM wParam, LPARAM lParam) {
	m_mutexQueue.lock();

	m_vMessageQueue.push_back(std::make_tuple(msg, wParam, lParam));

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