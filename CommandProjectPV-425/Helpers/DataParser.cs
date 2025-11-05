namespace CommandProjectPV_425.Helpers
{
    public static class DataParser
    {
        public static double ParseTimeToMs(string timeStr)
        {
            try
            {
                // конвертируем строку времени в миллисекунды:
                // s => ms: × 1000
                // μs =>: ÷ 1000
                // ns => ms: ÷ 1,000,000
                if (timeStr.EndsWith(" s"))
                    return double.Parse(timeStr.Replace(" s", "")) * 1000;
                else if (timeStr.EndsWith(" ms"))
                    return double.Parse(timeStr.Replace(" ms", ""));
                else if (timeStr.EndsWith(" μs"))
                    return double.Parse(timeStr.Replace(" μs", "")) / 1000;
                else if (timeStr.EndsWith(" ns"))
                    return double.Parse(timeStr.Replace(" ns", "")) / 1_000_000;
                else
                    return 0.0;
            }
            catch
            {
                return 0.0;
            }
        }

        // извлекаем числовое значение ускорения
        public static double ParseSpeedup(string speedupStr)
        {
            try
            {
                return double.Parse(speedupStr.Replace("x", "").Trim());
            }
            catch
            {
                return 0.0;
            }
        }
    }
}
