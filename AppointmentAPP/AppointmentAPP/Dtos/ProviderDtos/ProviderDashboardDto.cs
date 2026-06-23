namespace AppointmentAPP.Dtos.ProviderDtos
{
    public class ProviderDashboardDto
    {
        public ResponseProviderDto Profile { get; set; }
        public int TotalServices { get; set; }
        public int TodayAppointmentsCount { get; set; }
        public int UpcomingWeekAppointmentsCount { get; set; }

        //public int PendingReminders { get; set; }
        public int TotalFollowers { get; set; }
    }
}
