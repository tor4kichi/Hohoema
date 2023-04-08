namespace Hohoema.Helpers;

public static class DeviceTypeHelper
{
    public static bool IsXbox { get; } = Microsoft.Toolkit.Uwp.Helpers.SystemInformation.Instance.DeviceFamily.EndsWith("Xbox");

    public static bool IsDesktop { get; } = Microsoft.Toolkit.Uwp.Helpers.SystemInformation.Instance.DeviceFamily.EndsWith("Desktop");

    public static bool IsMobile { get; } = Microsoft.Toolkit.Uwp.Helpers.SystemInformation.Instance.DeviceFamily.EndsWith("Mobile");

    public static bool IsIot { get; } = Microsoft.Toolkit.Uwp.Helpers.SystemInformation.Instance.DeviceFamily.EndsWith("Iot");
}
