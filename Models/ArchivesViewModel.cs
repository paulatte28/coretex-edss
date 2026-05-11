using System.Collections.Generic;

namespace coretex_finalproj.Models
{
    public class ArchivesViewModel
    {
        public List<Branch> ArchivedBranches { get; set; } = new List<Branch>();
        public List<AppUser> ArchivedStaff { get; set; } = new List<AppUser>();
        public List<BackupFile> SystemBackups { get; set; } = new List<BackupFile>();
    }

    public class BackupFile
    {
        public string FileName { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public DateTime CreatedAt { get; set; }
        public string SizeDisplay => $"{(SizeBytes / 1024.0 / 1024.0):N1} MB";
    }
}
