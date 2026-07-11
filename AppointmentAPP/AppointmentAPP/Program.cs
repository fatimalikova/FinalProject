using AppointmentAPP;
using AppointmentAPP.Data;
using AppointmentAPP.Extensions;
using AppointmentAPP.Hubs;
using Stripe;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddApplicationServices(builder.Configuration);
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];
//builder.Services.Configure<StripeSettings>(
//    builder.Configuration.GetSection("Stripe"));

var app = builder.Build();

app.UseGlobalException();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend"); // CORS policy for frontend
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await RoleSeeder.SeedAsync(services);
    await UserSeeder.SeedAsync(services);
}

app.Run();