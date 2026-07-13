using AppointmentAPP.Data;
using AppointmentAPP.Dtos.ReviewDtos;
using AppointmentAPP.Enums;
using AppointmentAPP.Exceptions;
using AppointmentAPP.Interfaces;
using AppointmentAPP.Models;
using Microsoft.EntityFrameworkCore;

namespace AppointmentAPP.Services
{
    public class ReviewService(AppDbContext db) : IReviewService
    {
        public async Task<ResponseReviewDto> CreateAsync(Guid clientId, CreateReviewDto dto)
        {
            var appointment = await db.Appointments
                .FirstOrDefaultAsync(a => a.Id == dto.AppointmentId)
                ?? throw new NotFoundException("Appointment not found.");

            if (appointment.ClientId != clientId)
                throw new ForbiddenException("You can only review your own appointments.");

            if (appointment.ProviderId != dto.ProviderId)
                throw new BadRequestException("This appointment does not belong to the specified provider.");

            if (appointment.Status != AppointmentStatus.Completed)
                throw new BadRequestException("You can only review completed appointments.");

            var alreadyReviewed = await db.Reviews.AnyAsync(r => r.AppointmentId == dto.AppointmentId);
            if (alreadyReviewed)
                throw new BadRequestException("You have already reviewed this appointment.");

            var review = new Review
            {
                ClientId = clientId,
                ProviderId = dto.ProviderId,
                AppointmentId = dto.AppointmentId,
                Rating = dto.Rating,
                Comment = dto.Comment
            };

            db.Reviews.Add(review);
            await db.SaveChangesAsync();

            var client = await db.Users.FirstAsync(u => u.Id == clientId);

            return new ResponseReviewDto
            {
                Id = review.Id,
                Rating = review.Rating,
                Comment = review.Comment,
                ClientFullName = client.FullName,
                ClientImageUrl = client.ImageUrl,
                CreatedAt = review.CreatedAt
            };
        }

        public async Task<List<ResponseReviewDto>> GetByProviderAsync(Guid providerId)
        {
            var reviews = await db.Reviews
                .Include(r => r.Client)
                .Where(r => r.ProviderId == providerId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return reviews.Select(r => new ResponseReviewDto
            {
                Id = r.Id,
                Rating = r.Rating,
                Comment = r.Comment,
                ClientFullName = r.Client.FullName,
                ClientImageUrl = r.Client.ImageUrl,
                CreatedAt = r.CreatedAt
            }).ToList();
        }

        public async Task DeleteAsync(Guid userId, Guid reviewId)
        {
            var review = await db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId)
                ?? throw new NotFoundException("Review not found.");

            if (review.ClientId != userId)
                throw new ForbiddenException("You can only delete your own review.");

            db.Reviews.Remove(review);
            await db.SaveChangesAsync();
        }
    }
}