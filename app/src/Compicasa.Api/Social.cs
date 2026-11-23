using System.Security.Claims;
using Compicasa.Core;
using Microsoft.EntityFrameworkCore;

namespace Compicasa.Api;

/// <summary>One message in a conversation (B-036). Made-up people's replies are scripted: <see cref="Script"/> names the
/// line, which the front end words in the viewer's language, and <see cref="Body"/> is empty.</summary>
public sealed class Message
{
    public const int MaxLength = 1000;
    public long Id { get; set; }
    /// <summary>"p:{id}|{id}" for two mutual matches (ids in ordinal order), "g:{group id}" for a group.</summary>
    public string Thread { get; set; } = "";
    public string FromId { get; set; } = "";
    public string Body { get; set; } = "";
    public string? Script { get; set; }
    public DateTimeOffset At { get; set; }
}

/// <summary>When someone last read a conversation, for unread counts.</summary>
public sealed class ThreadRead
{
    public string AccountId { get; set; } = "";
    public string Thread { get; set; } = "";
    public long LastReadId { get; set; }
}

/// <summary>A block: the two people no longer see each other, message each other or share groups.</summary>
public sealed class Block
{
    public string FromId { get; set; } = "";
    public string ToId { get; set; } = "";
    public DateTimeOffset At { get; set; }
}

/// <summary>A report about a person, kept for the back office (B-041) and the safety process (B-013 section 6).</summary>
public sealed class Report
{
    public static readonly string[] Reasons = ["fake", "harassment", "scam", "unsafe", "other"];
    public long Id { get; set; }
    public string FromId { get; set; } = "";
    public string AboutId { get; set; } = "";
    public string? Thread { get; set; }
    public string Reason { get; set; } = "";
    public string? Note { get; set; }
    public DateTimeOffset At { get; set; }
}

public sealed record MessageRequest(string? Body);
public sealed record ReportRequest(string? About, string? Thread, string? Reason, string? Note, bool Block);
public sealed record InviteRequest(string? Id);

public static class Threads
{
    public static string Pair(string a, string b) => string.CompareOrdinal(a, b) < 0 ? $"p:{a}|{b}" : $"p:{b}|{a}";
    public static string Group(Guid id) => $"g:{id:N}";
    static bool Involves(string thread, string id) => thread.StartsWith("p:") && thread[2..].Split('|').Contains(id);

    /// <summary>Removes everything social that involves these accounts or groups: when an account is deleted, and when
    /// the daily batch of made-up people is replaced (D-029).</summary>
    public static async Task ForgetAsync(AppDb db, IReadOnlyCollection<string> accounts, IReadOnlyCollection<Guid> groups, CancellationToken ct = default)
    {
        var groupThreads = groups.Select(Group).ToList();
        var pairThreads = (await db.Messages.Where(m => m.Thread.StartsWith("p:")).Select(m => m.Thread).Distinct().ToListAsync(ct))
            .Concat(await db.ThreadReads.Where(r => r.Thread.StartsWith("p:")).Select(r => r.Thread).Distinct().ToListAsync(ct))
            .Where(t => accounts.Any(id => Involves(t, id))).Distinct().ToList();
        var threads = groupThreads.Concat(pairThreads).ToList();
        await db.Messages.Where(m => threads.Contains(m.Thread) || accounts.Contains(m.FromId)).ExecuteDeleteAsync(ct);
        await db.ThreadReads.Where(r => threads.Contains(r.Thread) || accounts.Contains(r.AccountId)).ExecuteDeleteAsync(ct);
        await db.Notifications.Where(n => accounts.Contains(n.AccountId) || (n.ActorId != null && accounts.Contains(n.ActorId))
            || (n.GroupId != null && groups.Contains(n.GroupId.Value))).ExecuteDeleteAsync(ct);
        await db.Blocks.Where(b => accounts.Contains(b.FromId) || accounts.Contains(b.ToId)).ExecuteDeleteAsync(ct);
        // Reports someone made go with their account; reports about them are kept for the safety record (B-013).
        await db.Reports.Where(r => accounts.Contains(r.FromId)).ExecuteDeleteAsync(ct);
    }
}

