using AppointmentAPP.Data;
using AppointmentAPP.Dtos.ServiceDtos;
using AppointmentAPP.Enums;
using AppointmentAPP.Exceptions;
using AppointmentAPP.Interfaces;
using AppointmentAPP.Models;
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

            if (service.DeactivatedByAdmin && dto.IsActive)
                throw new BadRequestException("This service was deactivated by an administrator and cannot be reactivated. Contact support.");

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
            service.DeactivatedByAdmin = true;
            service.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        public async Task ReactivateByAdminAsync(Guid serviceId)
        {
            var service = await db.Services.FirstOrDefaultAsync(s => s.Id == serviceId)
                ?? throw new NotFoundException("Service not found.");

            service.IsActive = true;
            service.DeactivatedByAdmin = false;
            service.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        public async Task<List<ServiceCatalogItemDto>> GetCatalogAsync()
        {
            var services = await db.Services
                .Where(s => s.IsActive
                            && s.Provider.Status == ProviderStatus.Approved
                            && s.Provider.IsActive)
                .Select(s => new { s.Name, s.Price })
                .ToListAsync();

            return services
                .GroupBy(s => s.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => new ServiceCatalogItemDto
                {
                    Name = g.Key,
                    MinPrice = g.Min(x => x.Price),
                    MaxPrice = g.Max(x => x.Price),
                    ProviderCount = g.Count()
                })
                .OrderBy(x => x.Name)
                .ToList();
        }

        public async Task<List<ServiceSearchResultDto>> SearchByNameAsync(string name)
        {
            var normalized = name.Trim().ToLower();

            var services = await db.Services
                .Include(s => s.Provider)
                .Where(s => s.IsActive
                            && s.Provider.Status == ProviderStatus.Approved
                            && s.Provider.IsActive
                            && s.Name.Trim().ToLower() == normalized)
                .ToListAsync();

            var providerIds = services.Select(s => s.ProviderId).Distinct().ToList();

            var reviewStats = await db.Reviews
                .Where(r => providerIds.Contains(r.ProviderId))
                .GroupBy(r => r.ProviderId)
                .Select(g => new { ProviderId = g.Key, Avg = g.Average(r => r.Rating), Count = g.Count() })
                .ToListAsync();

            return services.Select(s =>
            {
                var stats = reviewStats.FirstOrDefault(r => r.ProviderId == s.ProviderId);
                return new ServiceSearchResultDto
                {
                    ServiceId = s.Id,
                    ServiceName = s.Name,
                    Price = s.Price,
                    DurationMinutes = s.DurationMinutes,
                    ProviderId = s.ProviderId,
                    ProviderBusinessName = s.Provider.BusinessName,
                    ProviderCategory = s.Provider.Category,
                    ProviderAddress = s.Provider.Address,
                    AverageRating = stats is not null ? Math.Round(stats.Avg, 1) : 0,
                    ReviewCount = stats?.Count ?? 0
                };
            })
            .OrderByDescending(x => x.AverageRating)
            .ToList();
        }
    }
}
