using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MarketData.Infrastructure;

public record StoredMessage(long SequenceNumber, object Payload, DateTime ReceivedAt);

public class InMemoryMessageStore
{
    private readonly ConcurrentDictionary<long, StoredMessage> _store = new();

    public void Add(long seq, object payload)
    {
        var msg = new StoredMessage(seq, payload, DateTime.UtcNow);
        _store[seq] = msg;
    }

    public bool TryGet(long seq, out StoredMessage? message)
    {
        if (_store.TryGetValue(seq, out var m)) { message = m; return true; }
        message = null; return false;
    }

    public IEnumerable<StoredMessage> GetRange(long beginSeq, long endSeq)
    {
        if (beginSeq <= 0) beginSeq = 1;
        if (endSeq <= 0) endSeq = long.MaxValue;

        return _store.Keys
            .Where(k => k >= beginSeq && k <= endSeq)
            .OrderBy(k => k)
            .Select(k => _store[k]);
    }

    public long MaxSequence => _store.IsEmpty ? 0 : _store.Keys.Max();

    public void Clear() => _store.Clear();
}
