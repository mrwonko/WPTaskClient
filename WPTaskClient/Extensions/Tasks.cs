using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WPTaskClient.Extensions
{
    static class Tasks
    {
        public static async Task WithTimeout(this Task task, int timeoutMilliseconds)
        {
            if (timeoutMilliseconds == 0 || timeoutMilliseconds == Timeout.Infinite)
            {
                await task;
                return;
            }
            var cts = new CancellationTokenSource();
            var timeout = Task.Delay(timeoutMilliseconds, cts.Token);
            if (task == await Task.WhenAny(task, timeout))
            {
                cts.Cancel();
                await task;
                return;
            }
            // FIXME: can we somehow cancel task here?
            throw new TimeoutException();
        }

        public static async Task<TResult> WithTimeout<TResult>(this Task<TResult> task, int timeoutMilliseconds)
        {
            if (timeoutMilliseconds == 0 || timeoutMilliseconds == Timeout.Infinite)
            {
                return await task;
            }
            var cts = new CancellationTokenSource();
            var timeout = Task.Delay(timeoutMilliseconds, cts.Token);
            if (task == await Task.WhenAny(task, timeout))
            {
                cts.Cancel();
                return await task;
            }
            // FIXME: can we somehow cancel task here?
            throw new TimeoutException();
        }
    }
}
