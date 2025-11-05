namespace CommandProjectPV_425_Test.Helpers
{
    public static class TimeFormatter
    {
        // для микросекунд
        public static string FormatTimeUs(double timeUs)
        {
            if (timeUs >= 1000)
                return (timeUs / 1000).ToString("F2") + " ms";
            else if (timeUs >= 1)
                return timeUs.ToString("F1") + " μs";
            else
                return (timeUs * 1000).ToString("F1") + " ns";
        }
        // для миллисекунд
        public static string FormatTime(double? timeMs)
        {
            if (timeMs < 0.001)
                return $"{(timeMs * 1_000_000):F2} ns";
            else if (timeMs < 0.1)
                return $"{(timeMs * 1000):F2} μs";
            else if (timeMs < 1000)
                return $"{timeMs:F2} ms";
            else
                return $"{(timeMs / 1000):F2} s";
        }

        public static string FormatTimeShort(double timeMs)
        {
            if (timeMs < 0.001)
                return $"{(timeMs * 1_000_000):F0} ns";
            else if (timeMs < 0.1)
                return $"{(timeMs * 1000):F0} μs";
            else if (timeMs < 1000)
                return $"{timeMs:F0} ms";
            else
                return $"{(timeMs / 1000):F1} s";
        }
    }
}
