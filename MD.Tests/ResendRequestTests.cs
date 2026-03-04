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

            // send/store 15 order messages (deserialize and store objects)
            for (int i = 1; i <= 15; i++)
            {
                var msg = $"35=D|11=CL{i}|55=SYM|38={i * 10}|44=100.0|10=000|";
                var bytes = Encoding.ASCII.GetBytes(msg);
                var obj = MarketData.Parsing.FixParser.ParseAndDeserialize(bytes);
                var seq = SequenceGenerator.Next();
                store.Add(seq, obj!);
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

                // verify payload object contains expected ClOrdID
                var payloadObj = list[i].Payload;
                Assert.NotNull(payloadObj);
                // most likely OrderInsertMessage
                if (payloadObj is MarketData.Domain.OrderInsertMessage oi)
                {
                    Assert.Equal($"CL{expectedSeq}", oi.ClOrdID);
                }
                else
                {
                    // fallback: try to read Fields property
                    var prop = payloadObj.GetType().GetProperty("Fields");
                    Assert.NotNull(prop);
                    var dict = prop.GetValue(payloadObj) as System.Collections.Generic.IDictionary<string, string>;
                    Assert.NotNull(dict);
                    Assert.Equal($"CL{expectedSeq}", dict["11"]);
                }
            }
        }
    }
}
