using AppointmentAPP.Data;
using AppointmentAPP.Helpers;
using AppointmentAPP.Models;
using AppointmentAPP.Profiles;
using AppointmentAPP.Services;
using AppointmentAPP.Services.Interfaces;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace AppointmentAPP
{
    public static class ServicesRegistration
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers();
            services.AddValidatorsFromAssemblyContaining<Program>();

            //using custom response model for exception handling and validation errors

            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToArray();

                    return new BadRequestObjectResult(
                        ResponseModelHelper.BadRequestResult<object>(errors)
                    );
                };
            });

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter only the JWT token. 'Bearer' will be added automatically."
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        new string[] {}
                    }
                });
            });


            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));


            services.AddIdentity<AppUser, IdentityRole<Guid>>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();


            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["JWT:Issuer"],
                    ValidAudience = configuration["JWT:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Key"]))
                };
            });

            services.AddAutoMapper(typeof(MapperProfile).Assembly);

            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IFileUploadService, FileUploadService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IProviderService, ProviderService>();
            services.AddScoped<IServiceManagementService, ServiceManagementService>();
            services.AddScoped<IWorkingHourService, WorkingHourService>();
            services.AddScoped<IAvailabilityService, AvailabilityService>();
            services.AddScoped<IAppointmentService, AppointmentService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<ISystemSettingService, SystemSettingService>();
            services.AddHostedService<ReminderBackgroundService>();
            services.AddFluentValidationAutoValidation();
            services.AddHostedService<ReminderBackgroundService>();

            return services;
        }
    }
}
