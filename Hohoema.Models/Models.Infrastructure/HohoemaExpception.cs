using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Infrastructure
{
    public class HohoemaException : Exception
    {
        public HohoemaException()
        {
        }

        public HohoemaException(string message) : base(message)
        {
        }

        public HohoemaException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public HohoemaException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
