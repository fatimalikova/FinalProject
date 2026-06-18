using AppointmentAPP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointmentAPP.Data.Configurations
{
    public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
    {
        public void Configure(EntityTypeBuilder<Appointment> builder)
        {
            builder.Property(a => a.Status)
               .HasConversion<string>()   // enum DB-də string kimi saxlanılır (oxunaqlı olur)
               .HasMaxLength(30)
               .IsRequired();

            builder.Property(a => a.Notes).HasMaxLength(500);
            builder.Property(a => a.CancelReason).HasMaxLength(300);

            // AppUser (Client) silinəndə onun appointment-ləri də silinsin
            builder.HasOne(a => a.Client)
                .WithMany(u => u.Appointments)
                .HasForeignKey(a => a.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(a => new { a.ProviderId, a.StartDateTime });
        }
    }
}
