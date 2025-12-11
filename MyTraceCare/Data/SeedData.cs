using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyTraceCare.Models;

namespace MyTraceCare.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

            // ---------------------------
            // 1. Ensure Roles
            // ---------------------------
            foreach (var role in new[] { "Patient", "Clinician", "Admin" })
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // ---------------------------
            // 2. Admin User
            // ---------------------------
            var adminEmail = "admin@tracecare.com";
            var admin = await userManager.FindByEmailAsync(adminEmail);

            if (admin == null)
            {
                admin = new User
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    Gender = Gender.Male,
                    Role = UserRole.Admin,
                    DOB = new DateTime(1990, 1, 1),
                    CreatedAt = DateTime.UtcNow,
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(admin, "Admin123!");
                await userManager.AddToRoleAsync(admin, "Admin");
            }

            // ---------------------------
            // 3. Clinicians
            // ---------------------------
            var cliniciansToSeed = new[]
            {
                new { Name = "Dr. Emily Carter", Email = "emily.carter@clinic.com", Gender = Gender.Female, Dob = new DateTime(1980,5,12) },
                new { Name = "Dr. Michael Brown", Email = "michael.brown@clinic.com", Gender = Gender.Male, Dob = new DateTime(1978,3,21) }
            };

            var clinicians = new List<User>();

            foreach (var c in cliniciansToSeed)
            {
                var existing = await userManager.FindByEmailAsync(c.Email);
                if (existing == null)
                {
                    var user = new User
                    {
                        UserName = c.Email,
                        Email = c.Email,
                        FullName = c.Name,
                        Gender = c.Gender,
                        DOB = c.Dob,
                        Role = UserRole.Clinician,
                        CreatedAt = DateTime.UtcNow,
                        EmailConfirmed = true
                    };

                    await userManager.CreateAsync(user, "Clinician123!");
                    await userManager.AddToRoleAsync(user, "Clinician");
                    clinicians.Add(user);
                }
                else clinicians.Add(existing);
            }

            var c1 = clinicians[0];
            var c2 = clinicians[1];

            // ---------------------------
            // 4. Patients
            // ---------------------------
            var patientsToSeed = new[]
            {
                new { Device = "d13043b3", Name = "Arman Malik", Email = "arman34@gmail.com", Gender = Gender.Male, Dob = new DateTime(1995,1,6) },
                new { Device = "de0e9b2c", Name = "Sophia Mathews", Email = "sophia.mathews@example.com", Gender = Gender.Female, Dob = new DateTime(1987,2,28) },
                new { Device = "1c0fd777", Name = "Liam Fernandes", Email = "liam.fernandes@example.com", Gender = Gender.Male, Dob = new DateTime(1995,9,3) },
                new { Device = "71e66ab3", Name = "Amelia Thomas", Email = "amelia.thomas@example.com", Gender = Gender.Female, Dob = new DateTime(1992,12,11) },
                new { Device = "543d4676", Name = "Noah George", Email = "noah.george@example.com", Gender = Gender.Male, Dob = new DateTime(1990,7,16) }
            };

            var patientList = new List<(User user, string device)>();

            foreach (var p in patientsToSeed)
            {
                var existing = await userManager.FindByEmailAsync(p.Email);
                User patient;

                if (existing == null)
                {
                    patient = new User
                    {
                        UserName = p.Email,
                        Email = p.Email,
                        FullName = p.Name,
                        Gender = p.Gender,
                        DOB = p.Dob,
                        Role = UserRole.Patient,
                        CreatedAt = DateTime.UtcNow,
                        EmailConfirmed = true
                    };

                    await userManager.CreateAsync(patient, "Patient123!");
                    await userManager.AddToRoleAsync(patient, "Patient");
                }
                else patient = existing;

                patientList.Add((patient, p.Device));
            }

            // ---------------------------
            // 5. Clinician → Patient Assignment
            // ---------------------------
            if (!context.ClinicianPatients.Any())
            {
                context.ClinicianPatients.AddRange(
                    new ClinicianPatient { ClinicianId = c1.Id, PatientId = patientList[0].user.Id },
                    new ClinicianPatient { ClinicianId = c1.Id, PatientId = patientList[1].user.Id },
                    new ClinicianPatient { ClinicianId = c1.Id, PatientId = patientList[2].user.Id },
                    new ClinicianPatient { ClinicianId = c2.Id, PatientId = patientList[3].user.Id },
                    new ClinicianPatient { ClinicianId = c2.Id, PatientId = patientList[4].user.Id }
                );
            }

            // ---------------------------
            // 6. Link CSV Files
            // ---------------------------
            string dataRoot = Path.Combine(env.WebRootPath, "patient-data");

            if (Directory.Exists(dataRoot))
            {
                foreach (var entry in patientList)
                {
                    var files = Directory.GetFiles(dataRoot, $"{entry.device}_*.csv");

                    foreach (var fp in files)
                    {
                        var name = Path.GetFileName(fp);
                        var dateStr = Path.GetFileNameWithoutExtension(name).Split('_').Last();

                        if (DateTime.TryParseExact(dateStr, "yyyyMMdd", CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out var date))
                        {
                            string dbPath = "/patient-data/" + name;

                            if (!await context.PatientDataFiles.AnyAsync(f => f.FilePath == dbPath))
                            {
                                context.PatientDataFiles.Add(new PatientDataFile
                                {
                                    UserId = entry.user.Id,
                                    DeviceId = entry.device,
                                    Date = date,
                                    FilePath = dbPath
                                });
                            }
                        }
                    }
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
