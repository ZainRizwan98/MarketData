using System.Text;
using MarketData.Infrastructure;
using MarketData.Domain;
using Xunit;

namespace MarketData.Tests
{
    public class ResendRequestTests
    {
        [Fact]
        public void ResendRequest_RetrieveRange_ReturnsExpectedMessages()
        {
            // arrange
            var store = new InMemoryMessageStore();

            // reset global sequence generator
            SequenceGenerator.Set(0);

            // send/store 15 order messages
            for (int i = 1; i <= 15; i++)
            {
                var msg = $"35=D|11=CL{i}|55=SYM|38={i * 10}|44=100.0|10=000|";
                var bytes = Encoding.ASCII.GetBytes(msg);
                var seq = SequenceGenerator.Next();
                store.Add(seq, bytes);
            }

            // act: request resend for 3..12
            long begin = 3;
            long end = 12;
            var range = store.GetRange(begin, end);

            // assert: we should have end-begin+1 messages and correct sequence numbers
            var list = System.Linq.Enumerable.ToList(range);
            Assert.Equal(10, list.Count);

            for (int i = 0; i < list.Count; i++)
            {
                var expectedSeq = begin + i;
                Assert.Equal(expectedSeq, list[i].SequenceNumber);

                // verify payload decodes to expected ClOrdID
                var payload = Encoding.ASCII.GetString(list[i].RawMessage);
                Assert.Contains($"11=CL{expectedSeq}|", payload);
            }
        }
    }
}
