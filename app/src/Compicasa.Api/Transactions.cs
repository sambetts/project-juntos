using Microsoft.EntityFrameworkCore;

namespace Compicasa.Api;

/// <summary>
/// Runs several database steps as one transaction (Session 32 code review). Azure SQL retries on transient errors
/// (Program.cs), and a retrying connection only allows a transaction inside its execution strategy, which may run the
/// work again from the start: so each attempt begins with nothing tracked, and the work must read what it changes.
/// </summary>
public static class Transactions
{
    public static Task RunAsync(AppDb db, Func<Task> work, CancellationToken ct = default) =>
        db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await work();
            await tx.CommitAsync(ct);
        });
}
