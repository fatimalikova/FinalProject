using AppointmentAPP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointmentAPP.Data.Configurations
{
    public class FollowConfiguration : IEntityTypeConfiguration<Follow>
    {
        public void Configure(EntityTypeBuilder<Follow> builder)
        {
            builder.HasIndex(f => new { f.FollowerId, f.ProviderId }).IsUnique();

            builder.HasOne(f => f.Follower)
                .WithMany()
                .HasForeignKey(f => f.FollowerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(f => f.Provider)
                .WithMany(p => p.Followers)
                .HasForeignKey(f => f.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
