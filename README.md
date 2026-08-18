# SafeCollections 🚀

[![Language](https://shields.io)](https://microsoft.com)
[![Framework](https://shields.io)](https://microsoft.com)
[![License: MIT](https://shields.io)](LICENSE)

A high-performance, production-ready, thread-safe collection designed specifically for event-driven architectures where uniqueness, $O(1)$ lookup speed, and immediate state synchronization are critical.

## 💡 The Problem

Standard .NET concurrent collections fall short when you need a combination of:
1. **Thread-safety** under heavy concurrent read/write loads.
2. **Uniqueness** ($O(1)$ operations for adding/removing items via `HashSet`).
3. **Reactive Events** to notify external subscribers about real-time changes.
4. **Late-Subscription Synchronization** (delivering the entire accumulated state to a new listener *before* streaming live updates, without missing concurrent modifications).

`SafeList<T>` bridges this gap, serving as an ideal **In-Memory Cache with Real-Time Notifications**.

---

## ⚡ Key Features

* **Advanced Synchronization:** Powered by `ReaderWriterLockSlim`, allowing multiple concurrent readers to access data simultaneously without blocking each other, while maintaining strict isolation for write operations.
* **$O(1)$ Efficiency:** Built on top of `HashSet<T>` to guarantee instant item lookups, additions, and removals regardless of collection size.
* **Smart Event Streaming:** Supports a `_sendCollectionState` flag. When a new listener signs on, it atomically captures a snapshot of the current state, flushes it to the subscriber, and seamlessly transitions into streaming real-time changes—guaranteeing **zero data loss** or duplication during synchronization.
* **Deadlock-Free Architecture:** Fully decoupled event invocations ensure that subscribers can safely make reentrant queries (e.g., calling `.Items` or `.Length` from within the event handler) without causing `LockRecursionException` or deadlocks.
* **Production Tested:** Verified under extreme synthetic stress-tests simulating tens of thousands of chaotic, simultaneous read/write operations.

---

## 🏗️ Architecture & Business Use-Case

Imagine a high-frequency financial trading system or a banking application tracking transactions throughout the trading day:

1. The bank accumulates trades in a `SafeList<T>` to ensure no duplicate transactions are stored.
2. At any point, a new monitoring tool or UI dashboard ("Listener") connects.
3. The listener instantly receives all transactions accumulated so far and continuously streams new ones in real-time.
4. At the end of the day, the collection is cleared, resetting the system for the next cycle.

---

## 💻 Quick Start

### Basic Usage

```csharp
// Initialize a thread-safe list that sends initial snapshot to new subscribers
var safeList = new SafeList<Transaction>(sendCollectionState: true);

// Add items instantly with O(1) performance
safeList.AddItem(new Transaction("TXN-001", 1500.00));
safeList.AddItem(new Transaction("TXN-002", 450.50));

// Subscribe to real-time events
safeList.SignOnEvents((sender, args) =>
{
    switch (args.CollectionEventType)
    {
        case CollectionEventTypeEnum.None:
            Console.WriteLine(\$"Received initial snapshot of {args.Items.Length} items.");
            break;
        case CollectionEventTypeEnum.Added:
            Console.WriteLine(\$"New transaction added: {args.Items[0].Id}");
            break;
        case CollectionEventTypeEnum.Removed:
            Console.WriteLine(\$"Transaction removed.");
            break;
    }
});
```

---

## 🧪 Robust Testing & Stability

The project comes with a comprehensive suite of unit tests located in the `SafeCollections_UT` project, built using **xUnit**. 

The suite includes:
* **`ReentrancyAndDeadlockPreventionTest`:** Ensures event handlers can safely query the collection recursively from within the event execution context.
* **`ChaoticMultiThreadingLoadAndConsistencyTest`:** Spawns up to 20,000 concurrent, randomized tasks performing `AddItem` and `RemoveItem` operations simultaneously to ensure mathematical data consistency and lock stability under heavy race conditions.
* **High-Performance Memory Tests:** Includes benchmarks demonstrating integration with non-managed memory (`NativeMemory`), `stackalloc`, and `Span<T>` slices for zero-allocation data processing.

---

## 🛠️ Infrastructure & Deployment

The repository includes ready-to-use **Kubernetes Source-to-Image (s2i)** configurations, making it extremely straightforward to containerize and deploy this service into cloud-native microservice environments.

---

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
