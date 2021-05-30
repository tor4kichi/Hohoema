using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit
{
    class NiconicoToolkitException : Exception
    {
        public NiconicoToolkitException()
        {
        }

        public NiconicoToolkitException(string message) : base(message)
        {
        }

        public NiconicoToolkitException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NiconicoToolkitException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
