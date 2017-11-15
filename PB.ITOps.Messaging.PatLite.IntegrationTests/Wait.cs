using System;
using System.Threading;

namespace PB.ITOps.Messaging.PatLite.IntegrationTests
{
    internal static class Wait
    {
        public static T UntilIsNotNull<T>(Func<T> check, string failureMessage)
        {
            var result = WaitUntilIsNotNull(check);
            if (result != null)
            {
                return result;
            }

            throw new InvalidOperationException(failureMessage);
        }

        public static void ToEnsureIsNull(Func<object> check, string failureMessage)
        {
            if (WaitUntilIsNotNull(check) == null)
            {
                return;
            }

            throw new InvalidOperationException(failureMessage);
        }

        private static T WaitUntilIsNotNull<T>(Func<T> check)
        {
            var attemptCount = 0;

            do
            {
                var value = check();
                if (value != null)
                {
                    return value;
                }

                Thread.Sleep(1000);
            } while (attemptCount++ < 15);

            return default(T);
        }
    }
}