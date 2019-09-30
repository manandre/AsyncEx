using Nito.AsyncEx;
using Moq;
using Moq.Protected;
using System.Threading;
using Xunit;
using System.Linq.Expressions;
using System;
using System.IO;
using System.Threading.Tasks;

namespace UnitTests
{
    public class AsyncStreamTest
    {
        [Fact]
        public void AS_Read()
        {
            var sut = new Mock<AsyncStream>() { CallBase = true };
            var expected = 42;
            sut.Protected().Setup<Task<int>>("DoReadAsync", ItExpr.IsAny<byte[]>(), ItExpr.IsAny<int>(), ItExpr.IsAny<int>(), ItExpr.IsAny<CancellationToken>(), ItExpr.IsAny<bool>())
            .ReturnsAsync(expected);
            var read = sut.Object.Read(null, 0 , 0);
            sut.Protected().Verify("DoReadAsync", Times.Once(), ItExpr.IsAny<byte[]>(), ItExpr.IsAny<int>(), ItExpr.IsAny<int>(), ItExpr.IsAny<CancellationToken>(), true);
            Assert.Equal(expected, read);

#if !NETSTANDARD1_3
            read = sut.Object.EndRead(sut.Object.BeginRead(null, 0, 0, null, null));
            sut.Protected().Verify("DoReadAsync", Times.Once(), ItExpr.IsAny<byte[]>(), ItExpr.IsAny<int>(), ItExpr.IsAny<int>(), ItExpr.IsAny<CancellationToken>(), false);
            Assert.Equal(expected, read);
#endif
        }

        [Fact]
        public void AS_Write()
        {
            var sut = new Mock<AsyncStream>() { CallBase = true };
            sut.Object.Write(null, 0 , 0);
            sut.Protected().Verify("DoWriteAsync", Times.Once(), ItExpr.IsAny<byte[]>(), ItExpr.IsAny<int>(), ItExpr.IsAny<int>(), ItExpr.IsAny<CancellationToken>(), true);

#if !NETSTANDARD1_3
            sut.Object.EndWrite(sut.Object.BeginWrite(null, 0, 0, null, null));
            sut.Protected().Verify("DoWriteAsync", Times.Once(), ItExpr.IsAny<byte[]>(), ItExpr.IsAny<int>(), ItExpr.IsAny<int>(), ItExpr.IsAny<CancellationToken>(), false);
#endif
        }

        [Fact]
        public void AS_Flush()
        {
            var sut = new Mock<AsyncStream>() { CallBase = true };
            sut.Object.Flush();
            sut.Protected().Verify<Task>("DoFlushAsync", Times.Once(), ItExpr.IsAny<CancellationToken>(), true);
        }

        [Fact]
        public void AS_Dispose()
        {
            var sut = new Mock<AsyncStream>() { CallBase = true };
            sut.Object.Dispose();
            sut.Object.Dispose();
        }

