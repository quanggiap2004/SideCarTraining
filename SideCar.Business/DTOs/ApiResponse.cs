namespace SideCar.Business.DTOs
{
    public class BaseResponse<T>(string? message, T? data)
    {
        public string? Message { get; set; } = message;
        public T? Data { get; set; } = data;
    }
}
