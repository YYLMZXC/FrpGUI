using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Diagnostics;
using System.Globalization;

namespace FrpGUI.Avalonia.Converters
{
    public class NullableValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Nullable.GetUnderlyingType(targetType) == null)
            {
                throw new ArgumentException("目标类型不可为空", nameof(targetType));
            }
            if (value == null)
            {
                return null;
            }
            if (value is not string str)
            {
                throw new ArgumentException("值不是字符串", nameof(value));
            }
            if (string.IsNullOrWhiteSpace(str))
            {
                return null;
            }
            Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            try
            {
                return underlyingType switch
                {
                    // 基本数值类型
                    Type t when t == typeof(int) => int.Parse(str, culture),
                    Type t when t == typeof(double) => double.Parse(str, culture),
                    Type t when t == typeof(float) => float.Parse(str, culture),
                    Type t when t == typeof(decimal) => decimal.Parse(str, culture),
                    Type t when t == typeof(long) => long.Parse(str, culture),
                    Type t when t == typeof(short) => short.Parse(str, culture),
                    Type t when t == typeof(byte) => byte.Parse(str, culture),
                    Type t when t == typeof(sbyte) => sbyte.Parse(str, culture),
                    Type t when t == typeof(uint) => uint.Parse(str, culture),
                    Type t when t == typeof(ulong) => ulong.Parse(str, culture),
                    Type t when t == typeof(ushort) => ushort.Parse(str, culture),

                    // 布尔类型
                    Type t when t == typeof(bool) => bool.Parse(str),

                    // 日期时间
                    Type t when t == typeof(DateTime) => DateTime.Parse(str, culture),
                    Type t when t == typeof(DateTimeOffset) => DateTimeOffset.Parse(str, culture),
                    Type t when t == typeof(TimeSpan) => TimeSpan.Parse(str, culture),

                    // 其他类型（如 Guid）
                    Type t when t == typeof(Guid) => Guid.Parse(str),

                    // 枚举类型（AOT 需要确保枚举被编译）
                    Type t when t.IsEnum => Enum.Parse(t, str, ignoreCase: true),

                    // 默认情况（可能不支持）
                    _ => throw new NotSupportedException($"不支持转换到类型 {underlyingType.Name}")
                };
            }
            catch (FormatException ex)
            {
                //return new BindingNotification(new ArgumentException($"无法将值 '{str}' 转换为类型 {underlyingType.Name}", nameof(targetType)),BindingErrorType.DataValidationError)
                throw new ArgumentException($"无法将值 '{str}' 转换为类型 {underlyingType.Name}", nameof(targetType));
            }
        }
    }
}