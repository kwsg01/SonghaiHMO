using System;

namespace SonghaiHMO.Models
{
    public class ClaimAttachment
    {
        public int Id { get; set; }
        public int ClaimId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string UploadedBy { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.Now;
        
        public virtual Claim? Claim { get; set; }
    }
}