        [Fact]
        public void AS_Synchronized()
        {
            Assert.Throws<ArgumentNullException>(() => AsyncStream.Synchronized(null));
            var sut = new Mock<AsyncStream>() { CallBase = true };
            var synchronized = sut.Object.Synchronized();
            Assert.NotNull(synchronized);
            Assert.Same(synchronized, AsyncStream.Synchronized(synchronized));

            // Caps
            var funcs = new Expression<Func<AsyncStream, bool>>[]
            {
                x => x.CanRead,
                x => x.CanWrite,
                x => x.CanSeek,
                x => x.CanTimeout
            };
            foreach (var func in funcs)
            {
                foreach (var expected in new[] { false, true })
                {
                    sut.Setup(func).Returns(expected);
                    Assert.Equal(expected, func.Compile()(synchronized));
                }
            }

            // Position
            var expected2 = 42;
            sut.Setup(x => x.Position).Returns(expected2);
            Assert.Equal(expected2, synchronized.Position);

            sut.SetupProperty(x => x.Position);
            synchronized.Position = expected2;
            Assert.Equal(expected2, sut.Object.Position);

            // ReadTimeout
            sut.Setup(x => x.ReadTimeout).Returns(expected2);
            Assert.Equal(expected2, synchronized.ReadTimeout);

            sut.SetupProperty(x => x.ReadTimeout);
            synchronized.ReadTimeout = expected2;
            Assert.Equal(expected2, sut.Object.ReadTimeout);

            // WriteTimeout
            sut.Setup(x => x.WriteTimeout).Returns(expected2);
            Assert.Equal(expected2, synchronized.WriteTimeout);

            sut.SetupProperty(x => x.WriteTimeout);
            synchronized.WriteTimeout = expected2;
            Assert.Equal(expected2, sut.Object.WriteTimeout);

            // Seek
            var expected_offset = 42;
            var expected_origin = SeekOrigin.End;
            var result_offset = 0L;
            var result_origin = SeekOrigin.Begin;
            sut.Setup(x => x.Seek(It.IsAny<long>(), It.IsAny<SeekOrigin>()))
               .Callback<long, SeekOrigin>((offset, origin) =>
               {
                   result_offset = offset;
                   result_origin = origin;
               });
            synchronized.Seek(expected_offset, expected_origin);
            Assert.Equal(expected_offset, result_offset);
            Assert.Equal(expected_origin, result_origin);

            // Length
            expected2 = 42;
            sut.Setup(x => x.Length).Returns(expected2);
            Assert.Equal(expected2, synchronized.Length);

            // SetLength
            var expected_length = 42;
            var result_length = 0L;
            sut.Setup(x => x.SetLength(It.IsAny<long>()))
               .Callback<long>(length => result_length = length);
            synchronized.SetLength(expected_length);
            Assert.Equal(expected_length, result_length);

            // Read
            sut.Protected().Setup<Task<int>>("DoReadAsync", ItExpr.IsNull<byte[]>(), ItExpr.IsAny<int>(), ItExpr.IsAny<int>(), ItExpr.IsAny<CancellationToken>(), ItExpr.IsAny<bool>())
            .ReturnsAsync(expected2);
            var read = synchronized.ReadAsync(null, 0, 0).Result;
            Assert.Equal(expected2, read);
            read = synchronized.Read(null, 0, 0);
            Assert.Equal(expected2, read);
            read = synchronized.EndRead(synchronized.BeginRead(null, 0, 0, null, null));
            Assert.Equal(expected2, read);
            Assert.ThrowsAsync<OperationCanceledException>(() => synchronized.ReadAsync(null, 0, 0, new CancellationToken(true)));

            // Write
            synchronized.WriteAsync(null, 0, 0).Wait();
            sut.Protected().Verify("DoWriteAsync", Times.Once(), ItExpr.IsNull<byte[]>(), ItExpr.IsAny<int>(), ItExpr.IsAny<int>(), ItExpr.IsAny<CancellationToken>(), false);
            synchronized.Write(null, 0, 0);
            sut.Protected().Verify("DoWriteAsync", Times.Once(), ItExpr.IsNull<byte[]>(), ItExpr.IsAny<int>(), ItExpr.IsAny<int>(), ItExpr.IsAny<CancellationToken>(), true);
            synchronized.EndWrite(synchronized.BeginWrite(null, 0, 0, null, null));
            sut.Protected().Verify("DoWriteAsync", Times.Exactly(2), ItExpr.IsNull<byte[]>(), ItExpr.IsAny<int>(), ItExpr.IsAny<int>(), ItExpr.IsAny<CancellationToken>(), false);
            Assert.ThrowsAsync<OperationCanceledException>(() => synchronized.WriteAsync(null, 0, 0, new CancellationToken(true)));

            // Flush
            synchronized.FlushAsync().Wait();
            sut.Protected().Verify<Task>("DoFlushAsync", Times.Once(), ItExpr.IsAny<CancellationToken>(), false);
            synchronized.Flush();
            sut.Protected().Verify<Task>("DoFlushAsync", Times.Once(), ItExpr.IsAny<CancellationToken>(), true);
            Assert.ThrowsAsync<OperationCanceledException>(() => synchronized.FlushAsync(new CancellationToken(true)));

            // Dispose
            synchronized.Dispose();
        }
    }
}