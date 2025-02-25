#nullable enable
using System.IO;
using System.Linq;

namespace Hohoema.Helpers;

public static class FilePathHelper
{
    public static string ToSafeFilePath(this string fileName)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        return new string(fileName
            .Where(x => !invalidChars.Any(y => x == y))
            .ToArray()
            );
    }

    public static string ToSafeDirectoryPath(this string fileName)
    {
        char[] invalidChars = Path.GetInvalidPathChars();
        return new string(fileName
            .Where(x => !invalidChars.Any(y => x == y))
            .ToArray()
            );
    }
}
