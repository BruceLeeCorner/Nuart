namespace NKit.Uart.Test
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            Assert.True(Utility.GetBitValue(0b_1011_1101, 0));
            Assert.False(Utility.GetBitValue(0b_1011_1101, 1));
            Assert.True(Utility.GetBitValue(0b_1011_1101, 2));
            Assert.True(Utility.GetBitValue(0b_1011_1101, 3));
            Assert.True(Utility.GetBitValue(0b_1011_1101, 4));
            Assert.True(Utility.GetBitValue(0b_1011_1101, 5));
            Assert.False(Utility.GetBitValue(0b_1011_1101, 6));
            Assert.True(Utility.GetBitValue(0b_1011_1101, 7));
            Assert.Throws(typeof(ArgumentOutOfRangeException), () => Utility.GetBitValue(0b_1011_1101, -1));
            Assert.Throws(typeof(ArgumentOutOfRangeException), () => Utility.GetBitValue(0b_1011_1101, 8));
        }
    }
}