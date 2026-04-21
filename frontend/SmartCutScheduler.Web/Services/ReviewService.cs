using System.Net.Http.Json;
using SmartCutScheduler.Web.Models;

namespace SmartCutScheduler.Web.Services
{
    public class ReviewService
    {
        private readonly HttpClient _httpClient;

        public ReviewService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<ReviewModel>> GetReviewsForBarberAsync(Guid barberId)
        {
            var result = await _httpClient.GetFromJsonAsync<List<ReviewModel>>($"api/reviews/barber/{barberId}");
            return result ?? new List<ReviewModel>();
        }

        public async Task<bool> AddOrUpdateReviewAsync(Guid barberId, int rating, string? comment)
        {
            var response = await _httpClient.PostAsJsonAsync("api/reviews", new UpsertReviewRequest
            {
                BarberId = barberId,
                Rating = rating,
                Comment = comment
            });
            return response.IsSuccessStatusCode;
        }
    }
}
