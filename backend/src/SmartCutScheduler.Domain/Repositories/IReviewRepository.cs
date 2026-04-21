using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartCutScheduler.Domain.Entities;

namespace SmartCutScheduler.Domain.Repositories
{
    public interface IReviewRepository
    {
        Task<IEnumerable<Review>> GetReviewsForBarberAsync(Guid barberId);
        Task<Review?> GetReviewByUserAndBarberAsync(Guid userId, Guid barberId);
        Task AddOrUpdateReviewAsync(Review review);
    }
}
