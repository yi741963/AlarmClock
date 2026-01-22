using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AlarmClock;

/// <summary>
/// 台灣國定假日管理類別
/// </summary>
public static class TaiwanHolidays
{
    private static TaiwanLunisolarCalendar tCal = new TaiwanLunisolarCalendar();

    /// <summary>
    /// 取得指定年份的所有台灣國定假日
    /// </summary>
    /// <param name="year">年份</param>
    /// <returns>國定假日日期列表</returns>
    public static List<DateTime> GetHolidays(int year)
    {
        var holidays = new List<DateTime>();

        // 固定日期的國定假日
        holidays.AddRange(GetFixedHolidays(year));

        // 農曆新年（需要計算）
        holidays.AddRange(GetLunarHolidays(year));

        return holidays.OrderBy(d => d).ToList();
    }

    /// <summary>
    /// 取得固定日期的國定假日
    /// </summary>
    private static List<DateTime> GetFixedHolidays(int year)
    {
        return new List<DateTime>
        {
            new DateTime(year, 1, 1),   // 中華民國開國紀念日（元旦）
            new DateTime(year, 2, 28),  // 和平紀念日
            new DateTime(year, 4, 4),   // 兒童節
            new DateTime(year, 4, 5),   // 清明節
            new DateTime(year, 5, 1),   // 勞動節
            //new DateTime(year, 6, 10),  // 端午節（假設，實際需農曆計算）
            //new DateTime(year, 9, 17),  // 中秋節（假設，實際需農曆計算）
            new DateTime(year, 9, 28),  // 教師節
            new DateTime(year, 10, 10), // 國慶日
            new DateTime(year, 10, 25), // 光復節
            new DateTime(year, 12, 25), // 行憲紀念日
        };
    }

    /// <summary>
    /// 取得農曆假期
    /// </summary>
    private static List<DateTime> GetLunarHolidays(int year)
    {
        var holidays = new List<DateTime>();

        //1. 春節初一 (Lunar New Year Day 1)
        DateTime lnyDay1 = tCal.ToDateTime(year, 1, 1, 0, 0, 0, 0);
        holidays.Add(lnyDay1);
        DateTime lnyDay2 = lnyDay1.AddDays(1);
        holidays.Add(lnyDay2);
        DateTime lnyDay3 = lnyDay1.AddDays(2);
        holidays.Add(lnyDay3);

        //2. 除夕 (春節前一天)
        DateTime lnyEve = lnyDay1.AddDays(-1);
        holidays.Add(lnyEve);

        //3. 端午節 (農曆 5/5)
        DateTime dragonBoat = GetLunarDate(year, 5, 5);
        holidays.Add(dragonBoat);

        //4. 中秋節 (農曆 8/15)
        DateTime midAutumn = GetLunarDate(year, 8, 15);
        holidays.Add(midAutumn);

        return holidays;
    }

    /// <summary>
    /// 將農曆月日轉換為西元日期，自動處理閏月索引
    /// </summary>
    private static DateTime GetLunarDate(int year, int month, int day)
    {
        int leapMonth = tCal.GetLeapMonth(year);

        // 如果該年有閏月，且閏月在目標月份之前或剛好是目標月份
        // 則實際輸入的月份索引需要加 1
        // 範例：若閏 2 月，則真正的 3 月在 Calendar 索引中會變成 4
        int actualMonth = (leapMonth > 0 && leapMonth <= month) ? month + 1 : month;

        return tCal.ToDateTime(year, actualMonth, day, 0, 0, 0, 0);
    }

    /// <summary>
    /// 檢查指定日期是否為台灣國定假日
    /// </summary>
    /// <param name="date">要檢查的日期</param>
    /// <returns>是否為國定假日</returns>
    public static bool IsHoliday(DateTime date)
    {
        var holidays = GetHolidays(date.Year);
        return holidays.Any(h => h.Date == date.Date);
    }

    /// <summary>
    /// 取得國定假日的名稱
    /// </summary>
    /// <param name="date">日期</param>
    /// <returns>假日名稱，若非假日則返回空字串</returns>
    public static string GetHolidayName(DateTime date)
    {
        var holidayNames = new Dictionary<string, string>
        {
            { "01-01", "中華民國開國紀念日" },
            { "02-28", "和平紀念日" },
            { "04-04", "兒童節" },
            { "04-05", "清明節" },
            { "05-01", "勞動節" },
            { "09-28", "教師節" },
            { "10-10", "國慶日" },
            { "10-25", "台灣光復日" },
            { "12-25", "行憲紀念日" },
        };

        var key = date.ToString("MM-dd");
        if (holidayNames.TryGetValue(key, out var name))
        {
            return name;
        }

        // 檢查是否為春節期間
        var lunarNewYear = GetLunarHolidays(date.Year);
        if (lunarNewYear.Any(d => d.Date == date.Date))
        {
            return "春節";
        }

        return "";
    }
}
