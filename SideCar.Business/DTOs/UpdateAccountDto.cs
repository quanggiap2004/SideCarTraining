using SideCar.Business.Helpers.ValidationRules;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SideCar.Business.DTOs
{
    public class UpdateAccountDto
    {
        [JsonIgnore]
        public Guid? Id { get; set; }
        public string? PhoneNumber {  get; set; }
        public string? FullName { get; set; }
        
        public string? UserName { get; set; }
    }
}
