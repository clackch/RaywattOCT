#pragma once

#include <iostream>
#include <queue>
#include <functional>
#include <mutex>
#include <thread>
#include <condition_variable>
#include <concurrent_queue.h>

class WriteTaskController
{
public:
    explicit WriteTaskController(int interval);
    ~WriteTaskController();

    void start();
    void stop();
    bool addTask(std::function<void()> task);
    int getTaskNum();

    WriteTaskController(const WriteTaskController&) = delete;
    WriteTaskController& operator=(const WriteTaskController&) = delete;
    WriteTaskController(WriteTaskController&&) = delete;
    WriteTaskController& operator=(WriteTaskController&&) = delete;
private:
   Concurrency::concurrent_queue<std::function<void()>> taskQueue;
    std::mutex queueMutex;
    std::condition_variable cv;
    bool running;
    std::thread workerThread;
    int intervalTime;

    void worker();
};