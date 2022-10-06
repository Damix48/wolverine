using System;
using System.Linq;
using System.Threading.Tasks;
using Wolverine.RDBMS;
using Wolverine.Persistence.Durability;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Wolverine.Runtime;

namespace Wolverine.EntityFrameworkCore;

// ReSharper disable once InconsistentNaming
internal class EfCoreEnvelopeTransaction : IEnvelopeTransaction
{
    private readonly DatabaseSettings _settings;

    public EfCoreEnvelopeTransaction(DbContext dbContext, MessageContext messaging)
    {
        if (messaging.Persistence is IDatabaseBackedEnvelopePersistence persistence)
        {
            _settings = persistence.DatabaseSettings;
        }
        else
        {
            throw new InvalidOperationException(
                "This Wolverine application is not using Database backed message persistence. Please configure the message configuration");
        }

        DbContext = dbContext;
    }

    public DbContext DbContext { get; }

    public async Task PersistAsync(Envelope envelope)
    {
        if (DbContext.Database.CurrentTransaction == null)
        {
            await DbContext.Database.BeginTransactionAsync();
        }

        var conn = DbContext.Database.GetDbConnection();
        var tx = DbContext.Database.CurrentTransaction!.GetDbTransaction();
        var cmd = DatabasePersistence.BuildOutgoingStorageCommand(envelope, envelope.OwnerId, _settings);
        cmd.Transaction = tx;
        cmd.Connection = conn;

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task PersistAsync(Envelope[] envelopes)
    {
        if (!envelopes.Any())
        {
            return;
        }

        if (DbContext.Database.CurrentTransaction == null)
        {
            await DbContext.Database.BeginTransactionAsync();
        }

        var conn = DbContext.Database.GetDbConnection();
        var tx = DbContext.Database.CurrentTransaction!.GetDbTransaction();
        var cmd = DatabasePersistence.BuildIncomingStorageCommand(envelopes, _settings);
        cmd.Transaction = tx;
        cmd.Connection = conn;

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ScheduleJobAsync(Envelope envelope)
    {
        if (DbContext.Database.CurrentTransaction == null)
        {
            await DbContext.Database.BeginTransactionAsync();
        }

        var conn = DbContext.Database.GetDbConnection();
        var tx = DbContext.Database.CurrentTransaction!.GetDbTransaction();
        var builder = _settings.ToCommandBuilder();
        DatabasePersistence.BuildIncomingStorageCommand(_settings, builder, envelope);
        await builder.ExecuteNonQueryAsync(conn, tx: tx);
    }

    public Task CopyToAsync(IEnvelopeTransaction other)
    {
        throw new NotSupportedException();
    }

    public async ValueTask RollbackAsync()
    {
        if (DbContext.Database.CurrentTransaction != null)
        {
            await DbContext.Database.CurrentTransaction.RollbackAsync();
        }
    }
}
