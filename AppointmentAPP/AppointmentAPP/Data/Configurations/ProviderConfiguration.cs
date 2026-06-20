using AppointmentAPP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointmentAPP.Data.Configurations
{
    public class ProviderConfiguration : IEntityTypeConfiguration<Provider>
    {
        public void Configure(EntityTypeBuilder<Provider> builder)
        {
            builder.Property(p => p.BusinessName)
               .IsRequired()
               .HasMaxLength(150);

            builder.Property(p => p.Category)
                .IsRequired()
                .HasMaxLength(80);

            builder.Property(p => p.Description)
                .HasMaxLength(1000);

            builder.HasMany(p => p.Services)
                .WithOne(s => s.Provider)
                .HasForeignKey(s => s.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.WorkingHours)
                .WithOne(w => w.Provider)
                .HasForeignKey(w => w.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.UnavailableDays)
                .WithOne(u => u.Provider)
                .HasForeignKey(u => u.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Multiple cascade path problemi olmasın deyə Restrict
            builder.HasMany(p => p.Appointments)
                .WithOne(a => a.Provider)
                .HasForeignKey(a => a.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(p => p.Reviews)
                .WithOne(r => r.Provider)
                .HasForeignKey(r => r.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(p => p.Posts)
            .WithOne(post => post.Provider)
            .HasForeignKey(post => post.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
