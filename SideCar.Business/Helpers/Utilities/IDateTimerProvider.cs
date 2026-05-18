using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SideCar.Business.Helpers.Utilities
{
    public interface IDateTimerProvider
    {
        DateTime GetUtcNow { get; }
    }

    public class SystemDateTimeProvider : IDateTimerProvider
    {
        public DateTime GetUtcNow => DateTime.UtcNow;
    }
}
