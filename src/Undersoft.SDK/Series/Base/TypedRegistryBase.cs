namespace Undersoft.SDK.Series.Base
{
    using System.Collections.Generic;
    using System.Threading;
    using Undersoft.SDK;
    using Undersoft.SDK.Uniques;

    public abstract class TypedRegistryBase<V> : TypedListingBase<V> where V : IIdentifiable
    {
        const int WAIT_READ_TIMEOUT = 5000;
        const int WAIT_REHASH_TIMEOUT = 15000;
        const int WAIT_WRITE_TIMEOUT = 10000;

        internal readonly ManualResetEventSlim readingAccess = new ManualResetEventSlim(true, 128);
        internal readonly ManualResetEventSlim rehashingAccess = new ManualResetEventSlim(true, 128);
        internal readonly ManualResetEventSlim writingAccess = new ManualResetEventSlim(true, 128);
        internal readonly SemaphoreSlim writingPass = new SemaphoreSlim(1);
        internal int readers;

        public TypedRegistryBase() : base(17, HashBits.bit64) { }

        public TypedRegistryBase(int capacity = 17, HashBits bits = HashBits.bit64)
            : base(capacity, bits) { }

        public TypedRegistryBase(bool repeatable, int capacity = 17, HashBits bits = HashBits.bit64)
      : base(repeatable, capacity, bits) { }

        public TypedRegistryBase(
            IEnumerable<IIdentifiable> collection,
            int capacity = 17,
            bool repeatable = false,
            HashBits bits = HashBits.bit64
        ) : this(capacity, bits)
        {
            if (collection != null)
                foreach (var c in collection)
                    Add(c.Id, c);
        }

        public TypedRegistryBase(
            IEnumerable<V> collection,
            int capacity = 17,
            bool repeatable = false,
            HashBits bits = HashBits.bit64
        ) : this(capacity, bits)
        {
            if (collection != null)
                foreach (V c in collection)
                    InnerAdd(c);
        }

        public TypedRegistryBase(
            IList<IUnique<V>> collection,
            int capacity = 17,
            bool repeatable = false,
            HashBits bits = HashBits.bit64
        ) : this((int)(collection.Count * 1.5), bits)
        {
            if (collection != null)
                foreach (IUnique<V> c in collection)
                    Add(c);
        }

        public TypedRegistryBase(
            IList<V> collection,
            int capacity = 17,
            bool repeatable = false,
            HashBits bits = HashBits.bit64
        ) : this((int)(collection.Count * 1.5), bits)
        {
            if (collection != null)
                foreach (V c in collection)
                    InnerAdd(c);
        }

        protected void AcquireReading()
        {
            Interlocked.Increment(ref readers);
            rehashingAccess.Reset();
            if (!readingAccess.Wait(WAIT_READ_TIMEOUT))
            {
                ReleaseReading();
                throw new TimeoutException("Reading timeout has been exceeded");
            }
        }
        protected void AcquireRehashing()
        {
            if (!rehashingAccess.Wait(WAIT_REHASH_TIMEOUT))
                throw new TimeoutException("Wait InnerRehash Timeout");
            readingAccess.Reset();
        }
        protected void AcquireWriting()
        {
            do
            {
                if (!writingAccess.Wait(WAIT_WRITE_TIMEOUT))
                    throw new TimeoutException("Wait write Timeout");
                writingAccess.Reset();
            } while (!writingPass.Wait(0));
        }
        
        protected void ReleaseReading()
        {
            if (0 == Interlocked.Decrement(ref readers))
                rehashingAccess.Set();
        }
        protected void ReleaseRehashing()
        {
            readingAccess.Set();
        }
        protected void ReleaseWriting()
        {
            writingPass.Release();
            writingAccess.Set();
        }

        protected override ISeriesItem<V> GetItem(long key, V item)
        {
            AcquireReading();
            ISeriesItem<V> _item = base.GetItem(key, item);
            ReleaseReading();
            return _item;
        }

        protected override int IndexOf(long key, V item)
        {
            int id = 0;
            AcquireReading();
            id = base.IndexOf(key, item);
            ReleaseReading();
            return id;
        }

        protected override bool InnerAdd(ISeriesItem<V> value)
        {
            AcquireWriting();
            bool temp = base.InnerAdd(value);
            ReleaseWriting();
            return temp;
        }
        protected override bool InnerAdd(V value)
        {
            AcquireWriting();
            bool temp = base.InnerAdd(value);
            ReleaseWriting();
            return temp;
        }
        protected override bool InnerAdd(long key, V value)
        {
            AcquireWriting();
            bool temp = base.InnerAdd(key, value);
            ReleaseWriting();
            return temp;
        }

        protected override V InnerGet(long key)
        {
            AcquireReading();
            V v = base.InnerGet(key);
            ReleaseReading();
            return v;
        }

        protected override ISeriesItem<V> InnerGetItem(long key)
        {
            AcquireReading();
            ISeriesItem<V> item = base.InnerGetItem(key);
            ReleaseReading();
            return item;
        }

        protected override ISeriesItem<V> InnerPut(ISeriesItem<V> value)
        {
            AcquireWriting();
            ISeriesItem<V> temp = base.InnerPut(value);
            ReleaseWriting();
            return temp;
        }
        protected override ISeriesItem<V> InnerPut(V value)
        {
            AcquireWriting();
            ISeriesItem<V> temp = base.InnerPut(value);
            ReleaseWriting();
            return temp;
        }
        protected override ISeriesItem<V> InnerPut(long key, V value)
        {
            AcquireWriting();
            ISeriesItem<V> temp = base.InnerPut(key, value);
            ReleaseWriting();
            return temp;
        }

        protected override V InnerRemove(long key)
        {
            AcquireWriting();
            V temp = base.InnerRemove(key);
            ReleaseWriting();
            return temp;
        }

        protected override bool InnerTryGet(long key, out ISeriesItem<V> output)
        {
            AcquireReading();
            bool test = base.InnerTryGet(key, out output);
            ReleaseReading();
            return test;
        }

        protected override void Rehash(int newsize)
        {
            AcquireRehashing();
            base.Rehash(newsize);
            ReleaseRehashing();
        }

        protected override void Reindex()
        {
            AcquireRehashing();
            base.Reindex();
            ReleaseRehashing();
        }

        public override void Clear()
        {
            AcquireWriting();
            AcquireRehashing();

            base.Clear();

            ReleaseRehashing();
            ReleaseWriting();
        }

        public override void CopyTo(Array array, int index)
        {
            AcquireReading();
            base.CopyTo(array, index);
            ReleaseReading();
        }
        public override void CopyTo(ISeriesItem<V>[] array, int index)
        {
            AcquireReading();
            base.CopyTo(array, index);
            ReleaseReading();
        }
        public override void CopyTo(V[] array, int index)
        {
            AcquireReading();
            base.CopyTo(array, index);
            ReleaseReading();
        }

        public override V Dequeue()
        {
            AcquireWriting();
            V temp = base.Dequeue();
            ReleaseWriting();
            return temp;
        }

        public override ISeriesItem<V> GetItem(int index)
        {
            if (index < count)
            {
                AcquireReading();
                if (removed > 0)
                {
                    ReleaseReading();
                    AcquireWriting();
                    Reindex();
                    ReleaseWriting();
                    AcquireReading();
                }

                ISeriesItem<V> temp = vector[index];
                ReleaseReading();
                return temp;
            }
            return null;
        }

        public override int IndexOf(ISeriesItem<V> item)
        {
            int id = 0;
            AcquireReading();
            id = base.IndexOf(item);
            ReleaseReading();
            return id;
        }

        public override void Insert(int index, ISeriesItem<V> item)
        {
            AcquireWriting();
            bool temp = base.InnerAdd(item);
            base.InnerInsert(index, item);
            ReleaseWriting();
        }
        public override void Insert(int index, V item)
        {
            AcquireWriting();
            var newitem = NewItem(item);
            bool temp = base.InnerAdd(newitem);
            base.InnerInsert(index, newitem);
            ReleaseWriting();
        }

        public override V[] ToArray()
        {
            AcquireReading();
            V[] array = base.ToArray();
            ReleaseReading();
            return array;
        }

        public override bool TryDequeue(out ISeriesItem<V> output)
        {
            AcquireWriting();
            bool temp = base.TryDequeue(out output);
            ReleaseWriting();
            return temp;
        }
        public override bool TryDequeue(out V output)
        {
            AcquireWriting();
            bool temp = base.TryDequeue(out output);
            ReleaseWriting();
            return temp;
        }

        public override bool TryPick(int skip, out ISeriesItem<V> output)
        {
            AcquireWriting();
            bool temp = base.TryPick(skip, out output);
            ReleaseWriting();
            return temp;
        }
    }
}
