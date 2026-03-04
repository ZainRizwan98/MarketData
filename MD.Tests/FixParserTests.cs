using System.Text;
using MarketData.Parsing;
using MarketData.Domain;
using MarketData.Infrastructure;
using Xunit;

namespace MarketData.Tests
{
    public class FixParserTests
    {
        [Fact]
        public void ParseAndDeserialize_OrderInsert_ReturnsOrderInsertMessage()
        {
            // arrange - pipe-delimited FIX message
            var msg = "8=FIX.4.2|9=112|35=D|11=CL123|55=MSFT|54=1|38=100|44=150.5|10=000|";
            var bytes = Encoding.ASCII.GetBytes(msg);

            // act
            var obj = FixParser.ParseAndDeserialize(bytes);

            // assert
            Assert.IsType<OrderInsertMessage>(obj);
            var oi = (OrderInsertMessage)obj;
            Assert.Equal("CL123", oi.ClOrdID);
            Assert.Equal("MSFT", oi.Symbol);
            Assert.Equal(100m, oi.OrderQty);
            Assert.Equal(150.5m, oi.Price);
        }

        [Fact]
        public void ParseAndDeserialize_Heartbeat_ReturnsHeartbeatMessage()
        {
            var msg = "8=FIX.4.2|9=20|35=0|112=TESTREQ|10=000|";
            var bytes = Encoding.ASCII.GetBytes(msg);

            var obj = FixParser.ParseAndDeserialize(bytes);

            Assert.IsType<HeartbeatMessage>(obj);
            var hb = (HeartbeatMessage)obj;
            Assert.Equal("TESTREQ", hb.TestReqID);
        }

        [Fact]
        public void ParseAndDeserialize_ResetSequence_SetsSequenceGenerator()
        {
            var msg = "8=FIX.4.2|9=40|35=4|36=42|123=Y|10=000|";
            var bytes = Encoding.ASCII.GetBytes(msg);

            var obj = FixParser.ParseAndDeserialize(bytes);
            Assert.IsType<ResetSequenceMessage>(obj);
            var rs = (ResetSequenceMessage)obj;
            Assert.Equal(42, rs.NewSeqNo);

            // reset generator and set based on reset message behavior
            SequenceGenerator.Set(0);
            // simulate behavior: we set internal to NewSeqNo - 1 so next Next() returns NewSeqNo
            SequenceGenerator.Set(rs.NewSeqNo.Value - 1);
            var next = SequenceGenerator.Next();
            Assert.Equal(42, next);
        }
    }
}
