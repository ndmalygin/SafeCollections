using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using SafeCollections;
using Xunit;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

// ReSharper disable UnusedParameter.Local

namespace SafeCollections_UT
{
    /// <summary>
    ///     Unit tests for SafeList generic class.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public sealed class SafeList_UT
    {
        [Theory]
        [InlineData(100)]
        [InlineData(200)]
        [InlineData(-100)]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(123)]
        [InlineData(-33367)]
        [InlineData(-34)]
        [InlineData(int.MaxValue)]
        [InlineData(55)]
        public void AddItemTest(int item)
        {
            var safeList = new SafeList<int>(false);
            void Handler(object sender, CollectionEventArgs<int> args)
            {
                Assert.Equal(item, args.Items[0]);
                Assert.Equal(CollectionEventTypeEnum.Added, args.CollectionEventType);
            }
            safeList.SignOnEvents(Handler);
            safeList.AddItem(item);
            safeList.UnSignFromEvents(Handler);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(200)]
        [InlineData(-100)]
        [InlineData(-0)]
        [InlineData(-300)]
        [InlineData(-1)]
        [InlineData(-2)]
        [InlineData(-4)]
        [InlineData(int.MaxValue)]
        [InlineData(-1000000)]
        public void RemoveItemTest(int item)
        {
            var safeList = new SafeList<int>(false);
            safeList.AddItem(item);
            safeList.SignOnEvents(
                // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
                (sender, args) =>
                {
                    Assert.Equal(item, args.Items[0]);
                    Assert.Equal(CollectionEventTypeEnum.Removed, args.CollectionEventType);
                }
            );

            safeList.RemoveItems([item]);
        }

        [Theory]
        [InlineData(1000)]
        [InlineData(2000)]
        [InlineData(3000)]
        [InlineData(4000)]
        [InlineData(5000)]
        [InlineData(10000)]
        [InlineData(20000)]
        [InlineData(50000)]
        [InlineData(100000)]
        public async Task MultiThreadingTest(int taskCount)
        {
            var tasks = new List<Task>();
            var origList = new ConcurrentBag<TestObject>();
            var safeList = new SafeList<TestObject>(false);
            
            EventHandler<CollectionEventArgs<TestObject>> handler = (sender, args) =>
            {
                Assert.Contains(args.Items[0], origList);
                Assert.Equal(CollectionEventTypeEnum.Added, args.CollectionEventType);
            };
            
            safeList.SignOnEvents(
                handler
            );
            
            for (var i = 0; i < taskCount; i++)
            {
                tasks.Add(
                    new Task(() =>
                    {
                        var item = new TestObject(Random.Shared.Next());
                        
                        origList.Add(item);
                        safeList.AddItem(item);                        
                    })
                );
            }

            Parallel.ForEach(tasks, t => t.Start());
            await Task.WhenAll(tasks);

            Assert.Equal(taskCount, safeList.Length);

            var items = safeList.Items;
            foreach (var item in origList)
                Assert.Contains(item, items);
            
            safeList.UnSignFromEvents(handler);
            tasks.Clear();

            handler = (sender, args) =>
            {
                Assert.Contains(args.Items[0], origList);
                Assert.Equal(CollectionEventTypeEnum.Removed, args.CollectionEventType);
            };
            safeList.SignOnEvents(
                handler
            );
            
            var arr = origList.ToArray();
            for (var i = 0; i < taskCount; i++)
            {
                var i1 = i;
                tasks.Add(
                    new Task(() =>
                    {
                        safeList.RemoveItem(arr[i1]);                        
                    })
                );
            }

            Parallel.ForEach(tasks, t => t.Start());
            await Task.WhenAll(tasks);
            
            Assert.Empty(safeList.Items);
            Assert.Equal(0, safeList.Length);

            safeList.UnSignFromEvents(handler);
        }

        [Fact]
        public void AddItemsTest()
        {
            var safeList = new SafeList<int>(false);
            safeList.SignOnEvents(
                (sender, args) =>
                {
                    Assert.Equal([100, 200, 300], args.Items);
                    Assert.Equal(CollectionEventTypeEnum.Added, args.CollectionEventType);
                }
            );

            safeList.AddItems([100, 200, 300]);
        }

