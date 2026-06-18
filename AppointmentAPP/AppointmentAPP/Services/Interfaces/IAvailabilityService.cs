using AppointmentAPP.Dtos.Availability;

namespace AppointmentAPP.Services.Interfaces
{
    public interface IAvailabilityService
    {
        Task<List<AvailableSlotDto>> GetAvailableSlotsAsync(AvailabilityRequestDto request);
        Task<bool> IsSlotAvailableAsync(Guid providerId, DateTime startDateTime, DateTime endDateTime, Guid? excludeAppointmentId = null);
    }
}
