namespace SideCar.Business.DTOs
{
    public class Pagination<T>(IReadOnlyList<T> items, int total, int page, int size)
    {
        public int Size { get; set; } = size;
        public int Page { get; set; } = page;
        public int Total { get; set; } = total;
        public int TotalPages { get; set; } = (int)Math.Ceiling((decimal)total / size);
        public IReadOnlyList<T> Items { get; set; } = items;
    }
}
