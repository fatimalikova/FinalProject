using AppointmentAPP.Dtos.AppointmentDtos;
using AppointmentAPP.Dtos.NotificationDtos;
using AppointmentAPP.Dtos.ProviderDtos;
using AppointmentAPP.Dtos.ReviewDtos;
using AppointmentAPP.Dtos.ServiceDtos;
using AppointmentAPP.Dtos.SystemSetting;
using AppointmentAPP.Dtos.UnavailableDayDtos;
using AppointmentAPP.Dtos.UserDtos;
using AppointmentAPP.Dtos.WorkingHourDtos;
using AppointmentAPP.Models;
using AutoMapper;

namespace AppointmentAPP.Profiles
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            // ===== Provider =====
            CreateMap<Provider, ResponseProviderDto>()
                .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.OwnerFullName, opt => opt.MapFrom(s => s.User.FullName))
                .ForMember(d => d.AverageRating, opt => opt.MapFrom(s =>
                    s.Reviews.Any() ? Math.Round(s.Reviews.Average(r => r.Rating), 1) : 0))
                .ForMember(d => d.ReviewCount, opt => opt.MapFrom(s => s.Reviews.Count));

            CreateMap<CreateProviderDto, Provider>();
            CreateMap<UpdateProviderDto, Provider>();

            // ===== Service =====
            CreateMap<Service, ResponseServiceDto>()
                .ForMember(d => d.ProviderBusinessName, opt => opt.MapFrom(s => s.Provider.BusinessName));

            CreateMap<CreateServiceDto, Service>();
            CreateMap<UpdateServiceDto, Service>();

            // ===== WorkingHour =====
            CreateMap<WorkingHour, ResponseWorkingHourDto>();
            CreateMap<CreateWorkingHourDto, WorkingHour>();

            // ===== UnavailableDay =====
            CreateMap<UnavailableDay, ResponseUnavailableDayDto>();
            CreateMap<CreateUnavailableDayDto, UnavailableDay>();

            // ===== Appointment =====
            CreateMap<Appointment, AppointmentResponseDto>()
                .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.ClientFullName, opt => opt.MapFrom(s => s.Client.FullName))
                .ForMember(d => d.ProviderBusinessName, opt => opt.MapFrom(s => s.Provider.BusinessName))
                .ForMember(d => d.ServiceName, opt => opt.MapFrom(s => s.Service.Name));

            CreateMap<BookAppointmentDto, Appointment>();

            // ===== Review =====
            CreateMap<Review, ResponseReviewDto>()
                .ForMember(d => d.ClientFullName, opt => opt.MapFrom(s => s.Client.FullName));

            CreateMap<CreateReviewDto, Review>();

            // ===== Notification =====
            CreateMap<Notification, ResponseNotificationDto>()
                .ForMember(d => d.Type, opt => opt.MapFrom(s => s.Type.ToString()));

            // ===== SystemSetting =====
            CreateMap<SystemSetting, ResponseSystemSettingDto>();
            CreateMap<UpdateSystemSettingDto, SystemSetting>();

        }
    }
}
