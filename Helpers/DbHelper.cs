using Rolayther.Data;
using Rolayther.Exceptions;
using Rolayther.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog.Configuration;
using System.Linq.Expressions;

namespace Rolayther.Helpers
{
    public class DbHelper
    {
        public static async Task InitializeDatabaseAsync<T>(WebApplication app) where T : DbContext
        {
            try
            {
                IServiceProvider services = app.Services;

                await RunMigrationAsync<T>(services);
                await SeedRoles(services);
                await SeedAdmin(services, app.Configuration);
            }
            catch
            {
                throw;
            }
        }
        //metodo per eseguire le migrazioni
        private static async Task RunMigrationAsync<T>(IServiceProvider services) where T : DbContext
        {
            try
            {
                using var scope = services.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<T>();
                await dbContext.Database.MigrateAsync();
            }
            catch
            {
                throw;
            }
        }

        //metodo seeding dei ruoli
        private static async Task SeedRoles(IServiceProvider services)
        {
            try
            {
                using var scope = services.CreateAsyncScope();

                RoleManager<IdentityRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                bool AdminRoleExists = await roleManager.RoleExistsAsync(StringConstants.AdminRole);

                IdentityRole? AdminRole = null;
                IdentityRole? PlayerRole = null;
                IdentityRole? MasterRole = null;

                if (!AdminRoleExists)
                {
                    AdminRole = new IdentityRole()
                    {
                        Name = StringConstants.AdminRole,
                    };

                    IdentityResult AdminRoleCreated = await roleManager.CreateAsync(AdminRole);

                    if (!AdminRoleCreated.Succeeded)
                    {
                        throw new DbInizializationException("Errore durante la creazione del ruolo Admin");
                    }
                }

                // seeding ruolo player

                bool PlayerRoleExists = await roleManager.RoleExistsAsync(StringConstants.PlayerRole);

                if (!PlayerRoleExists)
                {
                    PlayerRole = new IdentityRole()
                    {
                        Name = StringConstants.PlayerRole,
                    };

                    IdentityResult PlayerRoleCreated = await roleManager.CreateAsync(PlayerRole);

                    if (!PlayerRoleCreated.Succeeded)
                    {
                        if (AdminRole != null)
                        {
                            await roleManager.DeleteAsync(AdminRole);
                        }
                        throw new DbInizializationException("Errore durante la creazione del ruolo Player");
                    }
                }

                // seeding ruolo master
                bool MasterRoleExists = await roleManager.RoleExistsAsync(StringConstants.MasterRole);

                if (!MasterRoleExists)
                {
                    MasterRole = new IdentityRole()
                    {
                        Name = StringConstants.MasterRole,
                    };

                    IdentityResult MasterRoleCreated = await roleManager.CreateAsync(MasterRole);
                    if (!MasterRoleCreated.Succeeded)
                    {
                        if (AdminRole != null)
                        {
                            await roleManager.DeleteAsync(AdminRole);
                        }
                        if (PlayerRole != null)
                        {
                            await roleManager.DeleteAsync(PlayerRole);
                        }
                        throw new DbInizializationException("Errore durante la creazione del ruolo Master");
                    }
                }
            }
            catch
            {
                throw;
            }
        }
        //metodo seeding admin

        private static async Task SeedAdmin(IServiceProvider services, IConfiguration configuration)
        {
            try
            {
                using var scope = services.CreateAsyncScope();

                UserManager<ApplicationUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                // Email e password dell'Admin NON sono più hardcoded: vanno messe nei User Secrets
                // in locale (chiavi Admin:Email e Admin:Password) e nelle variabili d'ambiente in produzione.
                string? adminEmail = configuration["Admin:Email"];
                string? adminPassword = configuration["Admin:Password"];

                if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
                {
                    throw new DbInizializationException(
                        "Credenziali Admin non configurate: aggiungi Admin:Email e Admin:Password ai User Secrets (o alle variabili d'ambiente in produzione).");
                }

                ApplicationUser? existingSuperAdmin = await userManager.FindByEmailAsync(adminEmail);

                if (existingSuperAdmin == null)
                {
                    ApplicationUser Admin = new ApplicationUser()
                    {
                        Name = "Admin",
                        Surname = "Rolayther",
                        Email = adminEmail,
                        UserName = adminEmail,
                        IsDeleted = false,
                    };
                    IdentityResult userCreated = await userManager.CreateAsync(Admin, adminPassword);

                    if (!userCreated.Succeeded)
                    {
                        string details = string.Join("; ", userCreated.Errors.Select(e => $"{e.Code}: {e.Description}"));
                        throw new DbInizializationException($"Errore durante la creazione dell'utente Admin - {details}");
                    }

                    IdentityResult roleAssigned = await userManager.AddToRoleAsync(Admin, StringConstants.AdminRole);

                    if (!roleAssigned.Succeeded)
                    {
                        await userManager.DeleteAsync(Admin);
                        throw new DbInizializationException("Errore durante l'assegnazione del ruolo all'utente");
                    }
                }
            }
            catch
            {
                throw;
            }
        }
    }
}
