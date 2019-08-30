using Nito.AsyncEx;
using Moq;
using System.Threading;
using Xunit;

namespace UnitTests
{
    public class AsyncStreamTest
    {
        [Fact]
        public void AS_Read()
        {
            var sut = new Mock<AsyncStream>() { CallBase = true };
            var expected = 42;
            sut.Setup(x => x.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
            var read = sut.Object.Read(null, 0 , 0);
            Assert.Equal(expected, read);

#if NETSTANDARD2_0
            read = sut.Object.EndRead(sut.Object.BeginRead(null, 0, 0, null, null));
            Assert.Equal(expected, read);
#endif
        }

        [Fact]
        public void AS_Write()
        {
            var sut = new Mock<AsyncStream>() { CallBase = true };
            sut.Setup(x => x.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Verifiable();
            sut.Object.Write(null, 0 , 0);
            sut.Verify(x => x.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

#if NETSTANDARD2_0
            sut.Object.EndWrite(sut.Object.BeginWrite(null, 0, 0, null, null));
            sut.Verify(x => x.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
#endif
        }

        [Fact]
        public void AS_Flush()
        {
            var sut = new Mock<AsyncStream>() { CallBase = true };
            sut.Setup(x => x.FlushAsync(It.IsAny<CancellationToken>())).Verifiable();
            sut.Object.Flush();
            sut.Verify(x => x.FlushAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void AS_Dispose()
        {
            var sut = new Mock<AsyncStream>() { CallBase = true };
            sut.Object.Dispose();
            sut.Object.Dispose();
        }
    }
}