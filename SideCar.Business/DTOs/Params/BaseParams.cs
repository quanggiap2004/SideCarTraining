using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SideCar.Business.DTOs.Params
{
    public class BaseParams
    {
        private const int MaxPageSize = 30;
        private int _pageSize = 10;
        public int Page { get; set; } = 1;
        public int PageSize
        {
            get => _pageSize; 
            set => _pageSize = value < 1 ? 1 : (value > MaxPageSize ? MaxPageSize : value);
        }
        public bool IsDescending { get; set; } = false;
    }
}
