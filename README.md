# 📅 Appointment System

> A full-featured appointment booking platform built as a final project for **Code Academy**.  
> Connects service providers (dentists, beauty salons, lawyers, tutors, etc.) with clients through a modern web interface.

---

## 🌐 Live Demo

> Backend: `http://localhost:5291`  
> Frontend: `http://127.0.0.1:5500` (Live Server)

---

## ✨ Features

### 👤 Client
- Register & login with **Email 2FA**
- Search & filter providers by category and rating
- **4-step booking wizard**: Provider → Service → Date/Time → Payment
- **Stripe payment** for paid services (free services skip the payment step)
- View upcoming & past appointments
- Write reviews (only for completed appointments)
- Like, comment & follow providers
- Edit profile & upload avatar (ImgBB)

### 🏢 Provider
- Create and manage business profile
- Add services (paid or free — price = 0)
- Set weekly working hours & unavailable days
- View appointments via calendar
- Share posts with images
- Real-time SignalR notifications
- Dashboard with weekly stats

### 🛡 Admin
- Approve / block providers
- Deactivate / reactivate services
- View all appointments on the platform
- Manage homepage sliders
- Configure system rules (cancellation notice, advance booking days, reminder hours)

---

## 🚀 Tech Stack

### Backend
| Technology | Purpose |
|---|---|
| ASP.NET Core Web API | REST API |
| Entity Framework Core | ORM (Code First) |
| SQL Server | Database |
| ASP.NET Identity | Authentication & user management |
| JWT + Refresh Token | Stateless auth |
| Email 2FA (Gmail SMTP) | Two-factor authentication |
| SignalR | Real-time notifications |
| Stripe.net | Payment processing |
| FluentValidation | Request validation |
| AutoMapper | DTO mapping |

### Frontend
| Technology | Purpose |
|---|---|
| JavaScript  | Logic & API calls (Fetch API) |
| HTML + CSS | Markup & styling (no framework) |
| Stripe.js  | Client-side payment |
| Font Awesome 6.5 | Icons |
| ImgBB API | Image hosting |
| Bootstrap 5 | Homepage slider only |

---

## 🗂 Project Structure

```
appointment-system/
│
├── AppointmentAPP/                  # ASP.NET Core Backend
│   ├── Controllers/
│   │   ├── AccountController.cs
│   │   ├── AppointmentController.cs
│   │   ├── AvailabilityController.cs
│   │   ├── PaymentController.cs
│   │   ├── ProviderController.cs
│   │   ├── ReviewController.cs
│   │   ├── PostController.cs
│   │   ├── NotificationController.cs
│   │   └── ...
│   ├── Models/
│   ├── Services/
│   ├── Dtos/
│   ├── Data/
│   └── appsettings.json
│
└── frontend/
    ├── client/                      # Client panel
    │   ├── auth/
    │   │   ├── login.html
    │   │   └── register.html
    │   ├── pages/
    │   │   ├── home.html
    │   │   ├── providers.html
    │   │   ├── provider-detail.html
    │   │   ├── booking.html
    │   │   ├── appointments.html
    │   │   ├── appointment-detail.html
    │   │   ├── dashboard.html
    │   │   └── notifications.html
    │   ├── css/client.css
    │   └── js/
    │       ├── client-api.js
    │       └── layout.js
    │
    ├── provider/                    # Provider panel
    │   └── provider/
    │       ├── dashboard.html
    │       ├── profile.html
    │       ├── services.html
    │       ├── working-hours.html
    │       ├── unavailable-days.html
    │       ├── appointments.html
    │       ├── calendar.html
    │       ├── posts.html
    │       └── notifications.html
    │
    └── admin/                       # Admin panel
        └── admin/
            ├── dashboard.html
            ├── providers.html
            ├── services.html
            ├── appointments.html
            ├── settings.html
            └── sliders.html
```

---

## 🗄 Database Schema

```
AspNetUsers          → Id, FullName, Email, UserName, ImageUrl, CreatedAt
Providers            → BusinessName, Category, Address, PhoneNumber, ContactEmail,
                       InstagramUrl, FacebookUrl, Status, IsActive
Services             → Name, Description, Price (≥0), DurationMinutes,
                       IsActive, DeactivatedByAdmin
Appointments         → ClientId, ProviderId, ServiceId, StartDateTime,
                       EndDateTime, Status, Notes, PriceAtBooking
WorkingHours         → ProviderId, Day (DayOfWeek), StartTime, EndTime
UnavailableDays      → ProviderId, Date
Reviews              → ProviderId, ClientId, AppointmentId, Rating (1–5), Comment
Posts                → ProviderId, ImageUrl, Caption, LikesCount, CommentsCount
PostComments         → PostId, UserId, Content
PostLikes            → PostId, UserId
Follows              → FollowerId, ProviderId
Notifications        → UserId, Title, Message, Type, IsRead
Sliders              → ImageUrl, Title, Subtitle, Link
SystemSettings       → MinCancellationNoticeHours, MaxAdvanceBookingDays,
                       ReminderHoursBeforeAppointment, RequireProviderApproval
```

