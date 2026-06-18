using AppointmentAPP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointmentAPP.Data.Configurations
{
    public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
    {
        public void Configure(EntityTypeBuilder<SystemSetting> builder)
        {
            builder.Property(s => s.MinCancellationNoticeHours).IsRequired();
            builder.Property(s => s.MaxAdvanceBookingDays).IsRequired();
            builder.Property(s => s.DefaultSlotIntervalMinutes).IsRequired();
            builder.Property(s => s.ReminderHoursBeforeAppointment).IsRequired();
        }
    }
}
