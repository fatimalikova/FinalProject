using AppointmentAPP.Dtos.AppointmentDtos;

namespace AppointmentAPP.Services.Interfaces
{
    public interface IAppointmentService
    {
        Task<AppointmentResponseDto> BookAsync(Guid clientId, BookAppointmentDto dto);
        Task<AppointmentResponseDto> CancelAsync(Guid userId, Guid appointmentId, CancelAppointmentDto dto);
        Task<AppointmentResponseDto> RescheduleAsync(Guid userId, Guid appointmentId, RescheduleAppointmentDto dto);
        Task<AppointmentResponseDto> CompleteAsync(Guid providerUserId, Guid appointmentId);
        Task<List<AppointmentResponseDto>> GetMyAppointmentsAsync(Guid clientUserId, string? filter);
        Task<List<AppointmentResponseDto>> GetProviderCalendarAsync(Guid providerUserId, DateTime from, DateTime to);
        Task AutoCompletePastAppointmentsAsync(); // background job çağırır
    }
}
