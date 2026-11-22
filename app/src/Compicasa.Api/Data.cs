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
    public string ProfileJson { get; set; } = "{}";

    public bool IsDemo => Kind != AccountKind.User;
    /// <summary>Visible to other people: made-up profiles always; real people once onboarded.</summary>
    public bool IsListed => IsDemo || OnboardedAt is not null;

    public Profile ReadProfile()
    {
        var p = JsonSerializer.Deserialize<Profile>(ProfileJson, Seeds.Json) ?? new Profile();
        p.Id = Id;
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
    public List<GroupMember> Members { get; set; } = [];
}

public static class MemberStatus
{
    public const string Pending = "pending";
    public const string Accepted = "accepted";
    public const string Declined = "declined";
    public const string Left = "left";
}

public sealed class GroupMember
{
    public Guid GroupId { get; set; }
    public string AccountId { get; set; } = "";
    /// <summary>Order in the group, proposer first.</summary>
    public int Position { get; set; }
    public string Status { get; set; } = MemberStatus.Pending;
    public DateTimeOffset? RespondedAt { get; set; }
}

/// <summary>One row: which day's made-up batch is loaded (D-029).</summary>
public sealed class DemoState
{
    public int Id { get; set; } = 1;
    public DateOnly BatchDate { get; set; }
    public DateTimeOffset RefreshedAt { get; set; }
}

public sealed class AppDb(DbContextOptions<AppDb> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Reaction> Reactions => Set<Reaction>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<DemoState> DemoStates => Set<DemoState>();

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
            e.HasIndex(x => x.AccountId);
        });
        b.Entity<DemoState>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
        });
    }
}
