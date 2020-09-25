using System;
using System.Text;
using System.Linq;
namespace RoleHandlerBot {
    public class BigNumber {
        public static string ParseValueToTokenDecimal(string value, int dec) {
            int currentDecimals;
            int periodIndex = -1;
            for (int i = 0; i < value.Length; i++) {
                if (value[i] == '.') {
                    periodIndex = i;
                    break;
                }
            }
            if (periodIndex != -1)
                currentDecimals = value.Length - periodIndex - 1;
            else
                currentDecimals = 0;
            value = value.Replace(".", "");
            return value.PadRight(value.Length + dec - currentDecimals, '0').TrimStart('0');
        }

        public static bool IsValidValue(string value, int dec) {
            int periodCount = 0;
            int periodIndex = -1;
            for (int i = 0; i < value.Length; i++) {
                if (!char.IsNumber(value[i])) {
                    if (value[i] != '.')
                        return false;
                    else {
                        periodCount++;
                        periodIndex = i;
                    }
                }
                if (periodCount > 1)
                    return false;
            }
            if (periodIndex != -1 && periodIndex != value.Length - 1)
                if (value.Length - periodIndex - 1 > dec)
                    return false;
            return true;
        }

        public static string FormatUint(string str, int dec, bool noSpace = false) {
            int periodIndex;
            if (str.Length <= dec) {
                str = str.PadLeft(dec, '0');
                str = "0." + str;
            }
            else
                str = str.Insert(str.Length - dec, ".");
            if (str.Last() == '.')
                str = str.Substring(0, str.Length - 1);
            else
                str = str.TrimEnd('0');
            if (noSpace)
                return str;
            periodIndex = str.IndexOf('.');
            if (periodIndex == -1)
                periodIndex = str.Length;
            var spaceCount = periodIndex / 3;
            var offset = 0;
            for (int i = 0; i < spaceCount; i++) {
                var index = i - offset + periodIndex % 3 + i * 3;
                if (index != 0)
                    str = str.Insert(index, " ");
                else
                    offset++;
            }
            if (str.Last() == '.')
                str = str.Substring(0, str.Length - 1);
            return str;
        }
    }
}
