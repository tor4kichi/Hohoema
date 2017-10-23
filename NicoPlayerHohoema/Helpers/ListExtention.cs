using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Helpers
{
    // this code copied from http://stackoverflow.com/questions/273313/randomize-a-listt

    public static class ListExtention
    {
        public static IEnumerable<T> Shuffle<T>(
           this IEnumerable<T> source,
           Random generator = null)
        {
            if (generator == null)
            {
                generator = new Random();
            }

            var elements = source.ToArray();
            for (var i = elements.Length - 1; i >= 0; i--)
            {
                var swapIndex = generator.Next(i + 1);
                yield return elements[swapIndex];
                elements[swapIndex] = elements[i];
            }
        }
    }
}
