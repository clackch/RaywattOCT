#include "WriteTaskController.h"

WriteTaskController::WriteTaskController(int interval) {
    intervalTime = interval;
    running = false;
}

WriteTaskController::~WriteTaskController() {
    stop();
}

void WriteTaskController::start() {
    taskQueue.clear();
    running = true;
    workerThread = std::thread(&WriteTaskController::worker, this);
}

bool WriteTaskController::addTask(std::function<void()> task) {
    //printf("[in add] : %d\n", taskQueue.unsafe_size());
    taskQueue.push(task);
    cv.notify_one();
    return true;
}

void WriteTaskController::stop() {
    {
        std::lock_guard<std::mutex> lock(queueMutex);
        running = false;
    }
    taskQueue.clear();
    cv.notify_all();  
    if (workerThread.joinable())
        workerThread.join();
}

int WriteTaskController::getTaskNum() {
    return taskQueue.unsafe_size();
}


void WriteTaskController::worker() {
    while (true) {
        std::function<void()> task;
        if (running) {
            if (taskQueue.try_pop(task)) {
                if (task) {
                    task();
                }
                //printf("[in worker] : %d\n", taskQueue.unsafe_size());
            }
        }
        if (!running) break;
        std::this_thread::sleep_for(std::chrono::milliseconds(intervalTime));
    }
}