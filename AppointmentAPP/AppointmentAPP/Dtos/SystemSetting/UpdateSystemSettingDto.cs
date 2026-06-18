using System.ComponentModel.DataAnnotations;

namespace AppointmentAPP.Dtos.SystemSetting
{
    public class UpdateSystemSettingDto
    {
        [Range(0, 168)]
        public int MinCancellationNoticeHours { get; set; }

        [Range(1, 365)]
        public int MaxAdvanceBookingDays { get; set; }

        [Range(5, 120)]
        public int DefaultSlotIntervalMinutes { get; set; }

        [Range(1, 168)]
        public int ReminderHoursBeforeAppointment { get; set; }

        public bool RequireProviderApproval { get; set; }
    }
}
