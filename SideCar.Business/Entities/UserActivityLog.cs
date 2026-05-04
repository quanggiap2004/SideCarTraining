using SideCar.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SideCar.Business.Entities
{
    public class UserActivityLog : BaseEntity
    {
        public ActivityType ActivityType { get; set; }
        public Guid UserId { get; set; }
        public Users? User { get; set; }
    }
}