---

## ⚙️ Setup & Installation

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/en-us/sql-server)
- [Node.js](https://nodejs.org/) (for Live Server)
- [Stripe Account](https://stripe.com) (test mode)
- [ImgBB API Key](https://imgbb.com)
- Gmail account with App Password

---

### 1. Clone the repository

```bash
git clone https://github.com/fatimalikova/FinalProject.git
cd appointment-system
```


### 2. Apply database migrations

```bash
cd AppointmentAPP
dotnet ef database update
```

### 3. Run the backend

```bash
dotnet run
# API running at http://localhost:5291
# Swagger: http://localhost:5291/swagger
```

### 4. Run the frontend

Open the `frontend/` folder with **VS Code Live Server** or any static file server:
```
http://127.0.0.1:5500/client/pages/home.html
```

---

## 📋 API Endpoints (Summary)

### Auth
```
POST /api/account/register
POST /api/account/login
POST /api/account/refresh
POST /api/account/verify-2fa
PUT  /api/account/profile
```

### Providers
```
GET  /api/providers
GET  /api/providers/{id}
POST /api/providers          (create profile)
PUT  /api/providers          (update profile)
```

### Services
```
GET  /api/services/provider/{providerId}
GET  /api/services/catalog
POST /api/services
PUT  /api/services/{id}
DEL  /api/services/{id}
```

### Appointments
```
GET  /api/appointments/my
GET  /api/appointments/{id}
POST /api/appointments/book
PUT  /api/appointments/{id}/cancel
PUT  /api/appointments/{id}/reschedule
PUT  /api/appointments/{id}/complete
```

### Availability
```
GET  /api/availability?providerId=&serviceId=&date=
```

### Payment
```
POST /api/payment/create-intent
POST /api/payment/setup-intent
GET  /api/payment/method/{pmId}
```

### Social
```
POST /api/posts
GET  /api/posts/provider/{providerId}
POST /api/posts/{id}/like
DEL  /api/posts/{id}/like
POST /api/posts/{id}/comments
GET  /api/posts/{id}/comments
POST /api/follows/{providerId}
DEL  /api/follows/{providerId}
POST /api/reviews
GET  /api/reviews/provider/{providerId}
```

---

## 🧩 Smart Booking Logic

```
Service price = 0  →  3-step wizard (no payment step)
Service price > 0  →  4-step wizard (Stripe payment)

Card saved in profile  →  Auto-filled on payment (only CVV needed)
No card saved         →  Redirect to dashboard to add card
```

---

## 🔔 Real-time Events (SignalR)

| Event | Recipients |
|---|---|
| Booking confirmed | Client + Provider |
| Appointment cancelled | Other party |
| Appointment rescheduled | Other party |
| Reminder (N hours before) | Client + Provider |
| New post from followed provider | Followers |

---

## ✅ Mandatory Features Checklist

- [x] F1 — Provider registration + services + working hours
- [x] F2 — Real-time slot availability (based on service duration)
- [x] F3 — Client booking (4-step wizard)
- [x] F4 — Confirm / Reschedule / Cancel (with notice period)
- [x] F5 — Provider calendar view
- [x] F6 — Automated reminders (SignalR + Email)
- [x] F7 — Client appointment history (upcoming / past)
- [x] F8 — Admin management panel

---

## 🌟 Bonus Features

- [x] Stripe payment integration (test mode)
- [x] Social platform (Post / Like / Comment / Follow)
- [x] Email 2FA
- [x] ImgBB image upload
- [x] Admin-controlled homepage slider
- [x] Real-time SignalR notifications
- [x] Review system (appointment-based)
- [x] Google Maps link integration
- [x] Similar providers recommendation
- [x] Free service flow (payment step skipped)
- [x] Provider social links (Instagram, Facebook)

---

## 🎨 Color Palette

| Color | Hex | Usage |
|---|---|---|
| Dark Teal | `#082627` | Primary background |
| Mid Teal | `#0f2b2b` | Cards, panels |
| Gold | `#C9912F` | Accent, buttons |
| Light Gold | `#DBA84A` | Hover states |
| Success Green | `#2D6A4F` | Success states |

---

## 📄 License

This project is for educational purposes — **Code Academy Final Project**.

---

## 👨‍💻 Author

Fatima Malikova

> *"This project taught me how to solve real-world problems — authentication, payment systems, real-time communication, and complex business logic."*
