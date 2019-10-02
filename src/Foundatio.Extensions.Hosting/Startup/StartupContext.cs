﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Foundatio.Utility;
using Microsoft.Extensions.Logging;

namespace Foundatio.Extensions.Hosting.Startup {
    public class StartupActionsContext {
        private readonly ILogger _logger;
        private int _waitCount = 0;

        public StartupActionsContext(ILogger<StartupActionsContext> logger) {
            _logger = logger;
        }

        public bool IsStartupComplete { get; private set; }
        public RunStartupActionsResult Result { get; private set; }

        internal void MarkStartupComplete(RunStartupActionsResult result) {
            IsStartupComplete = true;
            Result = result;
        }

        public async Task<RunStartupActionsResult> WaitForStartupAsync(CancellationToken cancellationToken) {
            bool isFirstWaiter = Interlocked.Increment(ref _waitCount) == 1;
            var startTime = SystemClock.UtcNow;
            var lastStatus = SystemClock.UtcNow;

            while (!cancellationToken.IsCancellationRequested) {
                if (IsStartupComplete)
                    return Result;

                if (isFirstWaiter && SystemClock.UtcNow.Subtract(lastStatus) > TimeSpan.FromSeconds(5) && _logger.IsEnabled(LogLevel.Information)) {
                    lastStatus = SystemClock.UtcNow;
                    _logger.LogInformation("Waiting for startup actions to be completed for {Duration:mm\\:ss}...", SystemClock.UtcNow.Subtract(startTime));
                }

                await Task.Delay(1000, cancellationToken).AnyContext();
            }

            if (isFirstWaiter && _logger.IsEnabled(LogLevel.Error))
                _logger.LogError("Timed out waiting for startup actions to be completed after {Duration:mm\\:ss}", SystemClock.UtcNow.Subtract(startTime));

            return new RunStartupActionsResult { Success = false, ErrorMessage = $"Timed out waiting for startup actions to be completed after {SystemClock.UtcNow.Subtract(startTime):mm\\:ss}" };
        }
    }
}