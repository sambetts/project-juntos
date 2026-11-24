using System.Text.Json;
using Compicasa.Core;
using Microsoft.EntityFrameworkCore;

namespace Compicasa.Api;

public static class AccountKind
{
    /// <summary>Someone who signed up.</summary>
    public const string User = "user";
    /// <summary>One of the 14 hand-written made-up profiles from the prototype.</summary>
    public const string Seed = "seed";
    /// <summary>A made-up profile from the daily batch (D-029).</summary>
    public const string Generated = "generated";
}

public sealed class Account
{
    public string Id { get; set; } = "";
    public string Kind { get; set; } = AccountKind.User;
    /// <summary>Session 12's log-in name; no longer used for new accounts (D-033). Null for made-up profiles.</summary>
    public string? Username { get; set; }
    /// <summary>Lower-case email address, used to sign in. Null for made-up profiles.</summary>
    public string? Email { get; set; }
    /// <summary>The person's object ID in the Entra External ID tenant, if they sign in there (D-033).</summary>
    public string? ExternalId { get; set; }
    /// <summary>Only for local accounts (developers and tests); External ID keeps its own.</summary>
    public string? PasswordHash { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>When the person confirmed they are 18 or over.</summary>
    public DateTimeOffset? AdultConfirmedAt { get; set; }
    /// <summary>Which version of the terms and privacy notice they accepted, and when.</summary>
    public string? TermsVersion { get; set; }
    public DateTimeOffset? TermsAcceptedAt { get; set; }
    /// <summary>When they finished the onboarding wizard. Until then nobody else sees them, and they see no one.</summary>
    public DateTimeOffset? OnboardedAt { get; set; }
    /// <summary>When they agreed to the code of conduct, asked before their first message (B-036).</summary>
    public DateTimeOffset? ConductAcceptedAt { get; set; }
    public string ProfileJson { get; set; } = "{}";

    public bool IsDemo => Kind != AccountKind.User;
    /// <summary>Visible to other people: made-up profiles always; real people once onboarded.</summary>
    public bool IsListed => IsDemo || OnboardedAt is not null;

    public Profile ReadProfile()
    {
        var p = JsonSerializer.Deserialize<Profile>(ProfileJson, Seeds.Json) ?? new Profile();
        p.Id = Id;
        p.Real = !IsDemo;
        return p;
    }

    public void WriteProfile(Profile p) => ProfileJson = JsonSerializer.Serialize(p, Seeds.Json);
}

public static class ReactionKind
{
    public const string Like = "like";
    public const string Pass = "pass";
}

public sealed class Reaction
{
    public string FromId { get; set; } = "";
    public string ToId { get; set; } = "";
    public string Kind { get; set; } = ReactionKind.Like;
    public DateTimeOffset At { get; set; }
}

public sealed class Group
{
    public Guid Id { get; set; }
    public string CreatedById { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public int? M2Min { get; set; }
    public int? M2Max { get; set; }
    /// <summary>How many households the group was proposed with, the proposer included. Freed places can be filled up to this (B-038).
    /// Null for groups made before Session 19, which never had replacements, so their member count is the size.</summary>
    public int? PlannedSize { get; set; }
    public List<GroupMember> Members { get; set; } = [];
}

public static class MemberStatus
{
    public const string Pending = "pending";
    public const string Accepted = "accepted";
    public const string Declined = "declined";
    public const string Left = "left";
    /// <summary>Didn't answer within <see cref="Lifecycle.InviteDays"/> days (B-038).</summary>
    public const string Expired = "expired";
    /// <summary>The person who invited them took the invitation back before they answered.</summary>
    public const string Withdrawn = "withdrawn";

