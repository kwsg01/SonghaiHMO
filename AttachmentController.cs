using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SonghaiHMO.Data;
using SonghaiHMO.Models;
using SonghaiHMO.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SonghaiHMO.Web.Controllers
{
    public class AttachmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ActivityLogger _logger;

        public AttachmentController(ApplicationDbContext context, ActivityLogger logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file, int claimId)
        {
            var adminId = HttpContext.Session.GetString("AdminId");
            if (string.IsNullOrEmpty(adminId))
                return Json(new { success = false, message = "Not authenticated" });

            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "No file selected" });

            try
            {
                // Create uploads directory if not exists
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Generate unique filename
                var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Save to database
                var attachment = new ClaimAttachment
                {
                    ClaimId = claimId,
                    FileName = file.FileName,
                    FilePath = $"/uploads/{uniqueFileName}",
                    FileType = file.ContentType,
                    FileSize = file.Length,
                    UploadedBy = HttpContext.Session.GetString("AdminName") ?? "Admin",
                    UploadedAt = DateTime.Now
                };

                _context.ClaimAttachments.Add(attachment);
                await _context.SaveChangesAsync();

                await _logger.LogAsync(
                    adminId,
                    HttpContext.Session.GetString("AdminName") ?? "Admin",
                    "Admin",
                    "UPLOAD_ATTACHMENT",
                    "Claim",
                    claimId.ToString(),
                    $"Uploaded file: {file.FileName} ({file.Length} bytes)"
                );

                return Json(new { success = true, message = "File uploaded successfully", file = attachment });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAttachments(int claimId)
        {
            var attachments = await _context.ClaimAttachments
                .Where(a => a.ClaimId == claimId)
                .ToListAsync();
            return Json(attachments);
        }

        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var attachment = await _context.ClaimAttachments.FindAsync(id);
            if (attachment == null)
                return NotFound();

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", attachment.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, attachment.FileType, attachment.FileName);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var attachment = await _context.ClaimAttachments.FindAsync(id);
            if (attachment == null)
                return Json(new { success = false });

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", attachment.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            _context.ClaimAttachments.Remove(attachment);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}