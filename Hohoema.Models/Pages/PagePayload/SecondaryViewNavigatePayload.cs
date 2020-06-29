using Hohoema.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Pages.PagePayload
{
    public class SecondaryViewNavigatePayload : PagePayloadBase<SecondaryViewNavigatePayload>
    {
        public string ContentId { get; set; }
        public string Title { get; set; }
        public string ContentType { get; set; }
    }
}
