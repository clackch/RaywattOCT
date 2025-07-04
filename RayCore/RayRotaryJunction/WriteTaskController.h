#pragma once

#include <iostream>
#include <queue>
#include <functional>
#include <mutex>
#include <thread>
#include <condition_variable>

class WriteTaskController
{
public:
    WriteTaskController(int interval);
    ~WriteTaskController();

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