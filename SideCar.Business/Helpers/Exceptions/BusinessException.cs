using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SideCar.Business.Helpers.Exceptions
{
    public class BusinessException(string? message, Exception? innerException = null) : Exception(message, innerException)
    {
    }
}
