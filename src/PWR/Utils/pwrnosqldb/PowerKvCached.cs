using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PWR.Utils.PowerKv;

public class ByteArrayWrapper : IEquatable<ByteArrayWrapper>
{
    private readonly byte[] _data;
    private readonly string _hash;
    private readonly int _hashCode;

    public ByteArrayWrapper(byte[] data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        _hash = Convert.ToHexString(data).ToLowerInvariant();
        _hashCode = _hash.GetHashCode();
    }

    public byte[] Data => _data;

    public bool Equals(ByteArrayWrapper? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return _data.SequenceEqual(other._data);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ByteArrayWrapper);
    }

    public override int GetHashCode()
    {
        return _hashCode;
    }

    public override string ToString()
    {
        return _hash;
    }
}

public class PowerKvCached : IDisposable
{
    private readonly PowerKv _db;
    private readonly ConcurrentDictionary<ByteArrayWrapper, byte[]> _cache;
    private readonly ConcurrentBag<Task> _activeTasks;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private volatile bool _isShutdown = false;
    private bool _disposed = false;

    public PowerKvCached(string projectId, string secret)
    {
        _db = new PowerKv(projectId, secret);
        _cache = new ConcurrentDictionary<ByteArrayWrapper, byte[]>();
        _activeTasks = new ConcurrentBag<Task>();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    private static byte[] ToBytes(object data)
    {
        return data switch
        {
            null => throw new PowerKvException("InvalidInput", "Data cannot be null"),
            byte[] bytes => bytes,
            string str => Encoding.UTF8.GetBytes(str),
            int i => Encoding.UTF8.GetBytes(i.ToString()),
            long l => Encoding.UTF8.GetBytes(l.ToString()),
            float f => Encoding.UTF8.GetBytes(f.ToString()),
            double d => Encoding.UTF8.GetBytes(d.ToString()),
            _ => throw new PowerKvException("InvalidInput", "Data must be byte[], string, or number")
        };
    }

    public void Put(object key, object value)
    {
        if (_isShutdown)
            throw new PowerKvException("InvalidInput", "PowerKvCached has been shut down");

        var keyBytes = ToBytes(key);
        var valueBytes = ToBytes(value);
        PutBytes(keyBytes, valueBytes);
    }

    public void PutBytes(byte[] key, byte[] value)
    {
        if (_isShutdown)
            throw new PowerKvException("InvalidInput", "PowerKvCached has been shut down");

        var keyWrapper = new ByteArrayWrapper(key);
        
        // Get old value and update cache immediately
        var oldValue = _cache.AddOrUpdate(keyWrapper, value, (k, v) => value);
        var hadOldValue = _cache.ContainsKey(keyWrapper);

        // If oldValue is same as new value, no need to update db
        // If oldValue doesn't exist, it means this key is being inserted for the first time, so we need to update db
        if (!hadOldValue || !oldValue.SequenceEqual(value))
        {
            // Start background write (non-blocking)
            var task = BackgroundWriteAsync(key, value, keyWrapper, _cancellationTokenSource.Token);
            _activeTasks.Add(task);
        }
    }

    private async Task BackgroundWriteAsync(byte[] keyBytes, byte[] valueBytes, ByteArrayWrapper keyWrapper, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Check if cache value has changed
                if (!_cache.TryGetValue(keyWrapper, out var currentCached) || !currentCached.SequenceEqual(valueBytes))
                {
                    Console.WriteLine($"Cache updated for key, stopping background write: {Encoding.UTF8.GetString(keyBytes)}");
                    return;
                }

                try
                {
                    var success = await _db.PutBytesAsync(keyBytes, valueBytes);
                    if (success)
                    {
                        Console.WriteLine($"Successfully updated key on PWR Chain: {Encoding.UTF8.GetString(keyBytes)}");
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"Failed to update key on PWR Chain, retrying: {Encoding.UTF8.GetString(keyBytes)}");

                        // Check if another task has already updated the value
                        try
                        {
                            var remoteValue = await _db.GetValueBytesAsync(keyBytes);
                            if (remoteValue.SequenceEqual(valueBytes))
                            {
                                Console.WriteLine($"Value already updated by another process: {Encoding.UTF8.GetString(keyBytes)}");
                                return;
                            }
                        }
                        catch
                        {
                            // Ignore errors when checking remote value
                        }

                        await Task.Delay(10, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating key on PWR Chain: {Encoding.UTF8.GetString(keyBytes)} - {ex.Message}");
                    
                    // Wait 10ms before retry
                    await Task.Delay(10, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error in background write: {ex.Message}");
        }
    }

    public async Task<byte[]?> GetValueAsync(object key)
    {
        var keyBytes = ToBytes(key);
        return await GetValueBytesAsync(keyBytes);
    }

    public async Task<byte[]?> GetValueBytesAsync(byte[] key)
    {
        var keyWrapper = new ByteArrayWrapper(key);

        // Check cache first
        if (_cache.TryGetValue(keyWrapper, out var cachedValue))
        {
            return cachedValue;
        }

        // If not in cache, fetch from remote
        try
        {
            var value = await _db.GetValueBytesAsync(key);
            if (value != null)
            {
                // Cache the retrieved value
                _cache.TryAdd(keyWrapper, value);
            }
            return value;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving value: {ex.Message}");
            return null;
        }
    }

    public byte[]? GetValue(object key)
    {
        return GetValueAsync(key).GetAwaiter().GetResult();
    }

    public byte[]? GetValueBytes(byte[] key)
    {
        return GetValueBytesAsync(key).GetAwaiter().GetResult();
    }

    public async Task<string?> GetStringValueAsync(object key)
    {
        var value = await GetValueAsync(key);
        return value != null ? Encoding.UTF8.GetString(value) : null;
    }

    public string? GetStringValue(object key)
    {
        return GetStringValueAsync(key).GetAwaiter().GetResult();
    }

    public async Task<int?> GetIntValueAsync(object key)
    {
        var value = await GetValueAsync(key);
        if (value == null) return null;
        
        var str = Encoding.UTF8.GetString(value);
        return int.TryParse(str, out var result) ? result : null;
    }

    public int? GetIntValue(object key)
    {
        return GetIntValueAsync(key).GetAwaiter().GetResult();
    }

    public async Task<long?> GetLongValueAsync(object key)
    {
        var value = await GetValueAsync(key);
        if (value == null) return null;
        
        var str = Encoding.UTF8.GetString(value);
        return long.TryParse(str, out var result) ? result : null;
    }

    public long? GetLongValue(object key)
    {
        return GetLongValueAsync(key).GetAwaiter().GetResult();
    }

    public async Task<double?> GetDoubleValueAsync(object key)
    {
        var value = await GetValueAsync(key);
        if (value == null) return null;
        
        var str = Encoding.UTF8.GetString(value);
        return double.TryParse(str, out var result) ? result : null;
    }

    public double? GetDoubleValue(object key)
    {
        return GetDoubleValueAsync(key).GetAwaiter().GetResult();
    }

    public async Task ShutdownAsync()
    {
        Console.WriteLine("Shutting down PowerKvCached...");
        _isShutdown = true;

        // Cancel all background tasks
        _cancellationTokenSource.Cancel();

        var activeTasks = _activeTasks.Where(t => !t.IsCompleted).ToArray();
        
        if (activeTasks.Length > 0)
        {
            Console.WriteLine($"Waiting for {activeTasks.Length} background writes to complete...");
            
            try
            {
                await Task.WhenAll(activeTasks).WaitAsync(TimeSpan.FromSeconds(60));
                Console.WriteLine("All background writes completed");
            }
            catch (TimeoutException)
            {
                Console.WriteLine($"Forced shutdown with {activeTasks.Count(t => !t.IsCompleted)} writes still active");
            }
        }
        else
        {
            Console.WriteLine("All background writes completed");
        }
    }

    public void Shutdown()
    {
        ShutdownAsync().GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            if (!_isShutdown)
            {
                Shutdown();
            }
            
            _cancellationTokenSource?.Dispose();
            _db?.Dispose();
            _disposed = true;
        }
    }
}
