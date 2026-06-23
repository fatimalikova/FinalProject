using AppointmentAPP.Data;
using AppointmentAPP.Dtos.PostDtos;
using AppointmentAPP.Enums;
using AppointmentAPP.Exceptions;
using AppointmentAPP.Interfaces;
using AppointmentAPP.Models;
using Microsoft.EntityFrameworkCore;

namespace AppointmentAPP.Services
{
    public class FollowService(AppDbContext db, INotificationService notificationService) : IFollowService
    {
        public async Task FollowAsync(Guid userId, Guid providerId)
        {
            var provider = await db.Providers.FirstOrDefaultAsync(p => p.Id == providerId)
                ?? throw new NotFoundException("Provider not found.");

            var alreadyFollowing = await db.Follows.AnyAsync(f => f.FollowerId == userId && f.ProviderId == providerId);
            if (alreadyFollowing) throw new BadRequestException("You are already following this provider.");

            db.Follows.Add(new Follow { FollowerId = userId, ProviderId = providerId });
            await db.SaveChangesAsync();

            var follower = await db.Users.FirstAsync(u => u.Id == userId);

            await notificationService.CreateAsync(
                provider.UserId, "New Follower",
                $"{follower.FullName} started following you.",
                NotificationType.System);
        }

        public async Task UnfollowAsync(Guid userId, Guid providerId)
        {
            var follow = await db.Follows.FirstOrDefaultAsync(f => f.FollowerId == userId && f.ProviderId == providerId)
                ?? throw new NotFoundException("You are not following this provider.");

            db.Follows.Remove(follow);
            await db.SaveChangesAsync();
        }

        public async Task<FollowStatusDto> GetStatusAsync(Guid providerId, Guid? currentUserId)
        {
            var followersCount = await db.Follows.CountAsync(f => f.ProviderId == providerId);

            var isFollowing = currentUserId.HasValue &&
                await db.Follows.AnyAsync(f => f.FollowerId == currentUserId.Value && f.ProviderId == providerId);

            return new FollowStatusDto { IsFollowing = isFollowing, FollowersCount = followersCount };
        }
    }

}