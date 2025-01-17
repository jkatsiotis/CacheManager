﻿using System;
using System.Globalization;
using System.Threading.Tasks;
using CacheManager.Core.Logging;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core.Internal
{
    /// <summary>
    /// The BaseCache class implements the overall logic of this cache library and delegates the
    /// concrete implementation of how e.g. add, get or remove should work to a derived class.
    /// <para>
    /// To use this base class simply override the abstract methods for Add, Get, Put and Remove.
    /// <br/> All other methods defined by <c>ICache</c> will be delegated to those methods.
    /// </para>
    /// </summary>
    /// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
    public abstract class BaseCache<TCacheValue> : IDisposable, ICache<TCacheValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseCache{TCacheValue}"/> class.
        /// </summary>
        protected internal BaseCache()
        {
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="BaseCache{TCacheValue}"/> class.
        /// </summary>
        ~BaseCache()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>The logger instance.</value>
        protected abstract ILogger Logger { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="BaseCache{TCacheValue}"/> is disposed.
        /// </summary>
        /// <value><c>true</c> if disposed; otherwise, <c>false</c>.</value>
        protected bool Disposed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="BaseCache{TCacheValue}"/> is disposing.
        /// </summary>
        /// <value><c>true</c> if disposing; otherwise, <c>false</c>.</value>
        protected bool Disposing { get; set; }

        /// <summary>
        /// Gets or sets a value for the specified key. The indexer is identical to the
        /// corresponding <see cref="Put(string, TCacheValue)"/> and <see cref="Get(string)"/> calls.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The value being stored in the cache for the given <paramref name="key"/>.</returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="key"/> is null.</exception>
        public virtual TCacheValue this[string key]
        {
            get => Get(key).Result;
            set => Put(key, value).Wait();
        }

        /// <summary>
        /// Gets or sets a value for the specified key and region. The indexer is identical to the
        /// corresponding <see cref="Put(string, TCacheValue, string)"/> and
        /// <see cref="Get(string, string)"/> calls.
        /// <para>
        /// With <paramref name="region"/> specified, the key will <b>not</b> be found in the global cache.
        /// </para>
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>
        /// The value being stored in the cache for the given <paramref name="key"/> and <paramref name="region"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If the <paramref name="key"/> or <paramref name="region"/> is null.
        /// </exception>
        public virtual TCacheValue this[string key, string region]
        {
            get => Get(key, region).Result;
            set => Put(key, value, region).Wait();
        }

        /// <summary>
        /// Adds a value for the specified key to the cache.
        /// <para>
        /// The <c>Add</c> method will <b>not</b> be successful if the specified
        /// <paramref name="key"/> already exists within the cache!
        /// </para>
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="value">The value which should be cached.</param>
        /// <returns>
        /// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If the <paramref name="key"/> or <paramref name="value"/> is null.
        /// </exception>
        public virtual async Task<bool> Add(string key, TCacheValue value)
        {
            // null checks are done within ctor of the item
            var item = new CacheItem<TCacheValue>(key, value);
            return await Add(item).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds a value for the specified key and region to the cache.
        /// <para>
        /// The <c>Add</c> method will <b>not</b> be successful if the specified
        /// <paramref name="key"/> already exists within the cache!
        /// </para>
        /// <para>
        /// With <paramref name="region"/> specified, the key will <b>not</b> be found in the global cache.
        /// </para>
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="value">The value which should be cached.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>
        /// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If the <paramref name="key"/>, <paramref name="value"/> or <paramref name="region"/> is null.
        /// </exception>
        public virtual async Task<bool> Add(string key, TCacheValue value, string region)
        {
            // null checks are done within ctor of the item
            var item = new CacheItem<TCacheValue>(key, region, value);
            return await Add(item).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds the specified <c>CacheItem</c> to the cache.
        /// <para>
        /// Use this overload to overrule the configured expiration settings of the cache and to
        /// define a custom expiration for this <paramref name="item"/> only.
        /// </para>
        /// <para>
        /// The <c>Add</c> method will <b>not</b> be successful if the specified
        /// <paramref name="item"/> already exists within the cache!
        /// </para>
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If the <paramref name="item"/> or the item's key or value is null.
        /// </exception>
        public virtual async Task<bool> Add(CacheItem<TCacheValue> item)
        {
            NotNull(item, nameof(item));

            return await AddInternal(item).ConfigureAwait(false);
        }

        /// <summary>
        /// Clears this cache, removing all items in the base cache and all regions.
        /// </summary>
        public abstract Task Clear();

        /// <summary>
        /// Clears the cache region, removing all items from the specified <paramref name="region"/> only.
        /// </summary>
        /// <param name="region">The cache region.</param>
        /// <exception cref="ArgumentNullException">If the <paramref name="region"/> is null.</exception>
        public abstract Task ClearRegion(string region);

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public abstract Task<bool> Exists(string key);

        /// <inheritdoc />
        public abstract Task<bool> Exists(string key, string region);

        /// <summary>
        /// Gets a value for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The value being stored in the cache for the given <paramref name="key"/>.</returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="key"/> is null.</exception>
        public virtual async Task<TCacheValue> Get(string key)
        {
            var item = await GetCacheItem(key).ConfigureAwait(false);

            if (item != null && item.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return item.Value;
            }

            return default;
        }

        /// <summary>
        /// Gets a value for the specified key and region.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>
        /// The value being stored in the cache for the given <paramref name="key"/> and <paramref name="region"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If the <paramref name="key"/> or <paramref name="region"/> is null.
        /// </exception>
        public virtual async Task<TCacheValue> Get(string key, string region)
        {
            var item = await GetCacheItem(key, region).ConfigureAwait(false);

            if (item != null
                && item.Key.Equals(key, StringComparison.OrdinalIgnoreCase)
                && item.Region != null
                && item.Region.Equals(region, StringComparison.OrdinalIgnoreCase))
            {
                return item.Value;
            }

            return default;
        }

        /// <summary>
        /// Gets a value for the specified key and will cast it to the specified type.
        /// </summary>
        /// <typeparam name="TOut">The type the value is converted and returned.</typeparam>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The value being stored in the cache for the given <paramref name="key"/>.</returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="key"/> is null.</exception>
        /// <exception cref="InvalidCastException">
        /// If no explicit cast is defined from <c>TCacheValue</c> to <c>TOut</c>.
        /// </exception>
        public virtual async Task<TOut> Get<TOut>(string key)
        {
            object value = await Get(key).ConfigureAwait(false);
            return GetCasted<TOut>(value);
        }

        /// <summary>
        /// Gets a value for the specified key and region and will cast it to the specified type.
        /// </summary>
        /// <typeparam name="TOut">The type the cached value should be converted to.</typeparam>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>
        /// The value being stored in the cache for the given <paramref name="key"/> and <paramref name="region"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If the <paramref name="key"/> or <paramref name="region"/> is null.
        /// </exception>
        /// <exception cref="InvalidCastException">
        /// If no explicit cast is defined from <c>TCacheValue</c> to <c>TOut</c>.
        /// </exception>
        public virtual async Task<TOut> Get<TOut>(string key, string region)
        {
            object value = await Get(key, region).ConfigureAwait(false);
            return GetCasted<TOut>(value);
        }

        /// <summary>
        /// Gets the <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="key"/> is null.</exception>
        public virtual async Task<CacheItem<TCacheValue>> GetCacheItem(string key)
        {
            NotNullOrWhiteSpace(key, nameof(key));

            return await GetCacheItemInternal(key).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the <c>CacheItem</c> for the specified key and region.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// If the <paramref name="key"/> or <paramref name="region"/> is null.
        /// </exception>
        public virtual async Task<CacheItem<TCacheValue>> GetCacheItem(string key, string region)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNullOrWhiteSpace(region, nameof(region));

            return await GetCacheItemInternal(key, region).ConfigureAwait(false);
        }

        /// <summary>
        /// Puts a value for the specified key into the cache.
        /// <para>
        /// If the <paramref name="key"/> already exists within the cache, the existing value will
        /// be replaced with the new <paramref name="value"/>.
        /// </para>
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="value">The value which should be cached.</param>
        /// <exception cref="ArgumentNullException">
        /// If the <paramref name="key"/> or <paramref name="value"/> is null.
        /// </exception>
        public virtual async Task Put(string key, TCacheValue value)
        {
            var item = new CacheItem<TCacheValue>(key, value);
            await Put(item).ConfigureAwait(false);
        }

        /// <summary>
        /// Puts a value for the specified key and region into the cache.
        /// <para>
        /// If the <paramref name="key"/> already exists within the cache, the existing value will
        /// be replaced with the new <paramref name="value"/>.
        /// </para>
        /// <para>
        /// With <paramref name="region"/> specified, the key will <b>not</b> be found in the global cache.
        /// </para>
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="value">The value which should be cached.</param>
        /// <param name="region">The cache region.</param>
        /// <exception cref="ArgumentNullException">
        /// If the <paramref name="key"/>, <paramref name="value"/> or <paramref name="region"/> is null.
        /// </exception>
        public virtual async Task Put(string key, TCacheValue value, string region)
        {
            var item = new CacheItem<TCacheValue>(key, region, value);
            await Put(item).ConfigureAwait(false);
        }

        /// <summary>
        /// Puts the specified <c>CacheItem</c> into the cache.
        /// <para>
        /// If the <paramref name="item"/> already exists within the cache, the existing item will
        /// be replaced with the new <paramref name="item"/>.
        /// </para>
        /// <para>
        /// Use this overload to overrule the configured expiration settings of the cache and to
        /// define a custom expiration for this <paramref name="item"/> only.
        /// </para>
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be cached.</param>
        /// <exception cref="ArgumentNullException">
        /// If the <paramref name="item"/> or the item's key or value is null.
        /// </exception>
        public virtual async Task Put(CacheItem<TCacheValue> item)
        {
            NotNull(item, nameof(item));

            await PutInternal(item).ConfigureAwait(false);
        }

        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="key"/> is null.</exception>
        public virtual async Task<bool> Remove(string key)
        {
            NotNullOrWhiteSpace(key, nameof(key));

            return await RemoveInternal(key).ConfigureAwait(false);
        }

        /// <summary>
        /// Removes a value from the cache for the specified key and region.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If the <paramref name="key"/> or <paramref name="region"/> is null.
        /// </exception>
        public virtual async Task<bool> Remove(string key, string region)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNullOrWhiteSpace(region, nameof(region));

            return await RemoveInternal(key, region).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds a value to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
        /// </returns>
        protected internal abstract Task<bool> AddInternal(CacheItem<TCacheValue> item);

        /// <summary>
        /// Puts a value into the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        protected internal abstract Task PutInternal(CacheItem<TCacheValue> item);

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposeManaged">
        /// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release
        /// only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposeManaged)
        {
            Disposing = true;
            if (!Disposed)
            {
                if (disposeManaged)
                {
                    // do not do anything
                }

                Disposed = true;
            }

            Disposing = false;
        }

        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected abstract Task<CacheItem<TCacheValue>> GetCacheItemInternal(string key);

        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key and region.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected abstract Task<CacheItem<TCacheValue>> GetCacheItemInternal(string key, string region);

        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        protected abstract Task<bool> RemoveInternal(string key);

        /// <summary>
        /// Removes a value from the cache for the specified key and region.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        protected abstract Task<bool> RemoveInternal(string key, string region);

        /// <summary>
        /// Checks if the instance is disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">If the instance is disposed.</exception>
        protected void CheckDisposed()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        /// <summary>
        /// Casts the value to <c>TOut</c>.
        /// </summary>
        /// <typeparam name="TOut">The type.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns>The casted value.</returns>
        protected static TOut GetCasted<TOut>(object value)
        {
            if (value == null)
            {
                return default;
            }

            try
            {
                var changed = Convert.ChangeType(value, typeof(TOut), CultureInfo.InvariantCulture);
                return changed == null ? (TOut)value : (TOut)changed;
            }
            catch
            {
                return (TOut)value;
            }
        }
    }
}
