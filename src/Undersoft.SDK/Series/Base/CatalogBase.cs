namespace Undersoft.SDK.Series.Base
{
    using System.Collections.Generic;
    using System.Threading;
    using Undersoft.SDK.Uniques;

    public abstract class CatalogBase<V> : ChainBase<V>
    {
        internal const int READING_TIMEOUT = 5000;

        internal const int REHASHING_TIMEOUT = 5000;

        internal const int WAIT_WRITE_TIMEOUT = 5000;

        int readers;

        internal readonly ManualResetEventSlim readingAccess = new ManualResetEventSlim(true, 128);
        internal readonly ManualResetEventSlim rehashingAccess = new ManualResetEventSlim(true, 128);
        internal readonly ManualResetEventSlim writingAccess = new ManualResetEventSlim(true, 128);
        internal readonly SemaphoreSlim writingPass = new SemaphoreSlim(1);

        protected CatalogBase() : base() { }

        protected CatalogBase(int capacity = 17, HashBits bits = HashBits.bit64)
            : base(capacity, bits) { }

        protected CatalogBase(
            IEnumerable<V> collection,
            int capacity = 17,
            HashBits bits = HashBits.bit64
        ) : this(capacity, bits)
        {
            if (collection != null)
                foreach (V c in collection)
                    Add(c);
        }

        protected CatalogBase(IList<V> collection, int capacity = 17, HashBits bits = HashBits.bit64)
            : this((capacity > collection.Count) ? capacity : collection.Count, bits)
        {
            foreach (V c in collection)
                Add(c);
        }

        protected void AcquireReading()
        {
            Interlocked.Increment(ref readers);
            rehashingAccess.Reset();
            if (!readingAccess.Wait(READING_TIMEOUT))
            {
                ReleaseReading();
                throw new TimeoutException("Reading timeout has been exceeded");
            }
        }

        protected void AcquireRehashing()
        {
            if (!rehashingAccess.Wait(REHASHING_TIMEOUT))
                throw new TimeoutException("Wait rehash Timeout");
            readingAccess.Reset();
        }

        protected void AcquireWriting()
        {
            do
            {
                if (!writingAccess.Wait(WAIT_WRITE_TIMEOUT))
                    throw new TimeoutException("Wait write Timeout");
            } while (!writingPass.Wait(0));
            writingAccess.Reset();
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

                int i = -1;
                int id = index;
                ISeriesItem<V> item = first.Next;
                for (; ; )
                {
                    if (++i == id)
                    {
                        ReleaseReading();
                        return item;
                    }
                    item = item.Next;
                }
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

        public override int IndexOf(V item)
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
            base.Insert(index, item);
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

        public override bool TryPick(int skip, out V output)
        {
            AcquireReading();
            bool temp = base.TryPick(skip, out output);
            AcquireReading();
            return temp;
        }

    }
}
