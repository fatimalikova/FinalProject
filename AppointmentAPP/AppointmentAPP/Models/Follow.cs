using AppointmentAPP.Models.Entity;

namespace AppointmentAPP.Models
{
    public class Follow : BaseEntity
    {
        public Guid FollowerId { get; set; }
        public AppUser Follower { get; set; }

        public Guid ProviderId { get; set; }
        public Provider Provider { get; set; }
    }
}