    /// <summary>Still part of the group: invited and not answered yet, or said yes.</summary>
    public static bool IsActive(string s) => s is Pending or Accepted;
}

public sealed class GroupMember
{
    public Guid GroupId { get; set; }
    public string AccountId { get; set; } = "";
    /// <summary>Order in the group, proposer first.</summary>
    public int Position { get; set; }
    public string Status { get; set; } = MemberStatus.Pending;
    public DateTimeOffset? RespondedAt { get; set; }
    /// <summary>When the invitation was sent; null for rows made before Session 19 (use the group's CreatedAt).</summary>
    public DateTimeOffset? InvitedAt { get; set; }
    /// <summary>Who sent the invitation; null for the proposer and for rows made before Session 19 (the proposer).</summary>
    public string? InvitedById { get; set; }
}

/// <summary>One row: which day's made-up batch is loaded (D-029).</summary>
public sealed class DemoState
{
    public int Id { get; set; } = 1;
    public DateOnly BatchDate { get; set; }
    public DateTimeOffset RefreshedAt { get; set; }
}

/// <summary>
/// A home for sale in our index (D-035): facts and the link to the agency's own advert. No photos are ever copied
/// or stored, only the address of the advert's main photo on the agency's own server (D-051), and no description
/// text or agents' contact details. Made-up homes have Source "demo" and no link.
/// </summary>
public sealed class Home
{
    /// <summary>"{source}:{the agency's own reference}".</summary>
    public string Id { get; set; } = "";
    public string Source { get; set; } = "";
    /// <summary>The agency (and office) the advert belongs to, shown with every home.</summary>
    public string Agency { get; set; } = "";
    /// <summary>The agency's own advert. Null for made-up homes.</summary>
    public string? Url { get; set; }
    public int? Price { get; set; }
    public int? M2 { get; set; }
    public int? Beds { get; set; }
    public int? Baths { get; set; }
    /// <summary>"flat" or "house" (Catalogue.Types).</summary>
    public string? Type { get; set; }
    public string? Province { get; set; }
    public string? Municipality { get; set; }
    public string? Postcode { get; set; }
    public string? District { get; set; }
    /// <summary>One of Catalogue.Areas, or null outside them (MadridZones).</summary>
    public string? Area { get; set; }
    /// <summary>Comma-separated HomeFit.Knowable ids.</summary>
    public string Features { get; set; } = "";
    public string? Energy { get; set; }
    /// <summary>The address of the advert's main photo on the agency's own server (D-051): shown from there, never
    /// downloaded or copied. Null if we haven't read the advert since D-051; "" if it has no photo we can use.</summary>
    public string? PhotoUrl { get; set; }
    public DateTimeOffset FirstSeenAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
    /// <summary>When we last read the advert itself (not just saw it in the sitemap).</summary>
    public DateTimeOffset CheckedAt { get; set; }
    /// <summary>Set when the advert left the agency's site.</summary>
    public DateTimeOffset? GoneAt { get; set; }

    public bool IsDemo => Source == HomeSources.Demo;

    public HomeListing ToListing() => new(Id, Source, Agency, Url, Price, M2, Beds, Baths, Type, Municipality, Postcode, District, Area,
        Features.Split(',', StringSplitOptions.RemoveEmptyEntries), Energy, IsDemo, Photo);

