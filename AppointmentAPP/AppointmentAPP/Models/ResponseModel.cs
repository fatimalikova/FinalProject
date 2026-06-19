namespace AppointmentAPP.Models
{
    public class ResponseModel<T>
    {
        public bool Success { get; set; }
        public List<string> Errors { get; set; } = new();
        public T? Data { get; set; }
        public int StatusCode { get; set; }
    }
}
