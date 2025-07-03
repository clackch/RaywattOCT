#include "WriteTaskManager.h"

WriteTaskManager::WriteTaskManager(int interval) {
    intervalTime = interval;
    running = true;
    workerThread = std::thread(&WriteTaskManager::worker, this);
}

WriteTaskManager::~WriteTaskManager() {
    stop();
}

void WriteTaskManager::addTask(std::function<void()> task) {
    {
        std::lock_guard<std::mutex> lock(queueMutex);
        taskQueue.push(std::move(task));
    }
    cv.notify_one();
}

void WriteTaskManager::stop() {
    {
        std::lock_guard<std::mutex> lock(queueMutex);
        running = false;
    }
    cv.notify_all();  
    if (workerThread.joinable())
        workerThread.join();
}


void WriteTaskManager::worker() {
    while (true) {
        std::function<void()> task;

        {
            std::unique_lock<std::mutex> lock(queueMutex);
            cv.wait(lock, [this] {
                return !taskQueue.empty() || !running;
                });
            if (!running && taskQueue.empty())
                break;

            task = std::move(taskQueue.front()); 
            taskQueue.pop();
        }
        task(); 
        std::this_thread::sleep_for(std::chrono::milliseconds(intervalTime));
    }
}