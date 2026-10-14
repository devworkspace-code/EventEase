using System.Text.Json;
using EventEase.Models;
using Microsoft.JSInterop;

namespace EventEase.Services;

public sealed class EventEaseState(IJSRuntime jsRuntime)
{
    private const string PersistentStorageKey = "eventease-data";
    private const string SessionStorageKey = "eventease-attendee-session";
    private static readonly JsonSerializerOptions StorageJsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IJSRuntime _jsRuntime = jsRuntime;
    private Task? _initializationTask;
    private readonly List<EventItem> _events = EventCatalog.Events.ToList();
    private readonly List<EventRegistrationRecord> _registrations = [];

    public IReadOnlyList<EventItem> Events => _events;
    public IReadOnlyList<EventRegistrationRecord> Registrations => _registrations;
    public AttendeeSession? CurrentAttendee { get; private set; }
    public string? SessionWarning { get; private set; }
    public event Action? Changed;

    public bool IsCurrentAttendeeRegistered(int eventId) =>
        CurrentAttendee is { } attendee &&
        _registrations.Any(registration =>
            registration.EventId == eventId &&
            string.Equals(registration.Email, attendee.Email, StringComparison.OrdinalIgnoreCase));

    public async Task InitializeAsync()
    {
        _initializationTask ??= InitializeCoreAsync();
        try
        {
            await _initializationTask;
        }
        catch
        {
            _initializationTask = null;
            throw;
        }
    }

    public EventItem? FindEvent(int id) => _events.FirstOrDefault(eventItem => eventItem.Id == id);

    public async Task SaveEventAsync(EventItem updatedEvent)
    {
        await InitializeAsync();
        var index = _events.FindIndex(eventItem => eventItem.Id == updatedEvent.Id);
        if (index < 0)
        {
            throw new InvalidOperationException($"Event {updatedEvent.Id} no longer exists.");
        }

        var updatedEvents = _events.ToArray();
        updatedEvents[index] = updatedEvent;
        await SavePersistentStateAsync(updatedEvents, _registrations.ToArray());
        _events[index] = updatedEvent;
        NotifyStateChanged();
    }

    public async Task<EventRegistrationRecord> RegisterAsync(int eventId, string name, string email)
    {
        await InitializeAsync();
        if (FindEvent(eventId) is null)
        {
            throw new InvalidOperationException("This event is no longer available.");
        }

        var existingRegistration = _registrations.FirstOrDefault(registration =>
            registration.EventId == eventId &&
            string.Equals(registration.Email, email.Trim(), StringComparison.OrdinalIgnoreCase));
        if (existingRegistration is not null)
        {
            throw new InvalidOperationException("This email address is already registered for the event.");
        }

        var attendee = new AttendeeSession(name.Trim(), email.Trim());
        var registration = new EventRegistrationRecord(
            Guid.NewGuid(),
            eventId,
            attendee.Name,
            attendee.Email,
            DateTimeOffset.UtcNow,
            null);

        var updatedRegistrations = _registrations.Append(registration).ToArray();
        await SavePersistentStateAsync(_events.ToArray(), updatedRegistrations);
        _registrations.Add(registration);
        CurrentAttendee = attendee;
        SessionWarning = null;
        try
        {
            await SaveSessionAsync();
        }
        catch (JSException)
        {
            SessionWarning = "Your registration was saved, but this browser could not save the attendee session.";
        }

        NotifyStateChanged();
        return registration;
    }

    public async Task SetAttendanceAsync(Guid registrationId, bool isCheckedIn)
    {
        await InitializeAsync();
        var index = _registrations.FindIndex(registration => registration.Id == registrationId);
        if (index < 0)
        {
            throw new InvalidOperationException("This registration could not be found.");
        }

        var updatedRegistration = _registrations[index] with
        {
            CheckedInAt = isCheckedIn ? DateTimeOffset.UtcNow : null
        };
        var updatedRegistrations = _registrations.ToArray();
        updatedRegistrations[index] = updatedRegistration;
        await SavePersistentStateAsync(_events.ToArray(), updatedRegistrations);
        _registrations[index] = updatedRegistration;
        NotifyStateChanged();
    }

    private async Task InitializeCoreAsync()
    {
        var persistentJson = await _jsRuntime.InvokeAsync<string?>(
            "eventEaseStorage.get",
            PersistentStorageKey,
            false);
        if (!string.IsNullOrWhiteSpace(persistentJson))
        {
            var savedState = JsonSerializer.Deserialize<PersistentState>(persistentJson, StorageJsonOptions)
                ?? throw new JsonException("Saved EventEase data was empty.");

            if (savedState.Events is { Length: > 0 })
            {
                _events.Clear();
                _events.AddRange(savedState.Events);
            }

            if (savedState.Registrations is not null)
            {
                _registrations.Clear();
                _registrations.AddRange(savedState.Registrations);
            }
        }

        var sessionJson = await _jsRuntime.InvokeAsync<string?>(
            "eventEaseStorage.get",
            SessionStorageKey,
            true);
        if (!string.IsNullOrWhiteSpace(sessionJson))
        {
            CurrentAttendee = JsonSerializer.Deserialize<AttendeeSession>(sessionJson, StorageJsonOptions)
                ?? throw new JsonException("Saved attendee session was empty.");
        }
    }

    private ValueTask SavePersistentStateAsync(EventItem[] events, EventRegistrationRecord[] registrations) =>
        _jsRuntime.InvokeVoidAsync(
            "eventEaseStorage.set",
            PersistentStorageKey,
            new PersistentState(events, registrations),
            false);

    private ValueTask SaveSessionAsync() =>
        _jsRuntime.InvokeVoidAsync(
            "eventEaseStorage.set",
            SessionStorageKey,
            CurrentAttendee,
            true);

    private void NotifyStateChanged() => Changed?.Invoke();

    private sealed record PersistentState(EventItem[]? Events, EventRegistrationRecord[]? Registrations);
}
