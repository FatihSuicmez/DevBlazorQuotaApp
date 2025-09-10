// Data/ApplicationUser.cs

using Microsoft.AspNetCore.Identity;

namespace QuotaApp.Domain.Entities;

// ASP.NET Core Identity'nin standart kullanıcı sınıfını genişletiyoruz.
// Şimdilik içine ek bir özellik eklememize gerek yok.
public class ApplicationUser : IdentityUser
{
}