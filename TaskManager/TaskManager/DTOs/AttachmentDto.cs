namespace TaskManager.DTOs
{
    public class AttachmentDto
    {
        public int Id { get; set; }
        public string OriginalFileName { get; set; }
        public string ContentType { get; set; }
        public long FileSize { get; set; }
        public string FilePath { get; set; }

    }
}
