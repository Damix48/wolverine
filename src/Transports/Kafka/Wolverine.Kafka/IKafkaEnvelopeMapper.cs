using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Wolverine.Transports;
using Wolverine.Util;

namespace Wolverine.Kafka;

public interface IKafkaEnvelopeMapper : IEnvelopeMapper<Message<string, string>, Message<string, string>>;

/// <summary>
/// Option to publish or receive raw JSON from or to Kafka topics
/// </summary>
internal class JsonOnlyMapper : IKafkaEnvelopeMapper
{
    private readonly JsonSerializerOptions _options;
    private readonly string? _messageTypeName;

    public JsonOnlyMapper(KafkaTopic topic, JsonSerializerOptions options)
    {
        _options = options;

        _messageTypeName = topic.MessageType?.ToMessageTypeName();
    }

    public void MapEnvelopeToOutgoing(Envelope envelope, Message<string, string> outgoing)
    {
        outgoing.Key = envelope.GroupId;

        if (envelope.Data != null && envelope.Data.Any())
        {
            outgoing.Value = Encoding.Default.GetString(envelope.Data);
        }
        else if (envelope.Message != null)
        {
            outgoing.Value = JsonSerializer.Serialize(envelope.Message, _options);
        }
        else
        {
            throw new InvalidOperationException(
                $"Envelope {envelope.Id} has neither {nameof(envelope.Data)} nor {nameof(Envelope.Message)}");
        }
    }

    public void MapIncomingToEnvelope(Envelope envelope, Message<string, string> incoming)
    {
        envelope.Data = Encoding.Default.GetBytes(incoming.Value);
        envelope.MessageType = _messageTypeName;
    }

    public IEnumerable<string> AllHeaders()
    {
        yield break;
    }
}