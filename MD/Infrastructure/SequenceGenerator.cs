using System.Threading;

namespace MarketData.Infrastructure;

public static class SequenceGenerator
{
    // starts at 0, first Next() returns 1
    private static long _counter = 0;

    public static long Next()
    {
        return Interlocked.Increment(ref _counter);
    }

    public static long Current => Interlocked.Read(ref _counter);

    public static void Set(long value)
    {
        // set internal counter to value in a thread-safe way
        Interlocked.Exchange(ref _counter, value);
    }
}
