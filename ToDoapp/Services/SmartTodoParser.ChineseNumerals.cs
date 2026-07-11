using System;
using System.Collections.Frozen;

namespace ToDoapp.Services;

/// <summary>
/// SmartTodoParser 的中文数字解析逻辑。
/// </summary>
public partial class SmartTodoParser
{
    private static readonly FrozenDictionary<char, int> ChineseDigitMap = new Dictionary<char, int>
    {
        ['〇'] = 0,
        ['零'] = 0,
        ['一'] = 1,
        ['二'] = 2,
        ['两'] = 2,
        ['三'] = 3,
        ['四'] = 4,
        ['五'] = 5,
        ['六'] = 6,
        ['七'] = 7,
        ['八'] = 8,
        ['九'] = 9
    }.ToFrozenDictionary();

    private static bool TryParseChineseNumber(string text, out int value)
    {
        value = 0;
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        if (text.Length == 1 && ChineseDigitMap.TryGetValue(text[0], out value))
        {
            return true;
        }

        int result = 0;
        int current = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '十')
            {
                if (current == 0)
                {
                    result += 10;
                }
                else
                {
                    result += current * 10;
                    current = 0;
                }
            }
            else if (ChineseDigitMap.TryGetValue(c, out var d))
            {
                current = d;
                if (i + 1 >= text.Length || text[i + 1] != '十')
                {
                    result += current;
                    current = 0;
                }
            }
            else
            {
                return false;
            }
        }

        value = result;
        return value is >= 0 and <= 99;
    }
}
