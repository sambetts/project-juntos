using Compicasa.Core;
using Microsoft.EntityFrameworkCore;

namespace Compicasa.Api;

/// <summary>
/// What happens to a group after it is proposed (B-038, D-039): invitations expire after <see cref="InviteDays"/> days;
/// the person who sent an invitation can withdraw it; someone who declines, leaves, lets an invitation expire or is
/// withdrawn frees a place, and any member who has said yes can invite one of their own mutual matches into it, up to
/// the size the group was proposed at. The group carries on with whoever is still in: it is formed once everyone still
/// in has said yes, and needs at least two households.
/// </summary>
public static class Lifecycle
{
    public const int InviteDays = 14;
    public const int MaxHouseholds = 4;

    public static DateTimeOffset InvitedAt(Group g, GroupMember m) => m.InvitedAt ?? g.CreatedAt;
    public static DateTimeOffset ExpiresAt(Group g, GroupMember m) => InvitedAt(g, m).AddDays(InviteDays);
    public static int PlannedSize(Group g) => g.PlannedSize ?? g.Members.Count;
    public static List<GroupMember> Active(Group g) => g.Members.Where(m => MemberStatus.IsActive(m.Status)).ToList();
    public static int FreePlaces(Group g) => Math.Max(0, Math.Min(PlannedSize(g), MaxHouseholds) - Active(g).Count);

    public static string State(Group g)
    {
        var active = Active(g);
        if (active.Count < 2) return "broken";
        return active.All(m => m.Status == MemberStatus.Accepted) ? "formed" : "pending";
    }

    /// <summary>
    /// Expires the overdue invitations in the viewer's groups and tells the others. Done when the viewer looks, so no
    /// background job is needed; dates are compared in memory because SQLite can't compare DateTimeOffset values.
    /// </summary>
    public static async Task ExpireAsync(AppDb db, string viewerId, DateTimeOffset now, CancellationToken ct = default)
    {
        var mine = await db.GroupMembers.Where(m => m.AccountId == viewerId).Select(m => m.GroupId).ToListAsync(ct);
        if (mine.Count == 0) return;
        var groups = await db.Groups.Include(g => g.Members)
            .Where(g => mine.Contains(g.Id) && g.Members.Any(m => m.Status == MemberStatus.Pending)).ToListAsync(ct);
        var changed = false;
        foreach (var g in groups)
        {
            var wasFormed = State(g) == "formed";
            foreach (var m in g.Members.Where(m => m.Status == MemberStatus.Pending && ExpiresAt(g, m) <= now).ToList())
            {
                m.Status = MemberStatus.Expired;
                m.RespondedAt = now;
                await NotifyMembersAsync(db, g, NoteKind.Expired, m.AccountId, now, except: m.AccountId, ct);
                changed = true;
            }
            await AfterChangeAsync(db, g, wasFormed, now, null, ct);
        }
        if (changed) await db.SaveChangesAsync(ct);
    }

    /// <summary>Tells everyone still in, once, when the group has just formed.</summary>
    public static async Task AfterChangeAsync(AppDb db, Group g, bool wasFormed, DateTimeOffset now, string? except, CancellationToken ct = default)
    {
        if (!wasFormed && State(g) == "formed")
            await NotifyMembersAsync(db, g, NoteKind.Formed, null, now, except, ct);
    }

    /// <summary>A notification to each real member still in the group, other than <paramref name="except"/>.</summary>
    public static async Task NotifyMembersAsync(AppDb db, Group g, string kind, string? actorId, DateTimeOffset now, string? except, CancellationToken ct = default)
    {
        foreach (var m in Active(g).Where(m => m.AccountId != except && m.AccountId != actorId))
            await Notes.AddAsync(db, m.AccountId, kind, actorId, g.Id, now, ct);
    }

    /// <summary>
    /// Can <paramref name="c"/>'s viewer invite <paramref name="newId"/> into a free place in <paramref name="g"/>?
    /// They must be the viewer's mutual match, not already in or answered, and fit the group: the group-level rules
    /// over everyone still in, and the made-up members' own rule about the newcomer.
    /// </summary>
    public static Msg? CheckInvite(Community c, Group g, string newId)
    {
        var me = g.Members.FirstOrDefault(m => m.AccountId == c.Viewer.Id);
        if (me is null || me.Status != MemberStatus.Accepted) return new Msg("notMember");
        if (FreePlaces(g) == 0) return new Msg("noFreePlace");
        var row = g.Members.FirstOrDefault(m => m.AccountId == newId);
        if (row is not null && row.Status is not (MemberStatus.Expired or MemberStatus.Withdrawn)) return new Msg("alreadyAsked");
        if (c.Get(newId) is not { } p || newId == c.Viewer.Id) return new Msg("unknownPerson");
        if (!c.Mutual(newId)) return new Msg("notMutual", newId);
        var others = Active(g).Where(m => m.AccountId != c.Viewer.Id).Select(m => c.Get(m.AccountId)).OfType<Profile>().ToList();
        foreach (var o in others.Where(o => c.IsDemo(o.Id)))
            if (!Matching.Accepts(o, p)) return new Msg("othersReject", new[] { o.Id, newId });
        var check = Matching.GroupCheck([c.Me, .. others, p]);
        return check.Ok ? null : check.Issues.FirstOrDefault() ?? new Msg("groupInvalid");
    }
}

public static class NoteKind
{
    /// <summary>A real person liked you (you haven't answered them).</summary>
    public const string Like = "like";
    /// <summary>A real person you liked liked you back.</summary>
    public const string Match = "match";
    public const string Invite = "invite";
    public const string Accepted = "accepted";
    public const string Declined = "declined";
    public const string Left = "left";
    public const string Expired = "expired";
    /// <summary>Your invitation was withdrawn.</summary>
    public const string Withdrawn = "withdrawn";
    /// <summary>Someone was invited into a free place in your group (the actor is the newcomer).</summary>
    public const string Invited = "invited";
    public const string Formed = "formed";
}

/// <summary>An event in someone's in-app inbox (B-037). Only real people get them; wording lives in the front end.</summary>
public sealed class Notification
{
    public long Id { get; set; }
    public string AccountId { get; set; } = "";
    public string Kind { get; set; } = "";
    public string? ActorId { get; set; }
    public Guid? GroupId { get; set; }
    public DateTimeOffset At { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}

public static class Notes
{
    public static async Task AddAsync(AppDb db, string to, string kind, string? actorId, Guid? groupId, DateTimeOffset now, CancellationToken ct = default)
    {
        if (to == actorId) return;
        if (!await db.Accounts.AnyAsync(a => a.Id == to && a.Kind == AccountKind.User, ct)) return;
        db.Notifications.Add(new Notification { AccountId = to, Kind = kind, ActorId = actorId, GroupId = groupId, At = now });
    }
}
