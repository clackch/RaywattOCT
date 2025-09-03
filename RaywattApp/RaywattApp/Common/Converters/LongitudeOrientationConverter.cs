using System;
using System.Globalization;
using System.Windows.Data;
using RaywattApp.Common.Enums; // <-- 여기 중요: enum 네임스페이스

namespace RaywattApp.Common.Converters
{
    /// <summary>
    /// RadioButton.IsChecked <-> LongitudeOrientation(enum) 변환기
    /// Convert:   VM의 값(value) == 이 라디오가 대표하는 값(parameter) ? true : false
    /// ConvertBack: 체크되면 parameter(enum)를 VM에 반영, 체크 해제면 DoNothing
    /// </summary>
    public class LongitudeOrientationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null || parameter is null) return false;

            if (value is LongitudeOrientation current)
            {
                // parameter는 보통 x:Static 으로 enum 인스턴스가 들어옵니다.
                if (parameter is LongitudeOrientation target)
                    return current == target;

                // 혹시 문자열로 온 경우 방어
                if (parameter is string s &&
                    Enum.TryParse(s, ignoreCase: true, out LongitudeOrientation parsed))
                    return current == parsed;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 체크된 라디오만 enum 값을 반환, 체크 해제는 값 변경 없음
            if (value is bool isChecked && isChecked && parameter != null)
            {
                if (parameter is LongitudeOrientation target)
                    return target;

                if (parameter is string s &&
                    Enum.TryParse(s, ignoreCase: true, out LongitudeOrientation parsed))
                    return parsed;
            }

            return Binding.DoNothing;
        }
    }
}
