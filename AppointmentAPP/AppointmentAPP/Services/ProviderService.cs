using AppointmentAPP.Data;
using AppointmentAPP.Dtos.ProviderDtos;
using AppointmentAPP.Enums;
using AppointmentAPP.Exceptions;
using AppointmentAPP.Interfaces;
using AppointmentAPP.Models;
using Microsoft.EntityFrameworkCore;

namespace AppointmentAPP.Services
{
    public class ProviderService(AppDbContext db) : IProviderService
    {
        public async Task<ResponseProviderDto> RegisterAsync(Guid userId, CreateProviderDto dto)
        {
            var alreadyExists = await db.Providers.AnyAsync(p => p.UserId == userId);
            if (alreadyExists)
                throw new BadRequestException("This user already has a provider profile.");

            var setting = await db.SystemSettings.FirstOrDefaultAsync();
            var requireApproval = setting?.RequireProviderApproval ?? true;

            var provider = new Provider
            {
                UserId = userId,
                BusinessName = dto.BusinessName,
                Category = dto.Category,
                Description = dto.Description,
                Address = dto.Address,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Status = requireApproval ? ProviderStatus.Pending : ProviderStatus.Approved
            };

            db.Providers.Add(provider);
            await db.SaveChangesAsync();

            return await MapToDto(provider.Id);
        }

        public async Task<ResponseProviderDto> GetMyProfileAsync(Guid userId)
        {
            var provider = await db.Providers.FirstOrDefaultAsync(p => p.UserId == userId)
                ?? throw new NotFoundException("Provider profile not found.");

            return await MapToDto(provider.Id);
        }

        public async Task<ResponseProviderDto> GetByIdAsync(Guid providerId)
        {
            var exists = await db.Providers.AnyAsync(p => p.Id == providerId);
            if (!exists) throw new NotFoundException("Provider not found.");
            return await MapToDto(providerId);
        }


        public async Task<List<ResponseProviderDto>> GetAllAsync(string? category, int page, int pageSize)
        {
            var query = db.Providers.Where(p => p.Status == ProviderStatus.Approved && p.IsActive);

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(p => p.Category.ToLower() == category.ToLower());

            var ids = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => p.Id)
                .ToListAsync();

            var result = new List<ResponseProviderDto>();
            foreach (var id in ids) result.Add(await MapToDto(id));
            return result;
        }


        public async Task<List<ResponseProviderDto>> GetAllForAdminAsync(string? status, int page, int pageSize)
        {
            var query = db.Providers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ProviderStatus>(status, true, out var parsedStatus))
                query = query.Where(p => p.Status == parsedStatus);

            var ids = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => p.Id)
                .ToListAsync();

            var result = new List<ResponseProviderDto>();
            foreach (var id in ids) result.Add(await MapToDto(id));
            return result;
        }

        public async Task<ResponseProviderDto> UpdateAsync(Guid userId, UpdateProviderDto dto)
        {
            var provider = await db.Providers.FirstOrDefaultAsync(p => p.UserId == userId)
                ?? throw new NotFoundException("Provider profile not found.");

            provider.BusinessName = dto.BusinessName;
            provider.ImageUrl = dto.ImageUrl;
            provider.CoverImageUrl = dto.CoverImageUrl;
            provider.Category = dto.Category;
            provider.Description = dto.Description;
            provider.UpdatedAt = DateTime.UtcNow;
            provider.PhoneNumber = dto.PhoneNumber;
            provider.ContactEmail = dto.ContactEmail;
            provider.InstagramUrl = dto.InstagramUrl;
            provider.FacebookUrl = dto.FacebookUrl;

            await db.SaveChangesAsync();
            return await MapToDto(provider.Id);
        }

        public async Task ApproveAsync(Guid providerId)
        {
            var provider = await db.Providers.FindAsync(providerId)
                ?? throw new NotFoundException("Provider not found.");

            provider.Status = ProviderStatus.Approved;
            provider.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        public async Task RejectAsync(Guid providerId)
        {
            var provider = await db.Providers.FindAsync(providerId)
                ?? throw new NotFoundException("Provider not found.");

            provider.Status = ProviderStatus.Rejected;
            provider.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        private async Task<ResponseProviderDto> MapToDto(Guid providerId)
        {
            var provider = await db.Providers
                .Include(p => p.User)
                .Include(p => p.Reviews)
                .Include(p => p.Followers)
                .FirstAsync(p => p.Id == providerId);

            return new ResponseProviderDto
            {
                Id = provider.Id,
                ImageUrl = provider.ImageUrl,
                CoverImageUrl = provider.CoverImageUrl,
                BusinessName = provider.BusinessName,
                Category = provider.Category,
                Description = provider.Description,
                Status = provider.Status.ToString(),
                OwnerFullName = provider.User.FullName,
                AverageRating = provider.Reviews.Any() ? Math.Round(provider.Reviews.Average(r => r.Rating), 1) : 0,
                ReviewCount = provider.Reviews.Count,
                FollowersCount = provider.Followers.Count,
                CreatedAt = provider.CreatedAt,
                Address = provider.Address,
                PhoneNumber = provider.PhoneNumber,
                ContactEmail = provider.ContactEmail,
                InstagramUrl = provider.InstagramUrl,
                FacebookUrl = provider.FacebookUrl,
                Latitude = provider.Latitude,
                Longitude = provider.Longitude
            };
        }


        public async Task<ProviderDashboardDto> GetMyDashboardAsync(Guid userId)
        {
            var provider = await db.Providers.FirstOrDefaultAsync(p => p.UserId == userId)
                ?? throw new NotFoundException("Provider profile not found.");

            var profile = await MapToDto(provider.Id);
            var today = DateTime.UtcNow.Date;

            var totalServices = await db.Services.CountAsync(s => s.ProviderId == provider.Id && s.IsActive);

            var todayCount = await db.Appointments.CountAsync(a =>
                a.ProviderId == provider.Id && a.StartDateTime.Date == today && a.Status != AppointmentStatus.Cancelled);

            var weekCount = await db.Appointments.CountAsync(a =>
                a.ProviderId == provider.Id &&
                a.StartDateTime.Date >= today && a.StartDateTime.Date <= today.AddDays(7) &&
                a.Status != AppointmentStatus.Cancelled);

            var followersCount = await db.Follows.CountAsync(f => f.ProviderId == provider.Id);

            return new ProviderDashboardDto
            {
                Profile = profile,
                TotalServices = totalServices,
                TodayAppointmentsCount = todayCount,
                UpcomingWeekAppointmentsCount = weekCount,
                TotalFollowers = followersCount
            };
        }
    }
}