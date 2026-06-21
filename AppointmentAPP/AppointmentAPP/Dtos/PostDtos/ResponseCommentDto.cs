namespace AppointmentAPP.Dtos.PostDtos
{
    public class ResponseCommentDto
    {
        public Guid Id { get; set; }
        public string Content { get; set; }
        public Guid UserId { get; set; }
        public string UserFullName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
