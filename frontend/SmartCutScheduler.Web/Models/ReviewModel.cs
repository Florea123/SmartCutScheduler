using System;

namespace SmartCutScheduler.Web.Models
{
    public class ReviewModel
    {
        public Guid Id { get; set; }
        public Guid BarberId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class UpsertReviewRequest
    {
        public Guid BarberId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}
