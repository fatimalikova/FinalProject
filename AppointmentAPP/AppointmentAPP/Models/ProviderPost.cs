using AppointmentAPP.Models.Entity;

namespace AppointmentAPP.Models
{
    public class ProviderPost : BaseEntity
    {
        public string Caption { get; set; }
        public string ImageUrl { get; set; }

        public Guid ProviderId { get; set; }
        public Provider Provider { get; set; }

        public List<PostLike> Likes { get; set; } = new();
        public List<PostComment> Comments { get; set; } = new();
    }
}
