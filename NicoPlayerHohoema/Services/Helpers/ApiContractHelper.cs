using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;

namespace NicoPlayerHohoema.Services.Helpers
{
    public static class ApiContractHelper
    {
        /// <summary>
        /// 1809
        /// </summary>
        public static bool Is2018FallUpdateAvailable
        {
            get
            {
                return ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7);
            }
        }


        /// <summary>
        /// 1803
        /// </summary>
        public static bool Is2018SpringUpdateAvailable
        {
            get
            {
                return ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 6);
            }
        }

        /// <summary>
        /// 1709
        /// </summary>
        public static bool IsFallCreatorsUpdateAvailable
        {
            get 
            {
                return ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5);
            }
        }

        /// <summary>
        /// 1703
        /// </summary>
        public static bool IsCreatorsUpdateAvailable
        {
            get
            {
                return ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4);
            }
        }

        /// <summary>
        /// 1607
        /// </summary>
        public static bool IsAnniversaryUpdateAvailable
        {
            get
            {
                return ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 3);
            }
        }
    }
}
