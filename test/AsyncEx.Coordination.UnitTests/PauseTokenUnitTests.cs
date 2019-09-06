using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Nito.AsyncEx.Testing;
using Xunit;

namespace UnitTests
{
    public class PauseTokenUnitTests
    {
        [Fact]
        public void IsPaused()
        {
            var pts = new PauseTokenSource();
            Assert.NotNull(pts);
            Assert.False(pts.IsPaused);
            pts.IsPaused=true;
            Assert.True(pts.IsPaused);
            pts.IsPaused=false;
            Assert.False(pts.IsPaused);
        }

        [Fact]
        public void PauseToken()
        {
            var pts = new PauseTokenSource();
            var token = pts.Token;
            Assert.False(token.IsPaused);
            Assert.True(token.CanBePaused);
            pts.IsPaused=true;
            Assert.True(token.IsPaused);
        }

        [Fact]
        public void WaitWhilePausedAsync()
        {
            var pts = new PauseTokenSource();
            var token = pts.Token;
            Assert.True(token.WaitWhilePausedAsync().IsCompletedSuccessfully);
            pts.IsPaused=true;
            Assert.False(token.WaitWhilePausedAsync().IsCompletedSuccessfully);
            pts.IsPaused=false;
            Assert.True(token.WaitWhilePausedAsync().IsCompletedSuccessfully);
        }

        [Fact]
        public async Task WaitWhilePausedAsync_WithCancellationToken()
        {
            var pts = new PauseTokenSource();
            var cts = new CancellationTokenSource();
            var token = pts.Token;
            Assert.True(token.WaitWhilePausedAsync(cts.Token).IsCompletedSuccessfully);
            pts.IsPaused=true;
            Assert.False(token.WaitWhilePausedAsync(cts.Token).IsCompletedSuccessfully);
            pts.IsPaused=false;
            Assert.True(token.WaitWhilePausedAsync(cts.Token).IsCompletedSuccessfully);

            pts.IsPaused=true;
            var task = pts.Token.WaitWhilePausedAsync(cts.Token);
            Assert.False(task.IsCompleted);
            cts.Cancel();
            await AsyncAssert.CancelsAsync(task);

            var task2 = pts.Token.WaitWhilePausedAsync(cts.Token);
            Assert.True(task.IsCanceled);
        }
    }
}
