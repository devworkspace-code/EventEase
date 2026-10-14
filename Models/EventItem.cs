namespace EventEase.Models;

public sealed record EventItem(
    int Id,
    string Name,
    DateTime Date,
    string Location,
    string Category,
    string Description,
    string Theme,
    string Icon);

public static class EventCatalog
{
    public static IReadOnlyList<EventItem> Events { get; } =
    [
        new EventItem(
            1,
            "Future of Work Summit",
            new DateTime(2026, 11, 12, 9, 0, 0),
            "The Glasshouse, New York",
            "Conference",
            "A full day of fresh perspectives on the people, ideas, and technology shaping how we work.",
            "summit",
            "✳"),
        new EventItem(
            2,
            "The Founders' Table",
            new DateTime(2026, 11, 21, 18, 30, 0),
            "The Hoxton, Chicago",
            "Networking",
            "An evening of good food and candid conversation for the people building what comes next.",
            "dinner",
            "◒"),
        new EventItem(
            3,
            "Creative Minds Workshop",
            new DateTime(2026, 12, 5, 10, 0, 0),
            "The Studio, Austin",
            "Workshop",
            "Get hands-on with practical creative exercises and leave with ideas ready to bring to life.",
            "workshop",
            "✳"),
        new EventItem(
            4,
            "Winter Lights Gala",
            new DateTime(2026, 12, 18, 19, 0, 0),
            "The Meridian, San Francisco",
            "Social",
            "Celebrate the year with an elegant night of live music, seasonal dining, and great company.",
            "gala",
            "✧")
    ];

    public static EventItem? Find(int id) => Events.FirstOrDefault(eventItem => eventItem.Id == id);
}

public sealed record EventRegistrationRecord(
    Guid Id,
    int EventId,
    string Name,
    string Email,
    DateTimeOffset RegisteredAt,
    DateTimeOffset? CheckedInAt);

public sealed record AttendeeSession(string Name, string Email);
