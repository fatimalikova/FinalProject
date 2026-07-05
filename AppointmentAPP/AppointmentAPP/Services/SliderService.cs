using AppointmentAPP.Data;
using AppointmentAPP.Dtos.SliderDtos;
using AppointmentAPP.Exceptions;
using AppointmentAPP.Interfaces;
using AppointmentAPP.Models;
using Microsoft.EntityFrameworkCore;

namespace AppointmentAPP.Services
{
    public class SliderService(AppDbContext db) : ISliderService
    {
        public async Task<List<ResponseSliderDto>> GetActiveAsync()
        {
            var sliders = await db.Sliders
                .Where(s => s.IsActive)
                .OrderBy(s => s.Order)
                .ToListAsync();
            return sliders.Select(MapToDto).ToList();
        }

        public async Task<List<ResponseSliderDto>> GetAllAsync()
        {
            var sliders = await db.Sliders
                .OrderBy(s => s.Order)
                .ToListAsync();
            return sliders.Select(MapToDto).ToList();
        }

        public async Task<ResponseSliderDto> CreateAsync(CreateSliderDto dto)
        {
            var slider = new Slider
            {
                ImageUrl = dto.ImageUrl,
                Title = dto.Title,
                Description = dto.Description,
                ButtonText = dto.ButtonText,
                ButtonLink = dto.ButtonLink,
                Order = dto.Order,
            };
            db.Sliders.Add(slider);
            await db.SaveChangesAsync();
            return MapToDto(slider);
        }

        public async Task<ResponseSliderDto> UpdateAsync(Guid id, UpdateSliderDto dto)
        {
            var slider = await db.Sliders.FindAsync(id)
                ?? throw new NotFoundException("Slider not found.");

            slider.ImageUrl = dto.ImageUrl;
            slider.Title = dto.Title;
            slider.Description = dto.Description;
            slider.ButtonText = dto.ButtonText;
            slider.ButtonLink = dto.ButtonLink;
            slider.Order = dto.Order;
            slider.IsActive = dto.IsActive;

            await db.SaveChangesAsync();
            return MapToDto(slider);
        }

        public async Task DeleteAsync(Guid id)
        {
            var slider = await db.Sliders.FindAsync(id)
                ?? throw new NotFoundException("Slider not found.");
            db.Sliders.Remove(slider);
            await db.SaveChangesAsync();
        }

        private static ResponseSliderDto MapToDto(Slider s) => new()
        {
            Id = s.Id,
            ImageUrl = s.ImageUrl,
            Title = s.Title,
            Description = s.Description,
            ButtonText = s.ButtonText,
            ButtonLink = s.ButtonLink,
            Order = s.Order,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt,
        };
    }
}
