#pragma once

#include <iostream>
#include <queue>
#include <functional>
#include <mutex>
#include <thread>
#include <condition_variable>

class WriteTaskManager
{
public:
    WriteTaskManager(int interval);
    ~WriteTaskManager();

    void addTask(std::function<void()> task);
    void stop();
private:
    std::queue<std::function<void()>> taskQueue;
    std::mutex queueMutex;
    std::condition_variable cv;
    bool running;
    std::thread workerThread;
    int intervalTime;

    void worker();
};