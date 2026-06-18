using AppointmentAPP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointmentAPP.Data.Configurations
{
    public class UnavailableDayConfiguration : IEntityTypeConfiguration<UnavailableDay>
    {
        public void Configure(EntityTypeBuilder<UnavailableDay> builder)
        {
            builder.Property(u => u.Date).IsRequired();
            builder.Property(u => u.Reason).HasMaxLength(300);
        }
    }
}
