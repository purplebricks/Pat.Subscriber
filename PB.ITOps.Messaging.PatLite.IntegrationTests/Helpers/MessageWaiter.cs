using System;
using System.Threading;

namespace PB.ITOps.Messaging.PatLite.IntegrationTests.Helpers
{
    public class MessageWaiter<T>
    {
        private readonly EventWaitHandle _messageReceivedWaiter;
        private CapturedMessage<T> _message;

        public MessageWaiter(MessageReceivedNotifier<T> messageNotifier, Func<CapturedMessage<T>, bool> predicate)
        {
            _messageReceivedWaiter = new EventWaitHandle(false, EventResetMode.AutoReset);

            messageNotifier.MessageReceived += (sender, args) =>
            {
                if (predicate(args.CapturedMessage))
                {
                    _message = args.CapturedMessage;
                    _messageReceivedWaiter.Set();
                }
            };
        }

        public CapturedMessage<T> WaitOne(int millisecondsTimeout = 60000)
        {
            _messageReceivedWaiter.WaitOne(millisecondsTimeout);
            return _message;
        }
    }
}