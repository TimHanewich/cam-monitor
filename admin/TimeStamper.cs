using System;

namespace CMonitorAdministration
{
    public class TimeStamper
    {
        public static string DateTimeToTimeStamp(DateTime dt)
        {
            return dt.Year.ToString("0000") + dt.Month.ToString("00") + dt.Day.ToString("00") + dt.Hour.ToString("00") + dt.Second.ToString("00");
        }

        public enum Depth
        {
            Seconds = 0,
            Minutes = 1,
            Hours = 2,
            Days = 3,
            Month = 4,
            Year = 5
        }
    }
}