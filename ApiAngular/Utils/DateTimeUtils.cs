using Microsoft.VisualBasic;

namespace ApiAngular.Utils
{
    public static class DateTimeUtils
    {
        public static string GetDateTimeVietNamNow()
        {
            TimeZoneInfo vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTime vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);
            var result = vietnamTime.ToString("MM/dd/yyyy h:mm:ss tt");
            return result;
        }
    }
}
