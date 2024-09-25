using System.Runtime.CompilerServices;
using Undersoft.SDK.Uniques;

namespace Undersoft.SDK.Series.Base
{

    public abstract class RegistryBase<V> : ListingBase<V>
    {
        const int READING_TIMEOUT = 5000;
        const int REHASHING_TIMEOUT = 5000;
        const int WRITING_TIMEOUT = 5000;

        int readers;

        readonly ManualResetEventSlim readingAccess = new ManualResetEventSlim(true, 128);
        readonly ManualResetEventSlim rehashingAccess = new ManualResetEventSlim(true, 128);
        readonly ManualResetEventSlim writingAccess = new ManualResetEventSlim(true, 128);
        readonly SemaphoreSlim writingPass = new SemaphoreSlim(1);


        public RegistryBase() : this(false, 17, HashBits.bit64)
        {
        }
        public RegistryBase(int capacity = 17, HashBits bits = HashBits.bit64) : base(capacity, bits)
        {
        }
        public RegistryBase(bool repeatable, int capacity = 17, HashBits bits = HashBits.bit64) : base(
            repeatable,
            capacity,
            bits)
        {
        }

        public RegistryBase(
            IEnumerable<V> collection,
            int capacity = 17,
            bool repeatable = false,
            HashBits bits = HashBits.bit64) : this(repeatable, capacity, bits)
        {
            if (collection != null)
                foreach (V c in collection)
                    InnerAdd(c);
        }

        protected void AcquireReading()
        {
            Interlocked.Increment(ref readers);
            rehashingAccess.Reset();
            if (!readingAccess.Wait(READING_TIMEOUT))
                throw new TimeoutException("Reading timeout has been exceeded");
        }
        protected void AcquireRehashing()
        {
            if (!rehashingAccess.Wait(REHASHING_TIMEOUT))
                throw new TimeoutException("Rehashing timeout has been exceeded");
            readingAccess.Reset();
        }
        protected void AcquireWriting()
        {
            do
            {
                if (!writingAccess.Wait(WRITING_TIMEOUT))
                    throw new TimeoutException("Writing timeout has been exceeded");
            } while (!writingPass.Wait(1));
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

        public override V Dequeue()
        {
            AcquireWriting();
            V temp = base.Dequeue();
            ReleaseWriting();
            return temp;
        }

        public override ISeriesItem<V> EmptyItem() { return new SeriesItem<V>(); }

        public override ISeriesItem<V>[] EmptyTable(int size) { return new SeriesItem<V>[size]; }

        public override ISeriesItem<V>[] EmptyVector(int size) { return new SeriesItem<V>[size]; }

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
            throw new IndexOutOfRangeException("Index out of range");
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
            InnerInsert(index, item);
            ReleaseWriting();
        }

        public override ISeriesItem<V> NewItem(ISeriesItem<V> item) { return new SeriesItem<V>(item); }

        public override ISeriesItem<V> NewItem(V value) { return new SeriesItem<V>(value); }

        public override ISeriesItem<V> NewItem(object key, V value) { return new SeriesItem<V>(key, value); }

        public override ISeriesItem<V> NewItem(long key, V value) { return new SeriesItem<V>(key, value); }

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
            ReleaseReading();
            return temp;
        }

        protected override ISeriesItem<V> swapRepeated(ISeriesItem<V> item)
        {
            AcquireRehashing();
            var _item = base.swapRepeated(item);
            ReleaseRehashing();
            return _item;
        }
    }
}
