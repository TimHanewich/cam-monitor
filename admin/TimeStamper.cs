using System;

namespace CMonitorAdministration
{
    public class TimeStamper
    {
        public static string DateTimeToTimeStamp(DateTime dt)
        {
            return dt.Year.ToString("0000") + dt.Month.ToString("00") + dt.Day.ToString("00") + dt.Hour.ToString("00") + dt.Second.ToString("00");
        }

        public static DateTime TimeStampToDateTime(string ts)
        {
            //Parse out
            int year = 0;
            int month = 0;
            int day = 0;
            int hour = 0;
            int minute = 0;
            int second = 0;

            //Parse now
            if (ts.Length >= 4)
            {
                year = Convert.ToInt32(ts.Substring(0, 4));
            }
            if (ts.Length >= 6)
            {
                month = Convert.ToInt32(ts.Substring(4, 2));
            }
            if (ts.Length >= 8)
            {
                day = Convert.ToInt32(ts.Substring(6, 2));
            }
            if (ts.Length >= 10)
            {
                hour = Convert.ToInt32(ts.Substring(8, 2));
            }
            if (ts.Length >= 12)
            {
                minute = Convert.ToInt32(ts.Substring(10, 2));
            }
            if (ts.Length >= 14)
            {
                second = Convert.ToInt32(ts.Substring(12, 2));
            }
            //Console.WriteLine("Year: " + year.ToString());
            //Console.WriteLine("Month: " + month.ToString());
            //Console.WriteLine("Day: " + day.ToString());
            //Console.WriteLine("Hour: " + hour.ToString());
            //Console.WriteLine("Minute: " + minute.ToString());
            //Console.WriteLine("Second: " + second.ToString());
            return new DateTime(year, month, day, hour, minute, second);
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