/// <summary>Scripted replies from made-up people, in order; the last one repeats. Wording is in i18n.js (scr_*).</summary>
public static class Scripts
{
    public static readonly string[] Pair = ["pHello", "pBrief", "pMoney", "pViewings", "pMore"];
    public static readonly string[] Group = ["gHello", "gBrief", "gRooms", "gLawyer", "gMore"];
    public static string Pick(string[] lines, int alreadySent) => lines[Math.Min(alreadySent, lines.Length - 1)];
}

public static class Social
{
    static string? UserId(ClaimsPrincipal u) => u.FindFirstValue(ClaimTypes.NameIdentifier);
    static IResult Error(string code, int status = 400) => Results.Json(new { error = code }, statusCode: status);

    static async Task<Account?> OnboardedAsync(HttpContext http, AppDb db)
    {
        var id = UserId(http.User);
        return id is null ? null : await db.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.Kind == AccountKind.User && a.OnboardedAt != null, http.RequestAborted);
    }

    /// <summary>The conversations the viewer can read: one per mutual match, one per group they are still in.</summary>
    sealed record Conv(string Kind, string Id, string Thread, IReadOnlyList<string> Members, string? State);

    static async Task<List<Conv>> ConversationsAsync(AppDb db, Community c, CancellationToken ct)
    {
        var list = c.MutualMatches().Select(p => new Conv("person", p.Id, Threads.Pair(c.Viewer.Id, p.Id), [p.Id], null)).ToList();
        var groups = await db.Groups.AsNoTracking().Include(g => g.Members)
            .Where(g => g.Members.Any(m => m.AccountId == c.Viewer.Id && (m.Status == MemberStatus.Pending || m.Status == MemberStatus.Accepted))).ToListAsync(ct);
        list.AddRange(groups.Select(g => new Conv("group", g.Id.ToString(), Threads.Group(g.Id),
            Lifecycle.Active(g).OrderBy(m => m.Position).Select(m => m.AccountId).Where(id => c.Get(id) is not null).ToList(), Lifecycle.State(g))));
        return list;
    }

    static object View(Message m) => new { id = m.Id, from = m.FromId, body = m.Body, script = m.Script, at = m.At };

    static async Task<Dictionary<string, long>> ReadMarksAsync(AppDb db, string viewerId, CancellationToken ct) =>
        await db.ThreadReads.AsNoTracking().Where(r => r.AccountId == viewerId).ToDictionaryAsync(r => r.Thread, r => r.LastReadId, ct);

    static async Task MarkReadAsync(AppDb db, string viewerId, string thread, long lastId, CancellationToken ct)
    {
        var row = await db.ThreadReads.FindAsync([viewerId, thread], ct);
        if (row is null) db.ThreadReads.Add(new ThreadRead { AccountId = viewerId, Thread = thread, LastReadId = lastId });
        else if (row.LastReadId < lastId) row.LastReadId = lastId;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>How many conversations have messages from someone else that the viewer hasn't read.</summary>
    static async Task<int> UnreadThreadsAsync(AppDb db, Community c, CancellationToken ct)
    {
        var convs = await ConversationsAsync(db, c, ct);
        var threads = convs.Select(x => x.Thread).ToList();
        var marks = await ReadMarksAsync(db, c.Viewer.Id, ct);
        var latest = await db.Messages.AsNoTracking().Where(m => threads.Contains(m.Thread) && m.FromId != c.Viewer.Id)
            .GroupBy(m => m.Thread).Select(g => new { g.Key, Last = g.Max(m => m.Id) }).ToListAsync(ct);
        return latest.Count(x => x.Last > marks.GetValueOrDefault(x.Key));
    }

    public static void Map(WebApplication app)
    {
        var me = app.MapGroup("/api").RequireAuthorization();

        // ---------- notifications (B-037) ----------
        me.MapGet("/inbox/count", async (HttpContext http, AppDb db, TimeProvider clock) =>
        {
            if (await OnboardedAsync(http, db) is not { } a) return Results.Unauthorized();
            await Lifecycle.ExpireAsync(db, a.Id, clock.GetUtcNow(), http.RequestAborted);
            var c = await Community.LoadAsync(db, a, http.RequestAborted);
            var alerts = await db.Notifications.CountAsync(n => n.AccountId == a.Id && n.ReadAt == null, http.RequestAborted);
            var invites = await db.GroupMembers.CountAsync(m => m.AccountId == a.Id && m.Status == MemberStatus.Pending, http.RequestAborted);
            return Results.Ok(new { alerts, messages = await UnreadThreadsAsync(db, c, http.RequestAborted), invites });
        });

        me.MapGet("/inbox", async (HttpContext http, AppDb db, TimeProvider clock) =>
        {
            if (await OnboardedAsync(http, db) is not { } a) return Results.Unauthorized();
            await Lifecycle.ExpireAsync(db, a.Id, clock.GetUtcNow(), http.RequestAborted);
            var c = await Community.LoadAsync(db, a, http.RequestAborted);
            var rows = await db.Notifications.AsNoTracking().Where(n => n.AccountId == a.Id).OrderByDescending(n => n.Id).Take(60).ToListAsync(http.RequestAborted);
            var myGroups = (await db.GroupMembers.AsNoTracking().Where(m => m.AccountId == a.Id).ToListAsync(http.RequestAborted))
                .ToDictionary(m => m.GroupId, m => m.Status);
            // Leave out anything about someone the viewer can no longer see (blocked, deleted, replaced) or a group that's gone.
            var shown = rows.Where(n => (n.ActorId is null || c.Get(n.ActorId) is not null) && (n.GroupId is null || myGroups.ContainsKey(n.GroupId.Value))).ToList();
            return Results.Ok(new
            {
                items = shown.Select(n => new
                {
                    id = n.Id, kind = n.Kind, actor = n.ActorId, group = n.GroupId, at = n.At, read = n.ReadAt is not null,
                    // Can the viewer still open the group from here?
                    groupOpen = n.GroupId is { } gid && MemberStatus.IsActive(myGroups[gid]),
                }),
                people = shown.Where(n => n.ActorId is not null).Select(n => c.Get(n.ActorId!)!).DistinctBy(p => p.Id)
                    .ToDictionary(p => p.Id, p => new { name = p.Name, color = p.Color, isDemo = c.IsDemo(p.Id) }),
            });
        });

        me.MapPost("/inbox/read", async (HttpContext http, AppDb db, TimeProvider clock) =>
        {
            if (await OnboardedAsync(http, db) is not { } a) return Results.Unauthorized();
            var now = clock.GetUtcNow();
            var unread = await db.Notifications.Where(n => n.AccountId == a.Id && n.ReadAt == null).ToListAsync(http.RequestAborted);
            foreach (var n in unread) n.ReadAt = now;
            await db.SaveChangesAsync(http.RequestAborted);
            return Results.NoContent();
        });

        // ---------- conversations (B-036) ----------
        me.MapGet("/chats", async (HttpContext http, AppDb db) =>
        {
            if (await OnboardedAsync(http, db) is not { } a) return Results.Unauthorized();
            var c = await Community.LoadAsync(db, a, http.RequestAborted);
            var convs = await ConversationsAsync(db, c, http.RequestAborted);
            var threads = convs.Select(x => x.Thread).ToList();
            var marks = await ReadMarksAsync(db, a.Id, http.RequestAborted);
            var lastIds = await db.Messages.AsNoTracking().Where(m => threads.Contains(m.Thread)).GroupBy(m => m.Thread)
                .Select(g => g.Max(m => m.Id)).ToListAsync(http.RequestAborted);
            var last = (await db.Messages.AsNoTracking().Where(m => lastIds.Contains(m.Id)).ToListAsync(http.RequestAborted)).ToDictionary(m => m.Thread);
            var unreadRows = await db.Messages.AsNoTracking().Where(m => threads.Contains(m.Thread) && m.FromId != a.Id)
                .Select(m => new { m.Thread, m.Id }).ToListAsync(http.RequestAborted);
            var items = convs.Select(x => new
            {
                kind = x.Kind, id = x.Id, members = x.Members, state = x.State,
                last = last.TryGetValue(x.Thread, out var l) ? View(l) : null,
                lastId = last.TryGetValue(x.Thread, out var l2) ? l2.Id : 0,
                unread = unreadRows.Count(r => r.Thread == x.Thread && r.Id > marks.GetValueOrDefault(x.Thread)),
            }).OrderByDescending(x => x.lastId).ThenBy(x => x.kind).ToList();
            var people = items.SelectMany(x => x.members).Distinct().Select(c.Get).OfType<Profile>()
                .ToDictionary(p => p.Id, p => new { id = p.Id, name = p.Name, color = p.Color, isDemo = c.IsDemo(p.Id) });
            return Results.Ok(new { items, people });
        });

        // Resolves a conversation the viewer may read: a mutual match's id, or a group they are still in.
        async Task<(Conv? Conv, Community C)> Find(AppDb db, Account a, string kind, string id, CancellationToken ct)
        {
            var c = await Community.LoadAsync(db, a, ct);
            var convs = await ConversationsAsync(db, c, ct);
            return (convs.FirstOrDefault(x => x.Kind == kind && x.Id == id), c);
        }

        async Task<IResult> Thread(AppDb db, Account a, Conv conv, Community c, CancellationToken ct)
        {
            var msgs = await db.Messages.AsNoTracking().Where(m => m.Thread == conv.Thread).OrderByDescending(m => m.Id).Take(200).ToListAsync(ct);
            msgs.Reverse();
            if (msgs.Count > 0) await MarkReadAsync(db, a.Id, conv.Thread, msgs[^1].Id, ct);
            var people = conv.Members.Append(a.Id).Concat(msgs.Select(m => m.FromId)).Distinct().Select(c.Get).OfType<Profile>()
                .ToDictionary(p => p.Id, p => new { id = p.Id, name = p.Name, color = p.Color, isDemo = c.IsDemo(p.Id) });
            return Results.Ok(new
            {
                kind = conv.Kind, id = conv.Id, members = conv.Members, state = conv.State,
                conduct = a.ConductAcceptedAt is not null,
                // Messages from someone the viewer has since blocked, or who has left, stay out.
                messages = msgs.Where(m => people.ContainsKey(m.FromId)).Select(View),
                people,
            });
        }

        me.MapGet("/chats/{kind:regex(^(person|group)$)}/{id}", async (string kind, string id, HttpContext http, AppDb db) =>
        {
            if (await OnboardedAsync(http, db) is not { } a) return Results.Unauthorized();
            var (conv, c) = await Find(db, a, kind, id, http.RequestAborted);
            return conv is null ? Error("notFound", 404) : await Thread(db, a, conv, c, http.RequestAborted);
        });

        me.MapPost("/chats/{kind:regex(^(person|group)$)}/{id}", async (string kind, string id, MessageRequest r, HttpContext http, AppDb db, TimeProvider clock) =>
        {
            if (await OnboardedAsync(http, db) is not { } a) return Results.Unauthorized();
            if (a.ConductAcceptedAt is null) return Error("conductNeeded", 409);
            var body = (r.Body ?? "").Trim();
            if (body.Length == 0 || body.Length > Message.MaxLength) return Error("badMessage");
            var (conv, c) = await Find(db, a, kind, id, http.RequestAborted);
            if (conv is null) return Error("notFound", 404);
            var now = clock.GetUtcNow();
            // A gentle limit against floods: 20 messages a minute.
            var recent = await db.Messages.AsNoTracking().Where(m => m.FromId == a.Id).OrderByDescending(m => m.Id).Take(20).Select(m => m.At).ToListAsync(http.RequestAborted);
            if (recent.Count == 20 && recent.All(t => now - t < TimeSpan.FromMinutes(1))) return Error("rateLimited", 429);
            db.Messages.Add(new Message { Thread = conv.Thread, FromId = a.Id, Body = body, At = now });
            // Made-up people reply at once with a scripted line: in a pair, the other person; in a group, the made-up
            // members take turns.
            var demos = conv.Members.Where(c.IsDemo).ToList();
            if (demos.Count > 0)
            {
                var sent = await db.Messages.CountAsync(m => m.Thread == conv.Thread && demos.Contains(m.FromId), http.RequestAborted);
                var from = demos[sent % demos.Count];
                var n = conv.Kind == "person" ? sent : sent / demos.Count;
                db.Messages.Add(new Message { Thread = conv.Thread, FromId = from, Script = Scripts.Pick(conv.Kind == "person" ? Scripts.Pair : Scripts.Group, n), At = now.AddMilliseconds(1) });
            }
            await db.SaveChangesAsync(http.RequestAborted);
            return await Thread(db, a, conv, c, http.RequestAborted);
        });

        // The code of conduct, agreed to before the first message (B-036).
        me.MapPost("/me/conduct", async (HttpContext http, AppDb db, TimeProvider clock) =>
        {
            if (await OnboardedAsync(http, db) is not { } a) return Results.Unauthorized();
            a.ConductAcceptedAt ??= clock.GetUtcNow();
            await db.SaveChangesAsync(http.RequestAborted);
            return Results.Ok(Accounts.Me(a));
        });

        // ---------- blocking and reporting ----------
        me.MapGet("/blocks", async (HttpContext http, AppDb db) =>
        {
            if (await OnboardedAsync(http, db) is not { } a) return Results.Unauthorized();
            var ids = await db.Blocks.AsNoTracking().Where(b => b.FromId == a.Id).Select(b => b.ToId).ToListAsync(http.RequestAborted);
            var accounts = await db.Accounts.AsNoTracking().Where(x => ids.Contains(x.Id)).ToListAsync(http.RequestAborted);
            return Results.Ok(accounts.Select(x => x.ReadProfile()).Select(p => new { id = p.Id, name = p.Name, color = p.Color }));
        });

        me.MapPut("/blocks/{id}", async (string id, HttpContext http, AppDb db, TimeProvider clock) =>
        {
            if (await OnboardedAsync(http, db) is not { } a) return Results.Unauthorized();
            if (id == a.Id || !await db.Accounts.AnyAsync(x => x.Id == id, http.RequestAborted)) return Error("unknownPerson", 404);
            await BlockAsync(db, a.Id, id, clock.GetUtcNow(), http.RequestAborted);
            return Results.NoContent();
        });

        me.MapDelete("/blocks/{id}", async (string id, HttpContext http, AppDb db) =>
        {
            if (await OnboardedAsync(http, db) is not { } a) return Results.Unauthorized();
            await db.Blocks.Where(b => b.FromId == a.Id && b.ToId == id).ExecuteDeleteAsync(http.RequestAborted);
            return Results.NoContent();
        });

        me.MapPost("/reports", async (ReportRequest r, HttpContext http, AppDb db, TimeProvider clock) =>
        {
            if (await OnboardedAsync(http, db) is not { } a) return Results.Unauthorized();
            var about = r.About ?? "";
            if (about == a.Id || !await db.Accounts.AnyAsync(x => x.Id == about, http.RequestAborted)) return Error("unknownPerson", 404);
            if (r.Reason is null || !Report.Reasons.Contains(r.Reason)) return Error("badReason");
            var note = (r.Note ?? "").Trim();
            if (note.Length > 1000) return Error("badMessage");
            var now = clock.GetUtcNow();
            db.Reports.Add(new Report { FromId = a.Id, AboutId = about, Thread = r.Thread is { Length: <= 90 } t ? t : null, Reason = r.Reason, Note = note.Length > 0 ? note : null, At = now });
            await db.SaveChangesAsync(http.RequestAborted);
            if (r.Block) await BlockAsync(db, a.Id, about, now, http.RequestAborted);
            return Results.NoContent();
        });
    }

    /// <summary>Blocks someone: from now on neither sees the other, and the blocker leaves every group they share,
    /// so nobody is left in a group with someone they blocked.</summary>
    public static async Task BlockAsync(AppDb db, string from, string to, DateTimeOffset now, CancellationToken ct)
    {
        if (await db.Blocks.FindAsync([from, to], ct) is null) db.Blocks.Add(new Block { FromId = from, ToId = to, At = now });
        var shared = await db.Groups.Include(g => g.Members)
            .Where(g => g.Members.Any(m => m.AccountId == from && (m.Status == MemberStatus.Pending || m.Status == MemberStatus.Accepted))
                     && g.Members.Any(m => m.AccountId == to && (m.Status == MemberStatus.Pending || m.Status == MemberStatus.Accepted))).ToListAsync(ct);
        foreach (var g in shared)
        {
            var mine = g.Members.First(m => m.AccountId == from);
            mine.Status = mine.Status == MemberStatus.Pending ? MemberStatus.Declined : MemberStatus.Left;
            mine.RespondedAt = now;
            // The others are told the blocker left or declined, never why.
            await Lifecycle.NotifyMembersAsync(db, g, mine.Status == MemberStatus.Left ? NoteKind.Left : NoteKind.Declined, from, now, from, ct);
        }
        await db.SaveChangesAsync(ct);
    }
}
