# SafeCollections
Thread safe collection (SafeList) based on Hashset with events notification. Test sample.

An example of using a collection: during a trading day, the bank's accounting system accumulates transactions received from the exchange in a collection being created. At an arbitrary moment in time, a listener is connected to the event of changing the collection of deals, which must receive both already accumulated deals and newly received ones. At the end of the trading day, the accounting system clears the collection of transactions, all listeners should be notified of this.

Kubernetes s2i implementation.
