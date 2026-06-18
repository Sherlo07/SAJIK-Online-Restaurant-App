using OnlineRestaurantApp.Components;

namespace OnlineRestaurantApp.IRepository
{
    public interface IFeedbackRepository
    {
        Task<IEnumerable<Feedback>> GetAllAsync();
        Task<Feedback?> GetByIdAsync(int FeedbackId);
        Task InsertAsync(Feedback feedback);
        Task UpdateAsync(Feedback feedback);
        Task DeleteAsync(int FeedbackId);
        Task SaveAsync();


        Task<(IEnumerable<Feedback> Items, int TotalCount)> GetPagedAsync(
                    string? createdRange,
                    string? email,
                    int pageIndex,
                    int pageSize);


    }
}
