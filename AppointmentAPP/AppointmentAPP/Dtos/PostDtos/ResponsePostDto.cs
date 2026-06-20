namespace AppointmentAPP.Dtos.PostDtos
{
    public class ResponsePostDto
    {
        public Guid Id { get; set; }
        public string? Caption { get; set; }
        public string ImageUrl { get; set; }
        public Guid ProviderId { get; set; }
        public string ProviderBusinessName { get; set; }
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
