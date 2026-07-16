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
               .HasConversion<string>()   
               .HasMaxLength(30)
               .IsRequired();

            builder.Property(a => a.Notes).HasMaxLength(500);
            builder.Property(a => a.CancelReason).HasMaxLength(300);

            builder.Property(a => a.PriceAtBooking)
                    .HasColumnType("decimal(10,2)");

            builder.HasOne(a => a.Client)
                .WithMany(u => u.Appointments)
                .HasForeignKey(a => a.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(a => new { a.ProviderId, a.StartDateTime });
        }
    }
}
