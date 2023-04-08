using Windows.Foundation.Metadata;

namespace Hohoema.Helpers;

public static class ApiContractHelper
{
    /// <summary>
    /// 1809
    /// </summary>
    public static bool Is2018FallUpdateAvailable => ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7);


    /// <summary>
    /// 1803
    /// </summary>
    public static bool Is2018SpringUpdateAvailable => ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 6);

    /// <summary>
    /// 1709
    /// </summary>
    public static bool IsFallCreatorsUpdateAvailable => ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5);

    /// <summary>
    /// 1703
    /// </summary>
    public static bool IsCreatorsUpdateAvailable => ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4);

    /// <summary>
    /// 1607
    /// </summary>
    public static bool IsAnniversaryUpdateAvailable => ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 3);
}
