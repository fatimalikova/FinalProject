using AppointmentAPP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointmentAPP.Data.Configurations
{
    public class ProviderPostConfiguration : IEntityTypeConfiguration<ProviderPost>
    {
        public void Configure(EntityTypeBuilder<ProviderPost> builder)
        {
            builder.Property(p => p.Caption).HasMaxLength(500);
            builder.Property(p => p.ImageUrl).IsRequired().HasMaxLength(1000);

            builder.HasMany(p => p.Likes)
                .WithOne(l => l.Post)
                .HasForeignKey(l => l.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.Comments)
                .WithOne(c => c.Post)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
