using System;

namespace totlib
{
    public interface IClock
    {
        DateTime Now { get; }
    }

    public class SystemClock : IClock
    {
        public static IClock Instance { get; } = new SystemClock();

        private SystemClock()
        {
        }

        public DateTime Now => DateTime.Now;
    }

    public class TestClock : IClock
    {
        public DateTime Now { get; set; } = new DateTime(2020, 9, 1);

        public void AdvanceBy(TimeSpan timespan) => Now += timespan;

        public void AdvanceTo(DateTime dateTime) => Now = dateTime;
    }
}