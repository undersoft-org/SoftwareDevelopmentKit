﻿namespace System.Linq
{
    using Collections.Generic;
    using Collections.ObjectModel;
    using Threading.Tasks;
    using Transactions;
    using Undersoft.SDK;
    using Undersoft.SDK.Invoking;
    using Undersoft.SDK.Series;
    using Undersoft.SDK.Utilities;

    public static class SeriesLinqExtensions
    {
        #region Methods       

        public static TResult[] DoEach<TItem, TResult>(this IEnumerable<TItem> items, Func<TItem, TResult> action)
        {
            return items.ForEach(action).Commit();
        }

        public static IEnumerable<TResult> ForOnly<TItem, TResult>(this IEnumerable<TItem> items, Func<TItem, bool> condition, Func<TItem, TResult> action)
        {
            if (items.Any(r => condition(r)))
            {
                foreach (var item in items)
                {
                    if (condition(item))
                        yield return action(item);
                }
            }
        }
        public static IEnumerable<TResult> ForOnly<TItem, TResult>(this IEnumerable<TItem> items, Func<TItem, bool> condition, Func<TItem, int, TResult> action)
        {
            if (items.Any(r => condition(r)))
            {
                int i = 0;
                foreach (var item in items)
                {                
                    if (condition(item))
                        yield return action(item, i);
                    i++;
                }
            }
        }

        public static async IAsyncEnumerable<TResult> ForOnlyAsync<TItem, TResult>(this IEnumerable<TItem> items, Func<TItem, bool> condition, Func<TItem, int, TResult> action)
        {
            if (items.Any(r => condition(r)))
            {
                int i = 0;
                foreach (var item in items)
                {
                    if (condition(item))
                        yield return await Task.Factory.StartNew(() => action(item, i), TaskCreationOptions.AttachedToParent);
                    i++;
                }
            }
        }
        public static async IAsyncEnumerable<TResult> ForOnlyAsync<TItem, TResult>(this IEnumerable<TItem> items, Func<TItem, bool> condition, Func<TItem, TResult> action)
        {
            if (items.Any(r => condition(r)))
            {
                foreach (var item in items)
                {
                    if (condition(item))
                        yield return await Task.Factory.StartNew(() => action(item), TaskCreationOptions.AttachedToParent);
                }
            }
        }

        public static void ForOnly<TItem>(this IEnumerable<TItem> items, Func<TItem, bool> condition, Action<TItem> action)
        {
            if (items.Any(r => condition(r)))
            {
                foreach (var item in items)
                {
                    if (condition(item))
                        action(item);
                }
            }
        }
        public static void ForOnly<TItem>(this IEnumerable<TItem> items, Func<TItem, bool> condition, Action<TItem, int> action)
        {
            if (items.Any(r => condition(r)))
            {
                int i = 0;
                foreach (var item in items)
                {
                    if (condition(item))
                        action(item, i++);
                }
            }
        }

        public static Task ForOnlyAsync<TItem>(this IEnumerable<TItem> items, Func<TItem, bool> condition, Action<TItem> action)
        {
            return Task.Factory.StartNew(() => items.ForOnly(condition, action), TaskCreationOptions.AttachedToParent);
        }

        public static void ForEach<TItem>(this IEnumerable<TItem> items, Action<TItem> action)
        {
            foreach (var item in items)
            {
                action(item);
            }
        }
        public static void ForEach<TItem>(this IEnumerable<TItem> items, Action<TItem, int> action)
        {
            int i = 0;
            foreach (var item in items)
            {
                action(item, i++);
            }
        }

        public static IEnumerable<TResult> ForEach<TItem, TResult>(this IEnumerable<TItem> items, Func<TItem, int, TResult> action)
        {
            int i = 0;
            foreach (var item in items)
            {
                yield return action(item, i++);
            }
        }
        public static IEnumerable<TResult> ForEach<TItem, TResult>(this IEnumerable<TItem> items, Func<TItem, TResult> action)
        {
            foreach (var item in items)
            {
                yield return action(item);
            }
        }       

        public static async IAsyncEnumerable<TResult> ForEachAsync<TItem, TResult>(this IEnumerable<TItem> items, Func<TItem, int, TResult> action)
        {
            int i = 0;
            foreach (var item in items)
            {
                yield return await Task.Factory.StartNew(() => action(item, i++), TaskCreationOptions.AttachedToParent);
            }
        }
        public static async IAsyncEnumerable<TResult> ForEachAsync<TItem, TResult>(this IEnumerable<TItem> items, Func<TItem, TResult> action)
        {
            foreach (var item in items)
            {
                yield return await Task.Factory.StartNew(() => action(item), TaskCreationOptions.AttachedToParent);
            }
        }
        public static async IAsyncEnumerable<TResult> ForEachAsync<TItem, TResult>(this IAsyncEnumerable<TItem> items, Func<TItem, TResult> action)
        {
            await foreach (var item in items)
            {
                yield return await Task.Factory.StartNew(() => action(item), TaskCreationOptions.AttachedToParent);
            }
        }

        public static IQueryable<TResult> ForEach<TItem, TResult>(this IQueryable<TItem> items, Func<TItem, TResult> action)
        {
            return items.AsEnumerable().ForEach(i => action(i)).AsQueryable();
        }

        public static Task<IQueryable<TResult>> ForEachAsync<TItem, TResult>(this IQueryable<TItem> items, Func<TItem, TResult> action)
        {
            return Task.FromResult(items.ForEach(i => action(i)));
        }

        public static Task ForEachAsync<TItem>(this IEnumerable<TItem> items, Action<TItem> action)
        {
            return Task.Factory.StartNew(() => items.ForEach(action), TaskCreationOptions.AttachedToParent);
        }
        public static Task ForEachAsync<TItem>(this IEnumerable<TItem> items, Action<TItem, int> action)
        {
            return Task.Factory.StartNew(() => items.ForEach(action), TaskCreationOptions.AttachedToParent);
        }
        public static Task ForEachAsync<TItem>(this ParallelQuery<TItem> items, Action<TItem> action)
        {
            return Task.Factory.StartNew(() => items.ForAll(action), TaskCreationOptions.AttachedToParent);
        }

        public static Task<List<TItem>> ToListAsync<TItem>(this IEnumerable<TItem> items, IInvoker callback = null)
        {
            return Task.Factory.StartNew(() =>
            {
                List<TItem> list;
                using (TransactionScope ts = CreateLockTransaction())
                {
                    list = items.ToList();
                    ts.Complete();
                    if(callback != null)
                        callback.InvokeAsync(list).ConfigureAwait(false);
                }
                return list;              
            }, TaskCreationOptions.AttachedToParent);
        }

        public static Task<TItem[]> ToArrayAsync<TItem>(this IEnumerable<TItem> items, IInvoker callback = null)
        {
            return Task.Factory.StartNew(() =>
            {
                if(callback != null)
                    return items.Commit(a => callback.InvokeAsync(a));                
                return items.ToArray();
            });
        }

        public static Task<Registry<TItem>> ToRegistryAsync<TItem>(this IEnumerable<TItem> items, bool repeatable = false, IInvoker callback = null)
        {
            return Task.Factory.StartNew(() => items.ToRegistry(repeatable, callback));
        }

        public static Task<Catalog<TItem>> ToCatalogAsync<TItem>(this IEnumerable<TItem> items, IInvoker callback = null)
        {
            return Task.Factory.StartNew(() => items.ToCatalog(callback));
        }

        public static Task<Listing<TItem>> ToListingAsync<TItem>(this IEnumerable<TItem> items, IInvoker callback = null)
        {
            return Task.Factory.StartNew(() => items.ToListing(callback));
        }

        public static Listing<TItem> ToListing<TItem>(this IEnumerable<TItem> items, IInvoker callback = null)
        {
            var listing = new Listing<TItem>(items);

            if (callback != null) 
                callback.InvokeAsync(listing);
            
            return listing;
        }

        public static Task<Chain<TItem>> ToChainAsync<TItem>(this IEnumerable<TItem> items, IInvoker callback = null)
        {
            return Task.Run(() => items.ToChain(callback));
        }

        public static Chain<TItem> ToChain<TItem>(this IEnumerable<TItem> items, IInvoker callback = null)
        {
            var listing = new Chain<TItem>(items);

            if (callback != null) 
                callback.InvokeAsync(listing);

            return listing;
        }

        public static Registry<TItem> ToRegistry<TItem>(this IEnumerable<TItem> items, bool repeatable = false, IInvoker callback = null)
        {
            var registry = new Registry<TItem>(items, 31, repeatable);

            if (callback != null) 
                callback.InvokeAsync(registry);

            return registry;
        }

        public static Catalog<TItem> ToCatalog<TItem>(this IEnumerable<TItem> items, IInvoker callback = null)
        {
            var catalog = new Catalog<TItem>(items);

            if (callback != null)
                callback.InvokeAsync(catalog);

            return catalog;
        }

        public static TypedRegistry<TItem> ToTypedRegistry<TItem>(this IEnumerable<TItem> items, bool repeatable = false, IInvoker callback = null) where TItem : IOrigin
        {
            var registry = new TypedRegistry<TItem>(items, 17, repeatable);

            if (callback != null)
                callback.InvokeAsync(registry);

            return registry;
        }

        public static Task<TItem[]> CommitAsync<TItem>(this IEnumerable<TItem> items)
        {
            return Task.Run(() => items.ToArray());
        }

        public static TItem[] Commit<TItem>(this IEnumerable<TItem> items)
        {
            return items.ToArray();
        }

        public static TItem[] Commit<TItem>(this IEnumerable<TItem> items, Action<TItem[]> actionAfterCommit)
        {
            TItem[] array;
            using (TransactionScope ts = CreateLockTransaction())
            {
                array = items.ToArray();
                ts.Complete();
                actionAfterCommit.Invoke(array);
            }
            return array;
        }

        public static ObservableCollection<TItem> ToObservableCollection<TItem>(this IEnumerable<TItem> items)
        {
            return new ObservableCollection<TItem>(items);
        }

        public static TransactionScope CreateLockTransaction()
        {
            var options = new TransactionOptions
            {
                IsolationLevel = IsolationLevel.RepeatableRead,                
            };
            return new TransactionScope(TransactionScopeOption.RequiresNew, options, TransactionScopeAsyncFlowOption.Enabled);
        }

        public static T[] ToArray<T>(this IQueryable<T> query)
        {
            using (TransactionScope ts = CreateLockTransaction())
            {
                return query.Commit();
            }
        }

        public static List<T> ToList<T>(this IQueryable<T> query)
        {
            using (TransactionScope ts = CreateLockTransaction())
            {
                return query.AsEnumerable().ToList();
            }
        }

        public static T EnsureOne<T>(this IEnumerable<T> query)
        {
            var item = query.FirstOrDefault();
            return item ??= typeof(T).New<T>();
        }
        #endregion
    }
}
