using Nito.AsyncEx;
using Nito.AsyncEx.Testing;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace UnitTests
{
    public class AsyncSemaphoreUnitTests
    {
        [Fact]
        public async Task WaitAsync_NoSlotsAvailable_IsNotCompleted()
        {
            var semaphore = new AsyncSemaphore(0);
            Assert.Equal(0, semaphore.CurrentCount);
            var task = semaphore.WaitAsync();
            Assert.Equal(0, semaphore.CurrentCount);
            await AsyncAssert.NeverCompletesAsync(task);
        }

        [Fact]
        public async Task WaitAsync_SlotAvailable_IsCompleted()
        {
            var semaphore = new AsyncSemaphore(1);
            Assert.Equal(1, semaphore.CurrentCount);
            var task1 = semaphore.WaitAsync();
            Assert.Equal(0, semaphore.CurrentCount);
            Assert.True(task1.IsCompleted);
            var task2 = semaphore.WaitAsync();
            Assert.Equal(0, semaphore.CurrentCount);
            await AsyncAssert.NeverCompletesAsync(task2);
        }

        [Fact]
        public void WaitAsync_PreCancelled_SlotAvailable_SucceedsSynchronously()
        {
            var semaphore = new AsyncSemaphore(1);
            Assert.Equal(1, semaphore.CurrentCount);
            var token = new CancellationToken(true);

            var task = semaphore.WaitAsync(token);

            Assert.Equal(0, semaphore.CurrentCount);
            Assert.True(task.IsCompleted);
            Assert.False(task.IsCanceled);
            Assert.False(task.IsFaulted);
        }

        [Fact]
        public void WaitAsync_PreCancelled_NoSlotAvailable_CancelsSynchronously()
        {
            var semaphore = new AsyncSemaphore(0);
            Assert.Equal(0, semaphore.CurrentCount);
            var token = new CancellationToken(true);

            var task = semaphore.WaitAsync(token);

            Assert.Equal(0, semaphore.CurrentCount);
            Assert.True(task.IsCompleted);
            Assert.True(task.IsCanceled);
            Assert.False(task.IsFaulted);
        }

        [Fact]
        public async Task WaitAsync_Cancelled_DoesNotTakeSlot()
        {
            var semaphore = new AsyncSemaphore(0);
            Assert.Equal(0, semaphore.CurrentCount);
            var cts = new CancellationTokenSource();
            var task = semaphore.WaitAsync(cts.Token);
            Assert.Equal(0, semaphore.CurrentCount);
            Assert.False(task.IsCompleted);

            cts.Cancel();

            try { await task; }
            catch (OperationCanceledException) { }
            semaphore.Release();
            Assert.Equal(1, semaphore.CurrentCount);
            Assert.True(task.IsCanceled);
        }

        [Fact]
        public void Release_WithoutWaiters_IncrementsCount()
        {
            var semaphore = new AsyncSemaphore(0);
            Assert.Equal(0, semaphore.CurrentCount);
            semaphore.Release();
            Assert.Equal(1, semaphore.CurrentCount);
            var task = semaphore.WaitAsync();
            Assert.Equal(0, semaphore.CurrentCount);
            Assert.True(task.IsCompleted);
        }

        [Fact]
        public async Task Release_WithWaiters_ReleasesWaiters()
        {
            var semaphore = new AsyncSemaphore(0);
            Assert.Equal(0, semaphore.CurrentCount);
            var task = semaphore.WaitAsync();
            Assert.Equal(0, semaphore.CurrentCount);
            Assert.False(task.IsCompleted);
            semaphore.Release();
            Assert.Equal(0, semaphore.CurrentCount);
            await task;
        }

        [Fact]
        public void Release_Overflow_ThrowsException()
        {
            var semaphore = new AsyncSemaphore(long.MaxValue);
            Assert.Equal(long.MaxValue, semaphore.CurrentCount);
            AsyncAssert.Throws<OverflowException>(() => semaphore.Release());
        }

        [Fact]
        public void Release_ZeroSlots_HasNoEffect()
        {
            var semaphore = new AsyncSemaphore(1);
            Assert.Equal(1, semaphore.CurrentCount);
            semaphore.Release(0);
            Assert.Equal(1, semaphore.CurrentCount);
        }

        [Fact]
        public void Id_IsNotZero()
        {
            var semaphore = new AsyncSemaphore(0);
            Assert.NotEqual(0, semaphore.Id);
        }

        [Fact]
        public async void LockAsync_SlotAvailable()
        {
            var semaphore = new AsyncSemaphore(1);
            using (await semaphore.LockAsync())
            {
                Assert.Equal(0, semaphore.CurrentCount);
            }
            Assert.Equal(1, semaphore.CurrentCount);
        }

        [Fact]
        public async Task LockAsync_PreCancelled_SlotAvailable_SucceedsSynchronously()
        {
            var semaphore = new AsyncSemaphore(1);
            Assert.Equal(1, semaphore.CurrentCount);
            var token = new CancellationToken(true);

            var ad = semaphore.LockAsync(token);
            using (await ad)
            {
                Assert.Equal(0, semaphore.CurrentCount);
                Assert.True(ad.AsTask().IsCompleted);
                Assert.False(ad.AsTask().IsCanceled);
                Assert.False(ad.AsTask().IsFaulted);
            }
            Assert.Equal(1, semaphore.CurrentCount);
        }

        [Fact]
        public async Task LockAsync_PreCancelled_NoSlotAvailable_CancelsSynchronously()
        {
            var semaphore = new AsyncSemaphore(0);
            Assert.Equal(0, semaphore.CurrentCount);
            var token = new CancellationToken(true);

            var ad = semaphore.LockAsync(token);
            await AsyncAssert.CancelsAsync(ad);

            Assert.Equal(0, semaphore.CurrentCount);
            Assert.True(ad.AsTask().IsCompleted);
            Assert.True(ad.AsTask().IsCanceled);
            Assert.False(ad.AsTask().IsFaulted);
        }

        [Fact]
        public async Task LockAsync_Cancelled_DoesNotTakeSlot()
        {
            var semaphore = new AsyncSemaphore(0);
            Assert.Equal(0, semaphore.CurrentCount);
            var cts = new CancellationTokenSource();
            var ad = semaphore.LockAsync(cts.Token);
            Assert.Equal(0, semaphore.CurrentCount);
            Assert.False(ad.AsTask().IsCompleted);

            cts.Cancel();

            await AsyncAssert.CancelsAsync(ad);
            semaphore.Release();
            Assert.Equal(1, semaphore.CurrentCount);
            Assert.True(ad.AsTask().IsCanceled);
        }
    }
}
