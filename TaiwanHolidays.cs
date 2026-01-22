using System;
using System.Collections.Generic;
using System.Linq;

namespace AlarmClock;

/// <summary>
/// 台灣國定假日管理類別
/// </summary>
public static class TaiwanHolidays
{
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
        holidays.AddRange(GetLunarNewYear(year));

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
            new DateTime(year, 6, 10),  // 端午節（假設，實際需農曆計算）
            new DateTime(year, 9, 17),  // 中秋節（假設，實際需農曆計算）
            new DateTime(year, 10, 10), // 國慶日
        };
    }

    /// <summary>
    /// 取得農曆新年假期（簡化版本，實際應使用農曆計算）
    /// </summary>
    private static List<DateTime> GetLunarNewYear(int year)
    {
        // 這裡使用簡化的農曆新年日期對照表
        // 實際應使用農曆轉換函式庫
        var lunarNewYearDates = new Dictionary<int, DateTime>
        {
            { 2024, new DateTime(2024, 2, 10) },
            { 2025, new DateTime(2025, 1, 29) },
            { 2026, new DateTime(2026, 2, 17) },
            { 2027, new DateTime(2027, 2, 6) },
            { 2028, new DateTime(2028, 1, 26) },
            { 2029, new DateTime(2029, 2, 13) },
            { 2030, new DateTime(2030, 2, 3) },
        };

        var holidays = new List<DateTime>();
        if (lunarNewYearDates.TryGetValue(year, out var lunarNewYear))
        {
            // 春節假期：除夕到初三（共4天）
            holidays.Add(lunarNewYear.AddDays(-1)); // 除夕
            holidays.Add(lunarNewYear);              // 初一
            holidays.Add(lunarNewYear.AddDays(1));   // 初二
            holidays.Add(lunarNewYear.AddDays(2));   // 初三
        }

        return holidays;
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
            { "10-10", "國慶日" },
        };

        var key = date.ToString("MM-dd");
        if (holidayNames.TryGetValue(key, out var name))
        {
            return name;
        }

        // 檢查是否為春節期間
        var lunarNewYear = GetLunarNewYear(date.Year);
        if (lunarNewYear.Any(d => d.Date == date.Date))
        {
            return "春節";
        }

        return "";
    }
}
