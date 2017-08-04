namespace PB.ITOps.Messaging.PatLite.RateLimiterPolicy.UnitTests
{
    public class TestTimer : ITimer
    {
        private long _elapsedTime = -1;

        public void AddElapsedSeconds(int value)
        {
            _elapsedTime += value * 1000;
        }

        public void AddElapsedMilliseconds(int value)
        {
            _elapsedTime += value;
        }

        public void Start()
        {
            if (_elapsedTime == -1)
            {
                _elapsedTime = 0;
            }
        }

        public long ElapsedMilliseconds
        {
            get => _elapsedTime; set => _elapsedTime = value;
        }
    }
}
