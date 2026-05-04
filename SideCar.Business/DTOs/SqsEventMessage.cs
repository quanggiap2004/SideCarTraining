using System.Text.Json;

namespace SideCar.Business.DTOs
{
    public class SqsEventMessage
    {
        public string MessageType { get; set; } = string.Empty;
        public JsonElement Data { get; set; }
    }
}
