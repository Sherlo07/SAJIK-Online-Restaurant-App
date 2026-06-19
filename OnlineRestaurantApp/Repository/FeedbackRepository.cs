using Microsoft.EntityFrameworkCore;
using OnlineRestaurantApp.Dal;
using OnlineRestaurantApp.IRepository;
using OnlineRestaurantApp.Components;

namespace OnlineRestaurantApp.Repository
{
    public class FeedbackRepository: IFeedbackRepository
    {
        private readonly OnlineRestaurantDbContext _context;

        public FeedbackRepository(OnlineRestaurantDbContext context)
        {
            _context = context;
        }
        private IQueryable<Feedback> Query => _context.feedbacks.AsNoTracking();
        public async Task DeleteAsync(int FeedbackId)
        {
            var f = await _context.feedbacks.FindAsync(FeedbackId);
            if (f != null)
            {
                _context.feedbacks.Remove(f);
            }
        }

        public async Task<IEnumerable<Feedback>> GetAllAsync()
        {
            return await _context.feedbacks.ToListAsync();

        }

        public async Task<Feedback?> GetByIdAsync(int FeedbackId)
        {
            var feedback = await _context.feedbacks
                .FirstOrDefaultAsync(f => f.FeedbackId == FeedbackId);
            return feedback;
        }

        public async Task InsertAsync(Feedback feedback)
        {
            await _context.feedbacks.AddAsync(feedback);

        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Feedback feedback)
        {
            _context.feedbacks.Update(feedback);
        }



        public async Task<(IEnumerable<Feedback> Items, int TotalCount)> GetPagedAsync(
                    string? createdRange,
                    string? email,
                    int pageIndex,
                    int pageSize)
        {
            if (pageIndex <= 0) pageIndex = 1;
            if (pageSize <= 0) pageSize = 10;

            var q = Query;

            // Email contains (case-sensitive DB collation applies; change if needed)
            if (!string.IsNullOrWhiteSpace(email))
            {
                var term = email.Trim();
                q = q.Where(f => (f.Email ?? "").Contains(term));
            }

            // Created range
            if (!string.IsNullOrWhiteSpace(createdRange))
            {
                var now = DateTime.UtcNow;
                DateTime start;
                switch (createdRange)
                {
                    case "Today":
                        start = now.Date;
                        q = q.Where(f => f.CreatedOn >= start);
                        break;
                    case "Last7":
                        start = now.Date.AddDays(-6); // includes today
                        q = q.Where(f => f.CreatedOn >= start);
                        break;
                    case "Last30":
                        start = now.Date.AddDays(-29);
                        q = q.Where(f => f.CreatedOn >= start);
                        break;
                    case "ThisMonth":
                        start = new DateTime(now.Year, now.Month, 1);
                        q = q.Where(f => f.CreatedOn >= start);
                        break;
                }
            }

            var total = await q.CountAsync();

            // Always latest first
            q = q.OrderByDescending(f => f.CreatedOn);

            var items = await q
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

    }
}
