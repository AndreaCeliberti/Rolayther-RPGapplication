using Rolayther.Models.Entities;
using Rolayther.Models.Enums;
using Rolayther.Services;
using System.ComponentModel.DataAnnotations;
using System.Data;

public class Session : BaseEntity
{
    [Key]
    public Guid SessionId { get; set; }

   

    [Required]
    [MaxLength(100)]
    public string SessionTitle { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? SessionDescription { get; set; }

    [Required]
    public DateTime ScheduledAt { get; set; }

    [Required]
    [MaxLength(100)]
    public string Duration { get; set; } = string.Empty;

    [Required]
    public int NumbOfPlayer { get; set; }

    public string? SessionLink { get; set; }

    public string? CoverImgUrl { get; set; }

    public Guid MasterId { get; set; }
    public Master? Master { get; set; }

    public Guid GameId { get; set; }
    public Game? Game { get; set; }

    public Guid GenreId { get; set; }
    public Genre? Genre { get; set; }

    public ICollection<Player> Players { get; set; } = new List<Player>();

    public SessionState CurrentState { get; set; } = SessionState.Draft;

    public ICollection<SessionStateHistory> StateHistory { get; set; } = new List<SessionStateHistory>();


    // Change status method
    public void ChangeState(SessionState newState, string userId, string role, string? reason = null)
    {
        //  idempotenza
        if (CurrentState == newState)
            return;

        if (!SessionStateService.CanTransition(CurrentState, newState))
            throw new InvalidOperationException($"Invalid transition from {CurrentState} to {newState}");

        var historyEntry = new SessionStateHistory
        {
            Id = Guid.NewGuid(),
            SessionId = SessionId,
            FromState = CurrentState,
            ToState = newState,
            ChangedByUserId = userId,
            ChangedByRole = role,
            Reason = reason,
            ChangedAtUtc = DateTime.UtcNow
        };

        CurrentState = newState;
        StateHistory.Add(historyEntry);
    }

    // Add player to session
    public void AddPlayer(Player player, string userId, string role)
    {
        if (CurrentState != SessionState.Published)
            throw new InvalidOperationException("Cannot join a session that is not published.");

        if (Players.Any(p => p.PlayerId == player.PlayerId))
            throw new InvalidOperationException("Player already joined.");

        if (Players.Count >= NumbOfPlayer)
            throw new InvalidOperationException("Session is already full.");

        Players.Add(player);

        //  se raggiunge il max -> Full
        if (Players.Count == NumbOfPlayer)
        {
            ChangeState(SessionState.Full, userId, role, "Session reached max number of players");
        }
    }

    // Remove player from session ( fix: usa PlayerId, non reference equality)
    public void RemovePlayer(Player player, string userId, string role)
    {
        var existing = Players.FirstOrDefault(p => p.PlayerId == player.PlayerId);
        if (existing == null)
            throw new InvalidOperationException("Player not in session.");

        Players.Remove(existing);

        if (CurrentState == SessionState.Full && Players.Count < NumbOfPlayer)
        {
            ChangeState(SessionState.Published, userId, role, "Player left session, slots available");
        }
    }
}
