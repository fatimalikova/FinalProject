using AppointmentAPP.Dtos.AppointmentDtos;
using AppointmentAPP.Dtos.ClientDtos;

namespace AppointmentAPP.Interfaces
{
    public interface IAppointmentService
    {
        Task<ResponseAppointmentDto> BookAsync(Guid clientId, BookAppointmentDto dto);
        Task<ResponseAppointmentDto> CancelAsync(Guid userId, Guid appointmentId, CancelAppointmentDto dto);
        Task<ResponseAppointmentDto> RescheduleAsync(Guid userId, Guid appointmentId, RescheduleAppointmentDto dto);
        Task<ResponseAppointmentDto> CompleteAsync(Guid providerUserId, Guid appointmentId);
        Task<List<ResponseAppointmentDto>> GetMyAppointmentsAsync(Guid clientUserId, string? filter);
        Task<List<ResponseAppointmentDto>> GetProviderCalendarAsync(Guid providerUserId, DateTime from, DateTime to);
        Task AutoCompletePastAppointmentsAsync();
        Task<ResponseAppointmentDto> GetByIdAsync(Guid userId, Guid appointmentId);
        Task<ResponseClientProfileDto> GetMyClientProfileAsync(Guid userId);
        Task<List<ResponseAppointmentDto>> GetAllForAdminAsync(string? status, DateTime? from, DateTime? to);
    }
}
