using AppointmentAPP.Models.Entity;

namespace AppointmentAPP.Models
{
    public class PostComment : BaseEntity
    {
        public string Content { get; set; }

        public Guid PostId { get; set; }
        public ProviderPost Post { get; set; }

        public Guid UserId { get; set; }
        public AppUser User { get; set; }
    }
}
