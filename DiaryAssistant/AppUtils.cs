using DiaryAssistant.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;

namespace DiaryAssistant.Utils
{
    // Booleanを反転するコンバーター
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value;
        }
    }

    // Booleanを表示状態に変換するコンバーター
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }

    // タイムスパンを文字列に変換するコンバーター
    public class TimeSpanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan timeSpan)
            {
                return timeSpan.ToString(@"hh\:mm");
            }
            return "00:00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string timeString)
            {
                if (TimeSpan.TryParse(timeString, out TimeSpan result))
                {
                    return result;
                }
            }
            return TimeSpan.Zero;
        }
    }

    // 日記データを持つ日付をカレンダーに表示するためのコンバーター
    public class CalendarDayToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date)
            {
                var diaryEntry = DiaryAssistant.Services.DatabaseService.GetDiaryEntry(date);
                if (diaryEntry != null)
                {
                    return System.Windows.Media.Brushes.LightBlue;
                }
            }
            return System.Windows.Media.Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // AIレスポンス解析ユーティリティ
    public static class AiResponseParser
    {
        // レスポンスから感情を抽出
        public static string ExtractEmotion(string response)
        {
            try
            {
                var match = Regex.Match(response, @"<response\s+emotion=""([^""]+)"">");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"感情抽出エラー: {ex.Message}");
            }

            return "normal";
        }

        // レスポンスからメッセージを抽出
        public static string ExtractMessage(string response)
        {
            try
            {
                var match = Regex.Match(response, @"<response\s+emotion=""[^""]+"">(.*?)<\/response>", RegexOptions.Singleline);
                if (match.Success)
                {
                    // タグの前のテキストとタグ内のテキストを結合
                    int tagStart = response.IndexOf("<response");
                    string textBeforeTag = tagStart > 0 ? response.Substring(0, tagStart).Trim() : "";
                    string tagContent = match.Groups[1].Value.Trim();

                    // 両方のテキストを結合（間に空白を入れて）
                    return string.IsNullOrEmpty(textBeforeTag)
                        ? tagContent
                        : textBeforeTag + " " + tagContent;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"メッセージ抽出エラー: {ex.Message}");
            }

            return response;
        }

        // レスポンスから要約を抽出
        public static string ExtractSummary(string response)
        {
            try
            {
                var match = Regex.Match(response, @"<summary>(.*?)<\/summary>", RegexOptions.Singleline);
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"要約抽出エラー: {ex.Message}");
            }

            return null;
        }

        // レスポンスから過去イベント情報を抽出
        public static EventUpdateInfo ExtractEventUpdate(string response)
        {
            try
            {
                var match = Regex.Match(response,
                    @"<eventUpdate\s+date=""([^""]+)""\s+type=""([^""]+)"">\s*<description>(.*?)<\/description>\s*<timeExpression>(.*?)<\/timeExpression>\s*<\/eventUpdate>",
                    RegexOptions.Singleline);

                if (match.Success)
                {
                    return new EventUpdateInfo
                    {
                        Date = DateTime.Parse(match.Groups[1].Value),
                        Type = match.Groups[2].Value,
                        Description = match.Groups[3].Value.Trim(),
                        TimeExpression = match.Groups[4].Value.Trim()
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"イベント更新情報抽出エラー: {ex.Message}");
            }

            return null;
        }
    }

    // 日付操作ユーティリティ
    public static class DateTimeUtils
    {
        // 相対的な日付表現から日付を算出
        public static DateTime ParseRelativeDate(string expression, DateTime baseDate)
        {
            expression = expression.ToLower();

            if (expression.Contains("昨日"))
            {
                return baseDate.AddDays(-1);
            }
            else if (expression.Contains("一昨日") || expression.Contains("おととい"))
            {
                return baseDate.AddDays(-2);
            }
            else if (expression.Contains("先週"))
            {
                return baseDate.AddDays(-7);
            }
            else if (Regex.IsMatch(expression, @"\d+日前"))
            {
                var match = Regex.Match(expression, @"(\d+)日前");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int days))
                {
                    return baseDate.AddDays(-days);
                }
            }

            return baseDate;
        }
    }
}