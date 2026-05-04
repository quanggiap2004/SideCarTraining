using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SideCar.Business.Entities
{
    public abstract class BaseEntity
    {
        public DateTime CreatedAt { get; set; }
        public Guid Id { get; set; }
    }
}
