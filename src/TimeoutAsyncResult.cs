// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Amqp
{
    using System;
    using System.Threading;

    abstract class TimeoutAsyncResult<T> : AsyncResult where T : class
    {
        readonly TimeSpan timeout;
        Timer timer;
        bool setTimerCalled;  // make sure derived class always call SetTimer

        protected TimeoutAsyncResult(TimeSpan timeout, AsyncCallback callback, object state)
            : base(callback, state)
        {
            // The derived class must call SetTimer to start the timer.
            // Timer is not started here because it could fire before the
            // derived class ctor completes.
            this.timeout = timeout;
        }

        protected abstract T Target { get; }

        protected void SetTimer()
        {
            this.setTimerCalled = true;
            if (this.timeout != Timeout.InfiniteTimeSpan && this.timeout != TimeSpan.MaxValue)
            {
                this.timer = new Timer(s => OnTimerCallback(s), this, this.timeout, Timeout.InfiniteTimeSpan);
            }
        }

        protected virtual void CompleteOnTimer()
        {
            if (this.timer != null)
            {
                this.timer.Dispose();
            }

            this.CompleteInternal(false, new TimeoutException(AmqpResources.GetString(AmqpResources.AmqpTimeout, this.timeout, this.Target)));
        }

        protected bool CompleteSelf(bool syncComplete)
        {
            return this.CompleteSelf(syncComplete, null);
        }

        protected bool CompleteSelf(bool syncComplete, Exception exception)
        {
            if (this.timer != null)
            {
                this.timer.Dispose();
            }

            return this.CompleteInternal(syncComplete, exception);
        }

        static void OnTimerCallback(object state)
        {
            TimeoutAsyncResult<T> thisPtr = (TimeoutAsyncResult<T>)state;
            thisPtr.CompleteOnTimer();
        }

        bool CompleteInternal(bool syncComplete, Exception exception)
        {
            Fx.Assert(exception != null || this.setTimerCalled, "Must call SetTimer.");
            return this.TryComplete(syncComplete, exception);
        }
    }
}