        [Fact]
        public void ClearAllTest()
        {
            var safeList = new SafeList<int>();
            safeList.AddItems([100, 200, 300]);
            safeList.SignOnEvents(
                (sender, args) =>
                {
                    switch (args.CollectionEventType)
                    {
                        case CollectionEventTypeEnum.None:
                            Assert.Equal([100, 200, 300], args.Items);
                            break;
                        default:
                            Assert.Null(args.Items);
                            Assert.Equal(CollectionEventTypeEnum.Cleared, args.CollectionEventType);
                            break;
                    }
                }
            );

            safeList.ClearAll();
        }

        [Fact]
        public void RemoveItemsTest()
        {
            var safeList = new SafeList<int>();
            safeList.AddItems([100, 200, 300]);
            safeList.SignOnEvents(
                (sender, args) =>
                {
                    switch (args.CollectionEventType)
                    {
                        case CollectionEventTypeEnum.None:
                            Assert.Equal([100, 200, 300], args.Items);
                            break;
                        default:
                            Assert.Equal([100, 300], args.Items);
                            Assert.Equal(CollectionEventTypeEnum.Removed, args.CollectionEventType);
                            break;
                    }
                }
            );

            safeList.RemoveItems([100, 300]);
        }
        
        [Fact]
        public void ReentrancyAndDeadlockPreventionTest()
        {
            // Arrange
            // Create a collection that sends its initial state upon subscription
            var safeList = new SafeList<int>(sendCollectionState: true);
            safeList.AddItem(42);

            var readLengthDuringEvent = -1;
            int[] itemsDuringEvent = null;
            var isHandlerCalledOnAdd = false;

            EventHandler<CollectionEventArgs<int>> handler = (sender, args) =>
            {
                // We simulate a reentrant call: trying to read from the collection 
                // while being inside the event handler that was triggered by the collection itself.
                if (args.CollectionEventType == CollectionEventTypeEnum.Added)
                {
                    isHandlerCalledOnAdd = true;
                    
                    // If the collection is holding a WriteLock here, these calls will cause a Deadlock 
                    // or throw a LockRecursionException because ReaderWriterLockSlim is not reentrant by default.
                    readLengthDuringEvent = safeList.Length;
                    itemsDuringEvent = safeList.Items;
                }
            };

            // Act
            // Subscribe to events. This will invoke the handler for the initial state (42)
            safeList.SignOnEvents(handler);

            // Trigger an Added event. This will invoke the handler and test the reentrancy lock safety
            safeList.AddItem(100);

            // Clean up subscription
            safeList.UnSignFromEvents(handler);

            // Assert
            // Verify that the handler was executed for the added item
            Assert.True(isHandlerCalledOnAdd);
            
            // Verify that we successfully read data without deadlocking or crashing
            Assert.Equal(2, readLengthDuringEvent);
            Assert.NotNull(itemsDuringEvent);
            Assert.Equal(2, itemsDuringEvent.Length);
            Assert.Contains(42, itemsDuringEvent);
            Assert.Contains(100, itemsDuringEvent);
        }
        
