using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SonghaiHMO.Data;
using SonghaiHMO.Models;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace SonghaiHMO.Web.Controllers
{
    public class ImportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ImportController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> ImportEnrollees(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "No file uploaded" });

            using var reader = new StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync();
            var lines = content.Split('\n');
            
            var imported = 0;
            var errors = 0;

            for (int i = 1; i < lines.Length; i++) // Skip header
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                
                var parts = lines[i].Split(',');
                if (parts.Length >= 5)
                {
                    try
                    {
                        var enrollee = new Enrollee
                        {
                            FirstName = parts[0].Trim(),
                            LastName = parts[1].Trim(),
                            Email = parts[2].Trim(),
                            Phone = parts[3].Trim(),
                            PlanType = parts[4].Trim(),
                            CreatedAt = DateTime.Now
                        };
                        _context.Enrollees.Add(enrollee);
                        imported++;
                    }
                    catch { errors++; }
                }
                else { errors++; }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, imported = imported, errors = errors });
        }

        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            var template = "FirstName,LastName,Email,Phone,PlanType\nJohn,Doe,john@example.com,08012345678,Basic";
            var bytes = System.Text.Encoding.UTF8.GetBytes(template);
            return File(bytes, "text/csv", "enrollee_template.csv");
        }
    }
}