    /// <summary>The photo address to send to the browser, or null (D-051).</summary>
    public string? Photo => IsDemo || string.IsNullOrEmpty(PhotoUrl) ? null : PhotoUrl;
}

public sealed class AppDb(DbContextOptions<AppDb> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Reaction> Reactions => Set<Reaction>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<DemoState> DemoStates => Set<DemoState>();
    public DbSet<Home> Homes => Set<Home>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<ThreadRead> ThreadReads => Set<ThreadRead>();
    public DbSet<Block> Blocks => Set<Block>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<CandidateVote> CandidateVotes => Set<CandidateVote>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<SavedHome> SavedHomes => Set<SavedHome>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Account>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(40);
            e.Property(x => x.Kind).HasMaxLength(12);
            e.Property(x => x.Username).HasMaxLength(32);
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.Email).HasMaxLength(254);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.ExternalId).HasMaxLength(64);
            e.HasIndex(x => x.ExternalId).IsUnique();
            e.Property(x => x.TermsVersion).HasMaxLength(32);
            e.HasIndex(x => x.Kind);
            e.Property(x => x.PasswordHash).HasMaxLength(200);
            e.Ignore(x => x.IsDemo);
            e.Ignore(x => x.IsListed);
        });
        b.Entity<Reaction>(e =>
        {
            e.HasKey(x => new { x.FromId, x.ToId });
            e.Property(x => x.FromId).HasMaxLength(40);
            e.Property(x => x.ToId).HasMaxLength(40);
            e.Property(x => x.Kind).HasMaxLength(8);
            e.HasIndex(x => x.ToId);
        });
        b.Entity<Group>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.CreatedById).HasMaxLength(40);
            e.HasMany(x => x.Members).WithOne().HasForeignKey(m => m.GroupId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<GroupMember>(e =>
        {
            e.HasKey(x => new { x.GroupId, x.AccountId });
            e.Property(x => x.AccountId).HasMaxLength(40);
            e.Property(x => x.Status).HasMaxLength(10);
            e.Property(x => x.InvitedById).HasMaxLength(40);
            e.HasIndex(x => x.AccountId);
        });
        b.Entity<Notification>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.AccountId).HasMaxLength(40);
            e.Property(x => x.Kind).HasMaxLength(12);
            e.Property(x => x.ActorId).HasMaxLength(40);
            e.HasIndex(x => new { x.AccountId, x.At });
        });
        b.Entity<Message>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Thread).HasMaxLength(90);
            e.Property(x => x.FromId).HasMaxLength(40);
            e.Property(x => x.Body).HasMaxLength(Message.MaxLength);
            e.Property(x => x.Script).HasMaxLength(24);
            e.HasIndex(x => new { x.Thread, x.At });
            e.HasIndex(x => x.FromId);
        });
        b.Entity<ThreadRead>(e =>
        {
            e.HasKey(x => new { x.AccountId, x.Thread });
            e.Property(x => x.AccountId).HasMaxLength(40);
            e.Property(x => x.Thread).HasMaxLength(90);
        });
        b.Entity<Block>(e =>
        {
            e.HasKey(x => new { x.FromId, x.ToId });
            e.Property(x => x.FromId).HasMaxLength(40);
            e.Property(x => x.ToId).HasMaxLength(40);
            e.HasIndex(x => x.ToId);
        });
        b.Entity<Report>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FromId).HasMaxLength(40);
            e.Property(x => x.AboutId).HasMaxLength(40);
            e.Property(x => x.Thread).HasMaxLength(90);
            e.Property(x => x.Reason).HasMaxLength(16);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.HasIndex(x => x.AboutId);
        });
        b.Entity<SavedHome>(e =>
        {
            e.HasKey(x => new { x.AccountId, x.HomeId });
            e.Property(x => x.AccountId).HasMaxLength(40);
            e.Property(x => x.HomeId).HasMaxLength(80);
        });
        b.Entity<ContactMessage>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.AccountId).HasMaxLength(40);
            e.Property(x => x.Name).HasMaxLength(ContactMessage.NameMax);
            e.Property(x => x.Email).HasMaxLength(254);
            e.Property(x => x.Topic).HasMaxLength(16);
            e.Property(x => x.Body).HasMaxLength(ContactMessage.BodyMax);
            e.Property(x => x.Lang).HasMaxLength(2);
        });
        b.Entity<Candidate>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.HomeId).HasMaxLength(80);
            e.Property(x => x.AddedById).HasMaxLength(40);
            e.HasIndex(x => new { x.GroupId, x.HomeId }).IsUnique();
            e.HasOne<Group>().WithMany().HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Votes).WithOne().HasForeignKey(v => v.CandidateId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<CandidateVote>(e =>
        {
            e.HasKey(x => new { x.CandidateId, x.AccountId, x.Stage });
            e.Property(x => x.AccountId).HasMaxLength(40);
            e.Property(x => x.Choice).HasMaxLength(8);
            e.Property(x => x.Reason).HasMaxLength(12);
            e.HasIndex(x => x.AccountId);
        });
        b.Entity<DemoState>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
        });
        b.Entity<Home>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(80);
            e.Property(x => x.Source).HasMaxLength(24);
            e.Property(x => x.Agency).HasMaxLength(120);
            e.Property(x => x.Url).HasMaxLength(400);
            e.Property(x => x.Type).HasMaxLength(8);
            e.Property(x => x.Province).HasMaxLength(60);
            e.Property(x => x.Municipality).HasMaxLength(80);
            e.Property(x => x.Postcode).HasMaxLength(8);
            e.Property(x => x.District).HasMaxLength(80);
            e.Property(x => x.Area).HasMaxLength(12);
            e.Property(x => x.Features).HasMaxLength(120);
            e.Property(x => x.Energy).HasMaxLength(4);
            e.HasIndex(x => new { x.Source, x.GoneAt });
            e.HasIndex(x => x.Area);
            e.Ignore(x => x.IsDemo);
        });
    }
}
