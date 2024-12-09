namespace Game_MVC.Models.Game
{
    public class GenreListResponse
    {
        public IEnumerable<GenreViewModel> Genres { get; set; } = Enumerable.Empty<GenreViewModel>();
        public int Count { get; set; }
    }
}
