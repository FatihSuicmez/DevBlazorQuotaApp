using Microsoft.AspNetCore.Identity;
using QuotaApp.Domain.Entities; // DÜZELTME: ApplicationUser'ın yeni adresi

namespace QuotaApp.Presentation.Components.Account; // DÜZELTME: Dosyanın yeni adresi

internal sealed class IdentityUserAccessor(UserManager<ApplicationUser> userManager, IdentityRedirectManager redirectManager)
{
    public async Task<ApplicationUser> GetRequiredUserAsync(HttpContext context)
    {
        var user = await userManager.GetUserAsync(context.User);

        if (user is null)
        {
            redirectManager.RedirectToWithStatus("Account/InvalidUser", $"Error: Unable to load user with ID '{userManager.GetUserId(context.User)}'.", context);
        }

        return user;
    }
}