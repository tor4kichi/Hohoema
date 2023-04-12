#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hohoema.Helpers;

// this code copied from http://stackoverflow.com/questions/273313/randomize-a-listt

public static class ListExtention
{
    public static IEnumerable<T> Shuffle<T>(
       this IEnumerable<T> source,
       Random generator = null)
    {
        generator ??= new Random();

        T[] elements = source.ToArray();
        for (int i = elements.Length - 1; i >= 0; i--)
        {
            int swapIndex = generator.Next(i + 1);
            yield return elements[swapIndex];
            elements[swapIndex] = elements[i];
        }
    }
}