        [Fact]
        public unsafe void AddItemsUsingUnmanagedMemoryAndSpanTest()
        {
            // Arrange
            var safeList = new SafeList<int>(false);
            const int elementCount = 10;

            // Allocating bytes directly in the unmanaged native heap
            nuint byteCount = (nuint)(elementCount * sizeof(int));
            int* nativePointer = (int*)NativeMemory.Alloc(byteCount);

            try
            {
                // Initialize unmanaged memory data
                for (int i = 0; i < elementCount; i++)
                {
                    nativePointer[i] = (i + 1) * 10; // 10, 20, 30... 100
                }

                // Wrap native pointer into a safe Span view for manipulation
                Span<int> nativeSpan = new Span<int>(nativePointer, elementCount);

                // Slice optimization demo: take a part of the span without allocating new memory
                ReadOnlySpan<int> slicedSpan = nativeSpan.Slice(0, 5); // Takes first 5 elements

                // Since SafeList expects an array, we extract it efficiently.
                safeList.AddItems(slicedSpan.ToArray());
            }
            finally
            {
                NativeMemory.Free(nativePointer);
            }

            // Memory is placed entirely on the thread execution stack
            Span<int> stackSpan = stackalloc int[3];
            stackSpan[0] = 500;
            stackSpan[1] = 600;
            stackSpan[2] = 700;

            // Act - Add stack-allocated data converted on-the-fly
            safeList.AddItems(stackSpan.ToArray());

            // Assert
            // 5 elements from native slice + 3 elements from stack = 8 total
            Assert.Equal(8, safeList.Length);
            
            var resultItems = safeList.Items;
            Assert.Contains(30, resultItems);  // From unmanaged slice
            Assert.Contains(600, resultItems); // From stack allocation
            Assert.DoesNotContain(60, resultItems); // Elements after index 5 in native memory were sliced out
        }
        
        [Theory]
        [InlineData(1000)]
        [InlineData(5000)]
        [InlineData(10000)]
        [InlineData(20000)]
        public async Task ChaoticMultiThreadingLoadAndConsistencyTest(int taskCount)
        {
            // Arrange
            var safeList = new SafeList<TestObject>(sendCollectionState: false);
            var tasks = new List<Task>();

            // Atomic counters to track exact state changes across all threads
            int successfulAdds = 0;
            int successfulRemoves = 0;

            // Shared helper to keep track of items currently inside the collection
            var activeItems = new ConcurrentBag<TestObject>();

            EventHandler<CollectionEventArgs<TestObject>> handler = (sender, args) =>
            {
                // Ensure event is fired with correct event types during heavy load
                Assert.True(args.CollectionEventType == CollectionEventTypeEnum.Added || 
                            args.CollectionEventType == CollectionEventTypeEnum.Removed ||
                            args.CollectionEventType == CollectionEventTypeEnum.ItemIsAlreadyExisted ||
                            args.CollectionEventType == CollectionEventTypeEnum.ItemNotFound);
            };

            safeList.SignOnEvents(handler);

            // Act
            for (int i = 0; i < taskCount; i++)
            {
                int currentId = i;

                tasks.Add(Task.Run(() =>
                {
                    // Randomly choose between Add (0) and Remove (1) operation in a thread-safe manner
                    int operationType = Random.Shared.Next(0, 2); 

                    if (operationType == 0)
                    {
                        // Operation: Add
                        var item = new TestObject(currentId);
                        if (safeList.AddItem(item))
                        {
                            Interlocked.Increment(ref successfulAdds);
                            activeItems.Add(item);
                        }
                    }
                    else
                    {
                        // Operation: Remove
                        // Try to grab an item that was previously added by any thread
                        if (activeItems.TryTake(out var itemToRemove))
                        {
                            if (safeList.RemoveItem(itemToRemove))
                            {
                                Interlocked.Increment(ref successfulRemoves);
                            }
                        }
                        else
                        {
                            // If no items are ready yet, simulate a noisy remove attempt with a ghost item
                            var ghostItem = new TestObject(-currentId);
                            safeList.RemoveItem(ghostItem);
                        }
                    }
                }));
            }

            // Wait for all chaotic additions and removals to complete
            await Task.WhenAll(tasks);
            safeList.UnSignFromEvents(handler);

            // Assert
            // Consistency Check: The actual length must precisely match (Adds - Removes)
            int expectedLength = successfulAdds - successfulRemoves;
            Assert.Equal(expectedLength, safeList.Length);

            // Snapshot Check: The array returned by .Items must have the exact same size
            var finalItems = safeList.Items;
            Assert.Equal(expectedLength, finalItems.Length);

            // Strict Integrity Check: All remaining items in our tracking bag must exist in the SafeList
            foreach (var remainingItem in activeItems)
            {
                Assert.Contains(remainingItem, finalItems);
            }
        }
    }
}
