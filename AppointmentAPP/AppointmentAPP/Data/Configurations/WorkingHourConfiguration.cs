using AppointmentAPP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointmentAPP.Data.Configurations
{
    public class WorkingHourConfiguration :IEntityTypeConfiguration<WorkingHour>
    {
        public void Configure(EntityTypeBuilder<WorkingHour> builder)
        {
            builder.Property(w => w.Day).IsRequired();
            builder.Property(w => w.StartTime).IsRequired();
            builder.Property(w => w.EndTime).IsRequired();
        }
    
    }
}
