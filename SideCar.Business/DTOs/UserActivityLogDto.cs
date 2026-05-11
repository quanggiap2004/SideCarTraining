using SideCar.Business.Entities;
using SideCar.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SideCar.Business.DTOs
{
    public class UserActivityLogDto
    {
        public Guid id { get; set; }
        public ActivityType ActivityType { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
