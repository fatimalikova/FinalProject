using AppointmentAPP.Models.Entity;

namespace AppointmentAPP.Models
{
    public class SystemSetting : BaseEntity
    {
        // Booking qaydaları
        public int MinCancellationNoticeHours { get; set; } = 24;
        public int MaxAdvanceBookingDays { get; set; } = 30;
        public int DefaultSlotIntervalMinutes { get; set; } = 15;

        // Reminder qaydaları
        public int ReminderHoursBeforeAppointment { get; set; } = 1;

        // Provider approval
        public bool RequireProviderApproval { get; set; } = true;
    }
}
