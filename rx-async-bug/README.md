# Rx Async Bug Demo

This project demonstrates an issue with Rx.NET and async operations in background services where dependencies might get disposed before async operations complete, leading to `ObjectDisposedException` errors during application shutdown.

## Background Workers

**RxWorker1**
- Uses `Subscribe`.
- Manually manages subscription disposal.
- Has an issue with using a dependency after disposal of the DI container.

**RxWorker2**
- Uses `ToTask`
- Waits for the task to complete when cancellation token is triggered.
- Has the same issue with using a dependency after disposal of the DI container.

**TaskWorker**
- Uses `PeriodicTimer` for traditional async approach
- Waits for work completion during shutdown (at least for `HostOptions.ShutdownTimeout`).

## The Problem

During application shutdown, the following sequence can occur:
1. Shutdown is initiated
2. `TickerSingleton` is disposed
3. Background worker's cancellation token is triggered
4. Worker unsubscribes from observable
5. In-flight async operation continues and tries to call disposed `TickerSingleton`
6. `ObjectDisposedException` is thrown, robbing the `DoWorkAsync` operation of a chance to complete.
