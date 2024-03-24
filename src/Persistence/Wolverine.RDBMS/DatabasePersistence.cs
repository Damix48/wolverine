using System.Data.Common;
using JasperFx.Core;
using JasperFx.Core.Reflection;
using Wolverine.Persistence.Durability;
using Wolverine.Runtime.Serialization;
using DbCommandBuilder = Weasel.Core.DbCommandBuilder;

namespace Wolverine.RDBMS;

public static class DatabasePersistence
{
    public static DbCommand BuildOutgoingStorageCommand(Envelope envelope, int ownerId,
        IMessageDatabase database)
    {
        var builder = database.ToCommandBuilder();

        var owner = builder.AddNamedParameter("owner", ownerId);
        ConfigureOutgoingCommand(database, builder, envelope, owner);
        return builder.Compile();
    }

    public static DbCommand BuildOutgoingStorageCommand(Envelope[] envelopes, int ownerId,
        IMessageDatabase database)
    {
        var builder = database.ToCommandBuilder();

        var owner = builder.AddNamedParameter("owner", ownerId);

        foreach (var envelope in envelopes) ConfigureOutgoingCommand(database, builder, envelope, owner);

        return builder.Compile();
    }

    private static void ConfigureOutgoingCommand(IMessageDatabase settings, DbCommandBuilder builder, Envelope envelope,
        DbParameter owner)
    {
        var list = new List<DbParameter>
        {
            builder.AddParameter(EnvelopeSerializer.Serialize(envelope)),
            builder.AddParameter(envelope.Id),
            owner,
            builder.AddParameter(envelope.Destination!.ToString()),
            builder.AddParameter(envelope.DeliverBy),
            builder.AddParameter(envelope.Attempts),
            builder.AddParameter(envelope.MessageType)
        };

        var parameterList = list.Select(x => $"@{x.ParameterName}").Join(", ");

        builder.Append(
            $"insert into {settings.SchemaName}.{DatabaseConstants.OutgoingTable} ({DatabaseConstants.OutgoingFields}) values ({parameterList});");
    }

    public static DbCommand BuildIncomingStorageCommand(IEnumerable<Envelope> envelopes,
        IMessageDatabase settings)
    {
        var builder = settings.ToCommandBuilder();

        foreach (var envelope in envelopes) BuildIncomingStorageCommand(settings, builder, envelope);

        return builder.Compile();
    }

    public static void BuildIncomingStorageCommand(IMessageDatabase settings, DbCommandBuilder builder,
        Envelope envelope)
    {
        var list = new List<DbParameter>
        {
            builder.AddParameter(EnvelopeSerializer.Serialize(envelope)),
            builder.AddParameter(envelope.Id),
            builder.AddParameter(envelope.Status.ToString()),
            builder.AddParameter(envelope.OwnerId),
            builder.AddParameter(envelope.ScheduledTime),
            builder.AddParameter(envelope.Attempts),
            builder.AddParameter(envelope.MessageType),
            builder.AddParameter(envelope.Destination?.ToString())
        };

        var parameterList = list.Select(x => $"@{x.ParameterName}").Join(", ");

        builder.Append(
            $@"insert into {settings.SchemaName}.{DatabaseConstants.IncomingTable}({DatabaseConstants.IncomingFields}) values ({parameterList});");
    }

    public static async Task<Envelope> ReadIncomingAsync(DbDataReader reader, CancellationToken cancellation = default)
    {
        var body = await reader.GetFieldValueAsync<byte[]>(0, cancellation);
        var envelope = EnvelopeSerializer.Deserialize(body);
        envelope.Status = Enum.Parse<EnvelopeStatus>(await reader.GetFieldValueAsync<string>(2, cancellation));
        envelope.OwnerId = await reader.GetFieldValueAsync<int>(3, cancellation);

        if (!await reader.IsDBNullAsync(4, cancellation))
        {
            envelope.ScheduledTime = await reader.GetFieldValueAsync<DateTimeOffset>(4, cancellation);
        }

        envelope.Attempts = await reader.GetFieldValueAsync<int>(5, cancellation);

        return envelope;
    }

    public static async Task<DeadLetterEnvelope> ReadDeadLetterAsync(DbDataReader reader, CancellationToken cancellation = default)
    {
        var id = await reader.GetFieldValueAsync<Guid>(0, cancellation);
        var envelope = EnvelopeSerializer.Deserialize(await reader.GetFieldValueAsync<byte[]>(2, cancellation));

        var exceptionType = await reader.GetFieldValueAsync<string>(6, cancellation);
        var exceptionMessage = await reader.GetFieldValueAsync<string>(7, cancellation);

        return new (id, envelope, exceptionType, exceptionMessage);
    }

    public static void ConfigureDeadLetterCommands(Envelope envelope, Exception? exception, DbCommandBuilder builder,
        IMessageDatabase wolverineDatabase)
    {
        var list = new List<DbParameter>
        {
            builder.AddParameter(envelope.Id),
            builder.AddParameter(envelope.ScheduledTime),
            builder.AddParameter(EnvelopeSerializer.Serialize(envelope)),
            builder.AddParameter(envelope.MessageType),
            builder.AddParameter(envelope.Destination?.ToString()),
            builder.AddParameter(envelope.Source),
            builder.AddParameter(exception?.GetType().FullNameInCode()),
            builder.AddParameter(exception?.Message),
            builder.AddParameter(envelope.SentAt.ToUniversalTime()),
            builder.AddParameter(false)
        };

        var parameterList = list.Select(x => $"@{x.ParameterName}").Join(", ");

        builder.Append(
            $"insert into {wolverineDatabase.SchemaName}.{DatabaseConstants.DeadLetterTable} ({DatabaseConstants.DeadLetterFields}) values ({parameterList});");
    }

    public static async Task<Envelope> ReadOutgoingAsync(DbDataReader reader, CancellationToken cancellation = default)
    {
        var body = await reader.GetFieldValueAsync<byte[]>(0, cancellation);
        var envelope = EnvelopeSerializer.Deserialize(body);
        envelope.OwnerId = await reader.GetFieldValueAsync<int>(2, cancellation);

        if (!await reader.IsDBNullAsync(4, cancellation))
        {
            envelope.DeliverBy = await reader.GetFieldValueAsync<DateTimeOffset>(4, cancellation);
        }

        envelope.Attempts = await reader.GetFieldValueAsync<int>(5, cancellation);

        return envelope;
    }
}