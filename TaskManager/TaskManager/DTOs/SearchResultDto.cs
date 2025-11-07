namespace TaskManager.DTOs
{
    public class SearchResultDto
    {
        public int PageId { get; set; }
        public string PageTitle { get; set; }
        public string? Icon { get; set; }
        public string? MatchType { get; set; }
        public string? MatchSnippet { get; set; }
    }
}
