﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models
{
    public enum HohoemaAppServiceLevel
    {
        Offline,
        ServiceUnavailable,
        WithoutLoggedIn,
        LoggedIn,
    }


    public static class HohoemaAppServiceLevelHelper
    {
        public static bool IsLoggedIn(this HohoemaAppServiceLevel serviceLevel)
        {
            return serviceLevel >= HohoemaAppServiceLevel.LoggedIn;
        }
        public static bool IsOutOfService(this HohoemaAppServiceLevel serviceLevel)
        {
            return serviceLevel < HohoemaAppServiceLevel.WithoutLoggedIn;
        }
    }
    
}
