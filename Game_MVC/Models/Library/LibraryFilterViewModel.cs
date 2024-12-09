namespace Game_MVC.Models.Library
{
    public class LibraryFilterViewModel
    {
        public GameStatus? Status { get; set; }
        public string? SortBy { get; set; }
        public bool Ascending { get; set; } = true;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
