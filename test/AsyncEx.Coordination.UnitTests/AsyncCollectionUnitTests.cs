using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nito.AsyncEx;
using System.Linq;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using Xunit;
using Nito.AsyncEx.Testing;
using System.Collections.Concurrent;

namespace UnitTests
{
    public class AsyncCollectionUnitTests
    {
        [Fact]
        public void ConstructorWithZeroMaxCount_Throws()
        {
            AsyncAssert.Throws<ArgumentOutOfRangeException>(() => new AsyncCollection<int>(0));
        }

        [Fact]
        public void ConstructorWithZeroMaxCountAndCollection_Throws()
        {
            AsyncAssert.Throws<ArgumentOutOfRangeException>(() => new AsyncCollection<int>(new ConcurrentQueue<int>(new int[] {}), 0));
        }

        [Fact]
        public void ConstructorWithMaxCountSmallerThanCollectionCount_Throws()
        {
            AsyncAssert.Throws<ArgumentException>(() => new AsyncCollection<int>(new ConcurrentQueue<int>(new[] { 3, 5 }), 1));
        }

        [Fact]
        public async Task ConstructorWithCollection_AddsItems()
        {
            var queue = new AsyncCollection<int>(new ConcurrentQueue<int>(new[] { 3, 5, 7 }));

            var result1 = await queue.TakeAsync();
            var result2 = await queue.TakeAsync();
            var result3 = await queue.TakeAsync();

            Assert.Equal(3, result1);
            Assert.Equal(5, result2);
            Assert.Equal(7, result3);
        }

        [Fact]
        public async Task AddAsync_SpaceAvailable_AddsItem()
        {
            var queue = new AsyncCollection<int>();

            await queue.AddAsync(3);
            var result = await queue.TakeAsync();

            Assert.Equal(3, result);
        }

        [Fact]
        public async Task AddAsync_CompleteAdding_ThrowsException()
        {
            var queue = new AsyncCollection<int>();
            queue.CompleteAdding();

            await AsyncAssert.ThrowsAsync<InvalidOperationException>(() => queue.AddAsync(3));
        }

        [Fact]
        public async Task TakeAsync_EmptyAndComplete_ThrowsException()
        {
            var queue = new AsyncCollection<int>();
            queue.CompleteAdding();

            await AsyncAssert.ThrowsAsync<InvalidOperationException>(() => queue.TakeAsync());
        }

        [Fact]
        public async Task TakeAsync_Empty_DoesNotComplete()
        {
            var queue = new AsyncCollection<int>();

            var task = queue.TakeAsync();

            await AsyncAssert.NeverCompletesAsync(task);
        }

        [Fact]
        public async Task TakeAsync_Empty_ItemAdded_Completes()
        {
            var queue = new AsyncCollection<int>();
            var task = queue.TakeAsync();

            await queue.AddAsync(13);
            var result = await task;

            Assert.Equal(13, result);
        }

        [Fact]
        public async Task TakeAsync_Cancelled_Throws()
        {
            var queue = new AsyncCollection<int>();
            var cts = new CancellationTokenSource();
            var task = queue.TakeAsync(cts.Token);

            cts.Cancel();

            await AsyncAssert.ThrowsAsync<OperationCanceledException>(() => task);
        }

        [Fact]
        public async Task AddAsync_Full_DoesNotComplete()
        {
            var queue = new AsyncCollection<int>(new ConcurrentQueue<int>(new[] { 13 }), 1);

            var task = queue.AddAsync(7);

            await AsyncAssert.NeverCompletesAsync(task);
        }

        [Fact]
        public async Task AddAsync_SpaceAvailable_Completes()
        {
            var queue = new AsyncCollection<int>(new ConcurrentQueue<int>(new[] { 13 }), 1);
            var task = queue.AddAsync(7);

            await queue.TakeAsync();

            await task;
        }

        [Fact]
        public async Task AddAsync_Cancelled_Throws()
        {
            var queue = new AsyncCollection<int>(new ConcurrentQueue<int>(new[] { 13 }), 1);
            var cts = new CancellationTokenSource();
            var task = queue.AddAsync(7, cts.Token);

            cts.Cancel();

            await AsyncAssert.ThrowsAsync<OperationCanceledException>(() => task);
        }

        [Fact]
        public void CompleteAdding_MultipleTimes_DoesNotThrow()
        {
            var queue = new AsyncCollection<int>();
            queue.CompleteAdding();

            queue.CompleteAdding();
        }

        [Fact]
        public async Task OutputAvailableAsync_NoItemsInQueue_IsNotCompleted()
        {
            var queue = new AsyncCollection<int>();

            var task = queue.OutputAvailableAsync();

            await AsyncAssert.NeverCompletesAsync(task);
        }

        [Fact]
        public async Task OutputAvailableAsync_ItemInQueue_ReturnsTrue()
        {
            var queue = new AsyncCollection<int>();
            queue.Add(13);

            var result = await queue.OutputAvailableAsync();
            Assert.True(result);
        }

        [Fact]
        public async Task OutputAvailableAsync_NoItemsAndCompleted_ReturnsFalse()
        {
            var queue = new AsyncCollection<int>();
            queue.CompleteAdding();

            var result = await queue.OutputAvailableAsync();
            Assert.False(result);
        }

        [Fact]
        public async Task OutputAvailableAsync_ItemInQueueAndCompleted_ReturnsTrue()
        {
            var queue = new AsyncCollection<int>();
            queue.Add(13);
            queue.CompleteAdding();

            var result = await queue.OutputAvailableAsync();
            Assert.True(result);
        }

        [Fact]
        public async Task StandardAsyncSingleConsumerCode()
        {
            var queue = new AsyncCollection<int>();
            var producer = Task.Run(() =>
            {
                queue.Add(3);
                queue.Add(13);
                queue.Add(17);
                queue.CompleteAdding();
            });

            var results = new List<int>();
            while (await queue.OutputAvailableAsync())
            {
                results.Add(queue.Take());
            }

            Assert.Equal(3, results.Count);
            Assert.Equal(3, results[0]);
            Assert.Equal(13, results[1]);
            Assert.Equal(17, results[2]);
        }

#if !NETCOREAPP2_2
        [Fact]
        public async Task IAsyncEnumerable()
        {
            var queue = new AsyncCollection<int>();
            var producer = Task.Run(() =>
            {
                queue.Add(3);
                queue.Add(13);
                queue.Add(17);
                queue.CompleteAdding();
            });

            var results = new List<int>();
            await foreach(var value in queue)
            {
                results.Add(value);
            }

            Assert.Equal(3, results.Count);
            Assert.Equal(3, results[0]);
            Assert.Equal(13, results[1]);
            Assert.Equal(17, results[2]);
        }
#endif
    }
}
