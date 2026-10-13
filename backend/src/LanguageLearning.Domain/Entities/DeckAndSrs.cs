using LanguageLearning.Domain.Common;
using LanguageLearning.Domain.Enums;

namespace LanguageLearning.Domain.Entities;

public class Deck : BaseEntity
{
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;
    public Guid LanguageId { get; set; }
    public Language Language { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPublic { get; set; } = false;
    public Guid? SourceDeckId { get; set; } // Nếu clone từ deck khác
    public int Version { get; set; } = 1;

    public ICollection<DeckTag> Tags { get; set; } = new List<DeckTag>();
    public ICollection<Card> Cards { get; set; } = new List<Card>();
}

public class DeckTag : BaseEntity
{
    public Guid DeckId { get; set; }
    public Deck Deck { get; set; } = null!;
    public string Tag { get; set; } = string.Empty;
}

public class Card : BaseEntity
{
    public Guid DeckId { get; set; }
    public Deck Deck { get; set; } = null!;
    public string Front { get; set; } = string.Empty;
    public string Back { get; set; } = string.Empty;
    public string? AudioUrl { get; set; }
    public string? ImageUrl { get; set; }
    public int Version { get; set; } = 1;

    public ICollection<CardSchedule> Schedules { get; set; } = new List<CardSchedule>();
    public ICollection<ReviewEvent> ReviewEvents { get; set; } = new List<ReviewEvent>();
}

public class CardSchedule : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid CardId { get; set; }
    public Card Card { get; set; } = null!;
    public DateTime DueAt { get; set; }
    public string StateJson { get; set; } = "{}"; // SRS interval, ease factor, repetitions
    public string AlgorithmVersion { get; set; } = "srs-v1";
    public DateTime? LastReviewedAt { get; set; }
}

public class ReviewEvent : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid CardId { get; set; }
    public Card Card { get; set; } = null!;
    public SrsRating Rating { get; set; }
    public DateTime ReviewedAt { get; set; }
    public Guid ClientEventId { get; set; } // Idempotency key from client
    public string AlgorithmVersion { get; set; } = "srs-v1";
}
