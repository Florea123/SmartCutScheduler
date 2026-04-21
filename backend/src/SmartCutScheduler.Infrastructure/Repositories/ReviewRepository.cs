using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Repositories;
using SmartCutScheduler.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace SmartCutScheduler.Infrastructure.Repositories
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly AppDbContext _context;

        public ReviewRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Review>> GetReviewsForBarberAsync(Guid barberId)
        {
            return await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.BarberId == barberId)
                .ToListAsync();
        }

        public async Task<Review?> GetReviewByUserAndBarberAsync(Guid userId, Guid barberId)
        {
            return await _context.Reviews.FirstOrDefaultAsync(r => r.UserId == userId && r.BarberId == barberId);
        }

        public async Task AddOrUpdateReviewAsync(Review review)
        {
            var existing = await GetReviewByUserAndBarberAsync(review.UserId, review.BarberId);
            if (existing != null)
            {
                existing.Rating = review.Rating;
                existing.Comment = review.Comment;
                existing.UpdatedAt = DateTime.UtcNow;
                _context.Reviews.Update(existing);
            }
            else
            {
                review.CreatedAt = DateTime.UtcNow;
                _context.Reviews.Add(review);
            }
            await _context.SaveChangesAsync();
        }
    }
}
