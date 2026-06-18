namespace AppointmentAPP.Dtos.SystemSetting
{
    public class ResponseSystemSettingDto
    {
        public int MinCancellationNoticeHours { get; set; }
        public int MaxAdvanceBookingDays { get; set; }
        public int DefaultSlotIntervalMinutes { get; set; }
        public int ReminderHoursBeforeAppointment { get; set; }
        public bool RequireProviderApproval { get; set; }
    }
}
