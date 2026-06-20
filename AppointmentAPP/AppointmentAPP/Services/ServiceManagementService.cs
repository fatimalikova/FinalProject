using AppointmentAPP.Data;
using AppointmentAPP.Dtos.ServiceDtos;
using AppointmentAPP.Models;
using AppointmentAPP.Services.Interfaces;
using AppointmentAPP.Exceptions;
using Microsoft.EntityFrameworkCore;
using ServiceEntity = AppointmentAPP.Models.Service;

namespace AppointmentAPP.Services
{
    public class ServiceManagementService(AppDbContext db) : IServiceManagementService
    {
        public async Task<ResponseServiceDto> CreateAsync(Guid userId, CreateServiceDto dto)
        {
            var provider = await GetOwnedProviderAsync(userId);

            var service = new ServiceEntity
            {
                ProviderId = provider.Id,
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                DurationMinutes = dto.DurationMinutes
            };

            db.Services.Add(service);
            await db.SaveChangesAsync();

            return MapToDto(service, provider.BusinessName);
        }

        public async Task<ResponseServiceDto> UpdateAsync(Guid userId, Guid serviceId, UpdateServiceDto dto)
        {
            var provider = await GetOwnedProviderAsync(userId);

            var service = await db.Services
                .FirstOrDefaultAsync(s => s.Id == serviceId && s.ProviderId == provider.Id)
                ?? throw new NotFoundException("Service not found.");

            service.Name = dto.Name;
            service.Description = dto.Description;
            service.Price = dto.Price;
            service.DurationMinutes = dto.DurationMinutes;
            service.IsActive = dto.IsActive;
            service.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return MapToDto(service, provider.BusinessName);
        }

        public async Task DeleteAsync(Guid userId, Guid serviceId)
        {
            var provider = await GetOwnedProviderAsync(userId);

            var service = await db.Services
                .FirstOrDefaultAsync(s => s.Id == serviceId && s.ProviderId == provider.Id)
                ?? throw new NotFoundException("Service not found.");

            var hasAppointments = await db.Appointments.AnyAsync(a => a.ServiceId == serviceId);

            if (hasAppointments)
            {
                service.IsActive = false; // tarixçə qorunsun
                service.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                db.Services.Remove(service);
            }

            await db.SaveChangesAsync();
        }

        public async Task<List<ResponseServiceDto>> GetByProviderAsync(Guid providerId)
        {
            var provider = await db.Providers.FindAsync(providerId)
                ?? throw new NotFoundException("Provider not found.");

            var services = await db.Services
                .Where(s => s.ProviderId == providerId && s.IsActive)
                .ToListAsync();

            return services.Select(s => MapToDto(s, provider.BusinessName)).ToList();
        }

        public async Task<List<ResponseServiceDto>> GetMyServicesAsync(Guid userId)
        {
            var provider = await GetOwnedProviderAsync(userId);

            var services = await db.Services.Where(s => s.ProviderId == provider.Id).ToListAsync();
            return services.Select(s => MapToDto(s, provider.BusinessName)).ToList();
        }

        private async Task<Provider> GetOwnedProviderAsync(Guid userId)
        {
            return await db.Providers.FirstOrDefaultAsync(p => p.UserId == userId)
                ?? throw new NotFoundException("Provider profile not found. Create your provider profile first.");
        }

        private static ResponseServiceDto MapToDto(ServiceEntity s, string providerName) => new()
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            Price = s.Price,
            DurationMinutes = s.DurationMinutes,
            IsActive = s.IsActive,
            ProviderId = s.ProviderId,
            ProviderBusinessName = providerName
        };


        public async Task<List<ResponseServiceDto>> GetAllForAdminAsync(Guid? providerId, bool? isActive)
        {
            var query = db.Services.Include(s => s.Provider).AsQueryable();

            if (providerId.HasValue)
                query = query.Where(s => s.ProviderId == providerId.Value);

            if (isActive.HasValue)
                query = query.Where(s => s.IsActive == isActive.Value);

            var services = await query.OrderByDescending(s => s.CreatedAt).ToListAsync();

            return services.Select(s => MapToDto(s, s.Provider.BusinessName)).ToList();
        }

        public async Task DeactivateByAdminAsync(Guid serviceId)
        {
            var service = await db.Services.FirstOrDefaultAsync(s => s.Id == serviceId)
                ?? throw new NotFoundException("Service not found.");

            service.IsActive = false;
            service.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }
}
