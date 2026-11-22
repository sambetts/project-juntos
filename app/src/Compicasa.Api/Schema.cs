using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Reflection;

namespace Compicasa.Api;

/// <summary>
/// How the database schema is created and changed (D-034). Azure SQL uses EF Core migrations, applied at
/// start-up. SQLite (a developer's machine, tests) creates the schema straight from the model; after a schema
/// change, delete the local compicasa.db. A test fails if the model has changes no migration covers.
/// </summary>
public static class Schema
{
    public static async Task PrepareAsync(AppDb db, ILogger log, CancellationToken ct = default)
    {
        if (!db.Database.IsSqlServer())
        {
            await db.Database.EnsureCreatedAsync(ct);
            return;
        }
        await BaselineAsync(db, log, ct);
        var pending = (await db.Database.GetPendingMigrationsAsync(ct)).ToList();
        if (pending.Count > 0) log.LogInformation("Applying migrations: {Migrations}", string.Join(", ", pending));
        await db.Database.MigrateAsync(ct);
    }

    /// <summary>
    /// The live database was made by EnsureCreated, so its tables exist but no migration history does.
    /// Record the first migration as applied, once, so Migrate doesn't try to create the tables again.
    /// </summary>
    static async Task BaselineAsync(AppDb db, ILogger log, CancellationToken ct)
    {
        // The first migration is the schema as Session 12 left it, created then without migrations.
        var baseline = db.Database.GetMigrations().First();
        var product = (typeof(DbContext).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "10.0.0").Split('+')[0];
        var marked = await db.Database.ExecuteSqlAsync($"""
            IF OBJECT_ID(N'[Accounts]') IS NOT NULL AND OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
            BEGIN
                CREATE TABLE [__EFMigrationsHistory] ([MigrationId] nvarchar(150) NOT NULL, [ProductVersion] nvarchar(32) NOT NULL,
                    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId]));
                INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ({baseline}, {product});
            END
            """, ct);
        if (marked > 0) log.LogInformation("Baselined the existing database at {Migration}", baseline);
    }
}

/// <summary>Lets `dotnet ef migrations add` build the model for Azure SQL without a real connection.</summary>
public sealed class DesignTimeDb : IDesignTimeDbContextFactory<AppDb>
{
    public AppDb CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AppDb>().UseSqlServer("Server=design-time;Database=compicasa").Options);
}
