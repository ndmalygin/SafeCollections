using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SafeCollections;
using Xunit;

namespace SafeCollections_UT
{
    /// <summary>
    ///     Unit tests for SafeList generic class.
    /// </summary>
    public sealed class SafeList_UT
    {
        private readonly SafeList<DummyObject> _list = new SafeList<DummyObject>();

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
                (sender, args) =>
                {
                    Assert.Equal(item, args.Items[0]);
                    Assert.Equal(CollectionEventTypeEnum.Removed, args.CollectionEventType);
                }
            );

            safeList.RemoveItems(new[] { item });
        }

        private void RemoveItem(int id)
        {
            var item = new DummyObject(id);
            _list.AddItem(item);
            Assert.True(_list.RemoveItem(item));
        }

        [Theory]
        [InlineData(1000)]
        public void RemoveMultiThreadingTest(int taskCount)
        {
            var tasks = new List<Task>();
            var random = new Random();

            for (var i = 0; i < taskCount; i++)
            {
                tasks.Add(
                    new Task(() =>
                    {
                        var i = random.Next();
                        RemoveItem(i);
                    })
                );
            }

            Parallel.ForEach(tasks, t => t.Start());
            Task.WhenAll(tasks);
        }

        [Fact]
        public void AddItemsTest()
        {
            var safeList = new SafeList<int>(false);
            safeList.SignOnEvents(
                (sender, args) =>
                {
                    Assert.Equal(new[] { 100, 200, 300 }, args.Items);
                    Assert.Equal(CollectionEventTypeEnum.Added, args.CollectionEventType);
                }
            );

            safeList.AddItems(new[] { 100, 200, 300 });
        }

        [Fact]
        public void ClearAllTest()
        {
            var safeList = new SafeList<int>();
            safeList.AddItems(new[] { 100, 200, 300 });
            safeList.SignOnEvents(
                (sender, args) =>
                {
                    switch (args.CollectionEventType)
                    {
                        case CollectionEventTypeEnum.None:
                            Assert.Equal(new[] { 100, 200, 300 }, args.Items);
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
            safeList.AddItems(new[] { 100, 200, 300 });
            safeList.SignOnEvents(
                (sender, args) =>
                {
                    switch (args.CollectionEventType)
                    {
                        case CollectionEventTypeEnum.None:
                            Assert.Equal(new[] { 100, 200, 300 }, args.Items);
                            break;
                        default:
                            Assert.Equal(new[] { 100, 300 }, args.Items);
                            Assert.Equal(CollectionEventTypeEnum.Removed, args.CollectionEventType);
                            break;
                    }
                }
            );

            safeList.RemoveItems(new[] { 100, 300 });
        }
    }
}
