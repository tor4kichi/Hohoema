using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable enable
namespace Hohoema.Helpers;

public static class NumberToKMGTPEZYStringHelper
{
    const string KMGTPEZY = "KMGTPEZY";

    public static string ToKMGTPEZY(double number)
    {
        int divCount = -1;
        while (number >= 1000.0d)
        {
            number /= 1000.0d;
            divCount++;
        }

        if (divCount >= KMGTPEZY.Length)
        {
            throw new NotSupportedException("ヨタより大きい桁数は対応してない");
        }
        else if (divCount >= 2 /* G 以上なら */)
        {
            return number.ToString("F2") + KMGTPEZY[divCount];
        }
        else if (divCount >= 0 /* K 以上なら */)
        {
            return number.ToString("F0") + KMGTPEZY[divCount];
        }
        else
        {
            return number.ToString("F0");
        }
    }

    public static string ToKMGTPEZY(int number)
    {
        int divCount = -1;
        while (number >= 1000)
        {
            number /= 1000;
            divCount++;
        }

        if (divCount >= KMGTPEZY.Length)
        {
            throw new NotSupportedException("ヨタより大きい桁数は対応してない");
        }
        else if (divCount >= 2 /* G 以上なら */)
        {
            return number.ToString("F2") + KMGTPEZY[divCount];
        }
        else if (divCount >= 0 /* K 以上なら */)
        {
            return number.ToString("F0") + KMGTPEZY[divCount];
        }
        else
        {
            return number.ToString("F0");
        }
    }
}
