using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico
{
    internal static class ContentManageResultMapper
    {
        public static ContentManageResult ToModelContentManageResult(this Mntone.Nico2.ContentManageResult result)
        {
            return (ContentManageResult)result;
        }

        public static Mntone.Nico2.ContentManageResult ToInfrastructureContentManageResult(this ContentManageResult result)
        {
            return (Mntone.Nico2.ContentManageResult)result;
        }
    }
}
