using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;

namespace NicoPlayerHohoema.Helpers
{
    public static class ApiContractHelper
    {
        public static bool IsFallCreatorsUpdateAvailable
        {
            get 
            {
                return ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5);
            }
        }

        public static bool IsCreatorsUpdateAvailable
        {
            get
            {
                return ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4);
            }
        }

        public static bool IsAnniversaryUpdateAvailable
        {
            get
            {
                return ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 3);
            }
        }
    }
}
