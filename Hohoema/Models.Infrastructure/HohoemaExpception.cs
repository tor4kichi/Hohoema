using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Infrastructure
{
    public sealed class HohoemaExpception : Exception
    {
        public HohoemaExpception()
        {
        }

        public HohoemaExpception(string message) : base(message)
        {
        }

        public HohoemaExpception(string message, Exception innerException) : base(message, innerException)
        {
        }

        public HohoemaExpception(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
