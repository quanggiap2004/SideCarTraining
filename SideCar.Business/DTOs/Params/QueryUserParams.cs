using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SideCar.Business.DTOs.Params
{
    public class QueryUserParams : BaseParams
    {
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public bool? IsDeleted { get; set; } = false;
    }
}
