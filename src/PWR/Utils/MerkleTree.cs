using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using LiteDB;
using Org.BouncyCastle.Crypto.Digests;

namespace PWR.Utils.MerkleTree;

// Error types
public class MerkleTreeException : Exception
{
    public MerkleTreeException(string message) : base(message) { }
    public MerkleTreeException(string message, Exception innerException) : base(message, innerException) { }
}

public class DatabaseException : MerkleTreeException
{
    public DatabaseException(string message) : base(message) { }
    public DatabaseException(string message, Exception innerException) : base(message, innerException) { }
}

public class InvalidArgumentException : MerkleTreeException
{
    public InvalidArgumentException(string message) : base(message) { }
}

public class IllegalStateException : MerkleTreeException
{
    public IllegalStateException(string message) : base(message) { }
}

// Database models for LiteDB
public class NodeRecord
{
    public string Id { get; set; } = string.Empty; // hex hash
    public byte[] Data { get; set; } = Array.Empty<byte>();
}

public class KeyDataRecord
{
    public string Id { get; set; } = string.Empty; // hex key
    public byte[] Data { get; set; } = Array.Empty<byte>();
}

public class MetadataRecord
{
    public string Id { get; set; } = string.Empty;
    public byte[]? ByteValue { get; set; }
    public int? IntValue { get; set; }
}

// Utility wrapper for byte arrays to use as Dictionary keys
public class ByteArrayWrapper : IEquatable<ByteArrayWrapper>
{
    public byte[] Data { get; }
    private readonly int _hashCode;

    public ByteArrayWrapper(byte[] data)
    {
        Data = new byte[data.Length];
        Array.Copy(data, Data, data.Length);
        _hashCode = ComputeHashCode(Data);
    }

    private static int ComputeHashCode(byte[] data)
    {
        unchecked
        {
            int hash = 17;
            foreach (byte b in data)
            {
                hash = hash * 31 + b;
            }
            return hash;
        }
    }

    public bool Equals(ByteArrayWrapper? other)
    {
        if (other == null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (Data.Length != other.Data.Length) return false;

        for (int i = 0; i < Data.Length; i++)
        {
            if (Data[i] != other.Data[i]) return false;
        }
        return true;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ByteArrayWrapper);
    }

    public override int GetHashCode()
    {
        return _hashCode;
    }
}

// Node structure
public class Node
{
    public byte[] Hash { get; set; }
    public byte[]? Left { get; set; }
    public byte[]? Right { get; set; }
    public byte[]? Parent { get; set; }
    public byte[]? NodeHashToRemoveFromDb { get; set; }

    // Constants
    private const int HASH_LENGTH = 32;

    // Private constructor for internal use
    private Node(byte[] hash, byte[]? left = null, byte[]? right = null, byte[]? parent = null)
    {
        if (hash == null || hash.Length == 0)
            throw new InvalidArgumentException("Node hash cannot be empty");

        Hash = new byte[hash.Length];
        Array.Copy(hash, Hash, hash.Length);

        Left = left != null ? CopyBytes(left) : null;
        Right = right != null ? CopyBytes(right) : null;
        Parent = parent != null ? CopyBytes(parent) : null;
        NodeHashToRemoveFromDb = null;
    }

    // Construct a leaf node with a known hash
    public static Node NewLeaf(byte[] hash)
    {
        if (hash == null || hash.Length == 0)
            throw new InvalidArgumentException("Node hash cannot be empty");

        return new Node(hash);
    }

    // Construct a node with all fields
    public static Node NewWithFields(byte[] hash, byte[]? left, byte[]? right, byte[]? parent)
    {
        if (hash == null || hash.Length == 0)
            throw new InvalidArgumentException("Node hash cannot be empty");

        return new Node(hash, left, right, parent);
    }

    // Construct a node (non-leaf) with left and right hashes, auto-calculate node hash
    public static Node NewInternal(byte[]? left, byte[]? right)
    {
        if (left == null && right == null)
            throw new InvalidArgumentException("At least one of left or right hash must be non-null");

        byte[] hash = CalculateHashStatic(left, right);
        return new Node(hash, left, right);
    }

    // Calculate hash based on left and right child hashes
    private static byte[] CalculateHashStatic(byte[]? left, byte[]? right)
    {
        if (left == null && right == null)
            throw new InvalidArgumentException("Cannot calculate hash with no children");

        byte[] leftHash = left ?? right!;
        byte[] rightHash = right ?? left!;

        return Keccac256TwoInputs(leftHash, rightHash);
    }

    public byte[] CalculateHash()
    {
        return CalculateHashStatic(Left, Right);
    }

    // Encode the node into bytes for storage
    public byte[] Encode()
    {
        bool hasLeft = Left != null;
        bool hasRight = Right != null;
        bool hasParent = Parent != null;

        using (var ms = new MemoryStream())
        {
            // Add hash
            ms.Write(Hash, 0, Hash.Length);

            // Add flags
            ms.WriteByte((byte)(hasLeft ? 1 : 0));
            ms.WriteByte((byte)(hasRight ? 1 : 0));
            ms.WriteByte((byte)(hasParent ? 1 : 0));

            // Add optional fields
            if (hasLeft)
                ms.Write(Left!, 0, Left!.Length);
            if (hasRight)
                ms.Write(Right!, 0, Right!.Length);
            if (hasParent)
                ms.Write(Parent!, 0, Parent!.Length);

            return ms.ToArray();
        }
    }

    // Decode a node from bytes
    public static Node Decode(byte[] data)
    {
        if (data.Length < HASH_LENGTH + 3)
            throw new InvalidArgumentException("Invalid encoded data length");

        int offset = 0;

        // Read hash
        byte[] hash = new byte[HASH_LENGTH];
        Array.Copy(data, offset, hash, 0, HASH_LENGTH);
        offset += HASH_LENGTH;

        // Read flags
        bool hasLeft = data[offset++] == 1;
        bool hasRight = data[offset++] == 1;
        bool hasParent = data[offset++] == 1;

        // Read optional fields
        byte[]? left = null;
        if (hasLeft)
        {
            left = new byte[HASH_LENGTH];
            Array.Copy(data, offset, left, 0, HASH_LENGTH);
            offset += HASH_LENGTH;
        }

        byte[]? right = null;
        if (hasRight)
        {
            right = new byte[HASH_LENGTH];
            Array.Copy(data, offset, right, 0, HASH_LENGTH);
            offset += HASH_LENGTH;
        }

        byte[]? parent = null;
        if (hasParent)
        {
            parent = new byte[HASH_LENGTH];
            Array.Copy(data, offset, parent, 0, HASH_LENGTH);
        }

        return new Node(hash, left, right, parent);
    }

    public void SetParentNodeHash(byte[]? parentHash)
    {
        Parent = parentHash != null ? CopyBytes(parentHash) : null;
    }

    public void UpdateLeaf(byte[] oldLeafHash, byte[] newLeafHash)
    {
        if (Left != null && ByteArraysEqual(Left, oldLeafHash))
        {
            Left = CopyBytes(newLeafHash);
            return;
        }

        if (Right != null && ByteArraysEqual(Right, oldLeafHash))
        {
            Right = CopyBytes(newLeafHash);
            return;
        }

        throw new InvalidArgumentException("Old hash not found among this node's children");
    }

    public void AddLeaf(byte[] leafHash)
    {
        if (Left == null)
        {
            Left = CopyBytes(leafHash);
        }
        else if (Right == null)
        {
            Right = CopyBytes(leafHash);
        }
        else
        {
            throw new InvalidArgumentException("Node already has both left and right children");
        }
    }

    private static byte[]? CopyBytes(byte[]? source)
    {
        if (source == null) return null;
        byte[] copy = new byte[source.Length];
        Array.Copy(source, copy, source.Length);
        return copy;
    }

    private static bool ByteArraysEqual(byte[]? a, byte[]? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        if (a.Length != b.Length) return false;

        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i]) return false;
        }
        return true;
    }

    // Proper Keccac-256 hash function using BouncyCastle
    private static byte[] Keccac256TwoInputs(byte[] input1, byte[] input2)
    {
        var digest = new KeccakDigest(256);
        var output = new byte[digest.GetDigestSize()];
        
        digest.BlockUpdate(input1, 0, input1.Length);
        digest.BlockUpdate(input2, 0, input2.Length);
        digest.DoFinal(output, 0);
        
        return output;
    }
}

// Global registry of open trees
public static class OpenTrees
{
    private static readonly ConcurrentDictionary<string, MerkleTree> _openTrees = 
        new ConcurrentDictionary<string, MerkleTree>();

    public static bool TryAdd(string name, MerkleTree tree)
    {
        return _openTrees.TryAdd(name, tree);
    }

    public static bool TryRemove(string name)
    {
        return _openTrees.TryRemove(name, out _);
    }

    public static bool ContainsKey(string name)
    {
        return _openTrees.ContainsKey(name);
    }
}

// Main MerkleTree class
public class MerkleTree : IDisposable
{
    // Constants
    private const int HASH_LENGTH = 32;

    // Metadata keys
    private const string KEY_ROOT_HASH = "rootHash";
    private const string KEY_NUM_LEAVES = "numLeaves";
    private const string KEY_DEPTH = "depth";
    private const string KEY_HANGING_NODE_PREFIX = "hangingNode";

    private readonly string _treeName;
    private readonly string _path;
    private readonly LiteDatabase _db;

    // Caches
    private readonly ConcurrentDictionary<ByteArrayWrapper, Node> _nodesCache;
    private readonly ConcurrentDictionary<int, byte[]> _hangingNodes;
    private readonly ConcurrentDictionary<ByteArrayWrapper, byte[]> _keyDataCache;

    // Metadata
    private int _numLeaves;
    private int _depth;
    private byte[]? _rootHash;

    // State
    private volatile bool _closed;
    private volatile bool _hasUnsavedChanges;

    // Thread safety
    private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

    public MerkleTree(string treeName)
    {
        if (OpenTrees.ContainsKey(treeName))
            throw new IllegalStateException("There is already an open instance of this tree");

        _treeName = treeName;
        _path = Path.Combine("merkleTree", treeName);

        // Create directory
        Directory.CreateDirectory(_path);

        // Initialize database
        var dbPath = Path.Combine(_path, "tree.db");
        _db = new LiteDatabase($"Filename={dbPath};Connection=shared");

        _nodesCache = new ConcurrentDictionary<ByteArrayWrapper, Node>();
        _hangingNodes = new ConcurrentDictionary<int, byte[]>();
        _keyDataCache = new ConcurrentDictionary<ByteArrayWrapper, byte[]>();

        _numLeaves = 0;
        _depth = 0;
        _rootHash = null;
        _closed = false;
        _hasUnsavedChanges = false;

        // Load metadata
        LoadMetadata();

        // Register instance
        if (!OpenTrees.TryAdd(treeName, this))
            throw new IllegalStateException("Failed to register tree instance");
    }

    private void LoadMetadata()
    {
        try
        {
            var metadataCol = _db.GetCollection<MetadataRecord>("metadata");

            // Load root hash
            var rootRecord = metadataCol.FindById(KEY_ROOT_HASH);
            _rootHash = rootRecord?.ByteValue;

            // Load num leaves
            var numLeavesRecord = metadataCol.FindById(KEY_NUM_LEAVES);
            _numLeaves = numLeavesRecord?.IntValue ?? 0;

            // Load depth
            var depthRecord = metadataCol.FindById(KEY_DEPTH);
            _depth = depthRecord?.IntValue ?? 0;

            // Load hanging nodes
            _hangingNodes.Clear();
            for (int i = 0; i <= _depth; i++)
            {
                string key = KEY_HANGING_NODE_PREFIX + i;
                var record = metadataCol.FindById(key);
                if (record?.ByteValue != null)
                {
                    _hangingNodes.TryAdd(i, record.ByteValue);
                }
            }
        }
        catch (Exception ex)
        {
            throw new DatabaseException($"Failed to load metadata: {ex.Message}", ex);
        }
    }

    private void ErrorIfClosed()
    {
        if (_closed)
            throw new IllegalStateException("MerkleTree is closed");
    }

    public byte[]? GetRootHash()
    {
        ErrorIfClosed();
        _lock.EnterReadLock();
        try
        {
            return _rootHash != null ? CopyBytes(_rootHash) : null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public int GetNumLeaves()
    {
        ErrorIfClosed();
        _lock.EnterReadLock();
        try
        {
            return _numLeaves;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public int GetDepth()
    {
        ErrorIfClosed();
        _lock.EnterReadLock();
        try
        {
            return _depth;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public byte[]? GetData(byte[] key)
    {
        ErrorIfClosed();
        if (key == null || key.Length == 0)
            throw new InvalidArgumentException("Key cannot be empty");

        _lock.EnterReadLock();
        try
        {
            // Check cache first
            var keyWrapper = new ByteArrayWrapper(key);
            if (_keyDataCache.TryGetValue(keyWrapper, out byte[]? cachedData))
            {
                return CopyBytes(cachedData);
            }

            // Check database
            var keyDataCol = _db.GetCollection<KeyDataRecord>("keyData");
            var record = keyDataCol.FindById(ToHexString(key));
            return record?.Data != null ? CopyBytes(record.Data) : null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void AddOrUpdateData(byte[] key, byte[] data)
    {
        ErrorIfClosed();
        if (key == null || key.Length == 0)
            throw new InvalidArgumentException("Key cannot be empty");
        if (data == null || data.Length == 0)
            throw new InvalidArgumentException("Data cannot be empty");

        _lock.EnterWriteLock();
        try
        {
            var existingData = GetDataInternal(key);
            byte[]? oldLeafHash = existingData != null ? CalculateLeafHash(key, existingData) : null;
            byte[] newLeafHash = CalculateLeafHash(key, data);

            if (oldLeafHash != null && ByteArraysEqual(oldLeafHash, newLeafHash))
            {
                return; // No change needed
            }

            // Store key-data mapping in cache
            var keyWrapper = new ByteArrayWrapper(key);
            _keyDataCache.AddOrUpdate(keyWrapper, CopyBytes(data)!, (k, v) => CopyBytes(data)!);
            _hasUnsavedChanges = true;

            if (oldLeafHash == null)
            {
                // Add new leaf
                var leafNode = Node.NewLeaf(newLeafHash);
                AddLeaf(leafNode);
            }
            else
            {
                // Update existing leaf
                UpdateLeaf(oldLeafHash, newLeafHash);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private byte[]? GetDataInternal(byte[] key)
    {
        // Check cache first
        var keyWrapper = new ByteArrayWrapper(key);
        if (_keyDataCache.TryGetValue(keyWrapper, out byte[]? cachedData))
        {
            return cachedData;
        }

        // Check database
        var keyDataCol = _db.GetCollection<KeyDataRecord>("keyData");
        var record = keyDataCol.FindById(ToHexString(key));
        return record?.Data;
    }

    private void AddLeaf(Node leafNode)
    {
        if (_numLeaves == 0)
        {
            // First leaf becomes root and hanging at level 0
            _hangingNodes.AddOrUpdate(0, CopyBytes(leafNode.Hash)!, (k, v) => CopyBytes(leafNode.Hash)!);
            _rootHash = CopyBytes(leafNode.Hash);
            _numLeaves++;
            UpdateNodeInCache(leafNode);
            return;
        }

        // Check if there's a hanging leaf at level 0
        if (_hangingNodes.TryGetValue(0, out byte[]? hangingLeafHash))
        {
            var hangingLeaf = GetNodeByHash(hangingLeafHash);

            if (hangingLeaf != null)
            {
                // Remove from hanging nodes at level 0
                _hangingNodes.TryRemove(0, out _);

                if (hangingLeaf.Parent == null)
                {
                    // Hanging leaf is the root - create parent with both leaves
                    var parentNode = Node.NewInternal(CopyBytes(hangingLeafHash), CopyBytes(leafNode.Hash));

                    // Update parent references for both leaves
                    hangingLeaf.SetParentNodeHash(parentNode.Hash);
                    UpdateNodeInCache(hangingLeaf);

                    leafNode.SetParentNodeHash(parentNode.Hash);
                    UpdateNodeInCache(leafNode);

                    // Add parent node at level 1
                    AddNode(1, parentNode);
                }
                else
                {
                    // Hanging leaf has a parent - add new leaf to that parent
                    var parentNode = GetNodeByHash(hangingLeaf.Parent);
                    if (parentNode == null)
                        throw new IllegalStateException("Parent node not found");

                    parentNode.AddLeaf(leafNode.Hash);

                    // Update new leaf's parent reference
                    leafNode.SetParentNodeHash(hangingLeaf.Parent);
                    UpdateNodeInCache(leafNode);

                    // Recalculate parent hash and update
                    var newParentHash = parentNode.CalculateHash();
                    UpdateNodeHash(parentNode, newParentHash);
                }
            }
        }
        else
        {
            // No hanging leaf at level 0 - make this leaf hanging
            _hangingNodes.AddOrUpdate(0, CopyBytes(leafNode.Hash)!, (k, v) => CopyBytes(leafNode.Hash)!);

            // Create a parent node with just this leaf and add it to level 1
            var parentNode = Node.NewInternal(CopyBytes(leafNode.Hash), null);
            leafNode.SetParentNodeHash(parentNode.Hash);
            UpdateNodeInCache(leafNode);

            AddNode(1, parentNode);
        }

        _numLeaves++;
        UpdateNodeInCache(leafNode);
    }

    private void AddNode(int level, Node node)
    {
        // Update depth if necessary
        if (level > _depth)
        {
            _depth = level;
        }

        // Get hanging node at this level
        if (_hangingNodes.TryGetValue(level, out byte[]? hangingNodeHash))
        {
            var hangingNode = GetNodeByHash(hangingNodeHash);

            if (hangingNode != null)
            {
                // Remove hanging node from this level
                _hangingNodes.TryRemove(level, out _);

                if (hangingNode.Parent == null)
                {
                    // Hanging node is a root - create parent with both nodes
                    var parent = Node.NewInternal(CopyBytes(hangingNodeHash), CopyBytes(node.Hash));

                    // Update parent references
                    hangingNode.SetParentNodeHash(parent.Hash);
                    UpdateNodeInCache(hangingNode);

                    node.SetParentNodeHash(parent.Hash);
                    UpdateNodeInCache(node);

                    // Recursively add parent at next level
                    AddNode(level + 1, parent);
                }
                else
                {
                    // Hanging node has a parent - add new node to that parent
                    var parentNode = GetNodeByHash(hangingNode.Parent);
                    if (parentNode == null)
                        throw new IllegalStateException("Parent node not found");

                    parentNode.AddLeaf(node.Hash);

                    // Update new node's parent reference
                    node.SetParentNodeHash(hangingNode.Parent);
                    UpdateNodeInCache(node);

                    // Recalculate parent hash and update
                    var newParentHash = parentNode.CalculateHash();
                    UpdateNodeHash(parentNode, newParentHash);
                }
            }
        }
        else
        {
            // No hanging node at this level - make this node hanging
            _hangingNodes.AddOrUpdate(level, CopyBytes(node.Hash)!, (k, v) => CopyBytes(node.Hash)!);

            // If this is at or above the current depth, it becomes the new root
            if (level >= _depth)
            {
                _rootHash = CopyBytes(node.Hash);
            }
            else
            {
                // Create a parent node and continue up
                var parentNode = Node.NewInternal(CopyBytes(node.Hash), null);
                node.SetParentNodeHash(parentNode.Hash);
                UpdateNodeInCache(node);

                AddNode(level + 1, parentNode);
            }
        }

        UpdateNodeInCache(node);
    }

    private void UpdateLeaf(byte[] oldLeafHash, byte[] newLeafHash)
    {
        if (ByteArraysEqual(oldLeafHash, newLeafHash))
            throw new InvalidArgumentException("Old and new leaf hashes cannot be the same");

        var leaf = GetNodeByHash(oldLeafHash);
        if (leaf == null)
            throw new InvalidArgumentException("Leaf not found");

        UpdateNodeHash(leaf, newLeafHash);
    }

    private void UpdateNodeHash(Node node, byte[] newHash)
    {
        if (node.NodeHashToRemoveFromDb == null)
        {
            node.NodeHashToRemoveFromDb = CopyBytes(node.Hash);
        }

        byte[] oldHash = CopyBytes(node.Hash)!;
        node.Hash = CopyBytes(newHash)!;

        // Update hanging nodes
        foreach (var kvp in _hangingNodes)
        {
            if (ByteArraysEqual(kvp.Value, oldHash))
            {
                _hangingNodes.TryUpdate(kvp.Key, CopyBytes(newHash)!, kvp.Value);
                break;
            }
        }

        // Update cache
        var oldHashWrapper = new ByteArrayWrapper(oldHash);
        var newHashWrapper = new ByteArrayWrapper(newHash);
        _nodesCache.TryRemove(oldHashWrapper, out _);
        _nodesCache.AddOrUpdate(newHashWrapper, node, (k, v) => node);

        // Handle different node types
        bool isLeaf = node.Left == null && node.Right == null;
        bool isRoot = node.Parent == null;

        // If this is the root node, update the root hash
        if (isRoot)
        {
            _rootHash = CopyBytes(newHash);

            // Update children's parent references
            if (node.Left != null)
            {
                var leftNode = GetNodeByHash(node.Left);
                if (leftNode != null)
                {
                    leftNode.SetParentNodeHash(newHash);
                    UpdateNodeInCache(leftNode);
                }
            }

            if (node.Right != null)
            {
                var rightNode = GetNodeByHash(node.Right);
                if (rightNode != null)
                {
                    rightNode.SetParentNodeHash(newHash);
                    UpdateNodeInCache(rightNode);
                }
            }
        }

        // If this is a leaf node with a parent, update the parent
        if (isLeaf && !isRoot)
        {
            if (node.Parent != null)
            {
                var parentNode = GetNodeByHash(node.Parent);
                if (parentNode != null)
                {
                    parentNode.UpdateLeaf(oldHash, newHash);
                    var newParentHash = parentNode.CalculateHash();
                    UpdateNodeHash(parentNode, newParentHash);
                }
            }
        }
        // If this is an internal node with a parent, update the parent and children
        else if (!isLeaf && !isRoot)
        {
            // Update children's parent references
            if (node.Left != null)
            {
                var leftNode = GetNodeByHash(node.Left);
                if (leftNode != null)
                {
                    leftNode.SetParentNodeHash(newHash);
                    UpdateNodeInCache(leftNode);
                }
            }
            if (node.Right != null)
            {
                var rightNode = GetNodeByHash(node.Right);
                if (rightNode != null)
                {
                    rightNode.SetParentNodeHash(newHash);
                    UpdateNodeInCache(rightNode);
                }
            }

            // Update parent
            if (node.Parent != null)
            {
                var parentNode = GetNodeByHash(node.Parent);
                if (parentNode != null)
                {
                    parentNode.UpdateLeaf(oldHash, newHash);
                    var newParentHash = parentNode.CalculateHash();
                    UpdateNodeHash(parentNode, newParentHash);
                }
            }
        }
    }

    private Node? GetNodeByHash(byte[]? hash)
    {
        if (hash == null || hash.Length == 0)
            return null;

        // Check cache first
        var hashWrapper = new ByteArrayWrapper(hash);
        if (_nodesCache.TryGetValue(hashWrapper, out Node? cachedNode))
        {
            return cachedNode;
        }

        // Check database
        var nodesCol = _db.GetCollection<NodeRecord>("nodes");
        var record = nodesCol.FindById(ToHexString(hash));

        if (record?.Data != null)
        {
            var node = Node.Decode(record.Data);
            // Add to cache
            _nodesCache.TryAdd(hashWrapper, node);
            return node;
        }

        return null;
    }

    private void UpdateNodeInCache(Node node)
    {
        var hashWrapper = new ByteArrayWrapper(node.Hash);
        _nodesCache.AddOrUpdate(hashWrapper, node, (k, v) => node);
    }

    public void FlushToDisk()
    {
        if (!_hasUnsavedChanges)
            return;

        ErrorIfClosed();

        _lock.EnterWriteLock();
        try
        {
            var metadataCol = _db.GetCollection<MetadataRecord>("metadata");
            var nodesCol = _db.GetCollection<NodeRecord>("nodes");
            var keyDataCol = _db.GetCollection<KeyDataRecord>("keyData");

            // Write metadata
            if (_rootHash != null)
            {
                metadataCol.Upsert(new MetadataRecord { Id = KEY_ROOT_HASH, ByteValue = _rootHash });
            }

            metadataCol.Upsert(new MetadataRecord { Id = KEY_NUM_LEAVES, IntValue = _numLeaves });
            metadataCol.Upsert(new MetadataRecord { Id = KEY_DEPTH, IntValue = _depth });

            // Write hanging nodes
            foreach (var kvp in _hangingNodes)
            {
                string key = KEY_HANGING_NODE_PREFIX + kvp.Key;
                metadataCol.Upsert(new MetadataRecord { Id = key, ByteValue = kvp.Value });
            }

            // Write nodes
            foreach (var kvp in _nodesCache)
            {
                var node = kvp.Value;
                var nodeRecord = new NodeRecord 
                { 
                    Id = ToHexString(node.Hash), 
                    Data = node.Encode() 
                };
                nodesCol.Upsert(nodeRecord);

                if (node.NodeHashToRemoveFromDb != null)
                {
                    nodesCol.Delete(ToHexString(node.NodeHashToRemoveFromDb));
                }
            }

            // Write key data
            foreach (var kvp in _keyDataCache)
            {
                var keyDataRecord = new KeyDataRecord 
                { 
                    Id = ToHexString(kvp.Key.Data), 
                    Data = kvp.Value 
                };
                keyDataCol.Upsert(keyDataRecord);
            }

            // Clear caches
            _nodesCache.Clear();
            _keyDataCache.Clear();

            _hasUnsavedChanges = false;
        }
        catch (Exception ex)
        {
            throw new DatabaseException($"Failed to flush to disk: {ex.Message}", ex);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Close()
    {
        if (_closed)
            return;

        _lock.EnterWriteLock();
        try
        {
            FlushToDisk();
            _closed = true;
            OpenTrees.TryRemove(_treeName);
            _db?.Dispose();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Clear()
    {
        ErrorIfClosed();

        _lock.EnterWriteLock();
        try
        {
            var metadataCol = _db.GetCollection<MetadataRecord>("metadata");
            var nodesCol = _db.GetCollection<NodeRecord>("nodes");
            var keyDataCol = _db.GetCollection<KeyDataRecord>("keyData");

            // Clear all collections
            metadataCol.DeleteAll();
            nodesCol.DeleteAll();
            keyDataCol.DeleteAll();

            // Reset in-memory state
            _nodesCache.Clear();
            _keyDataCache.Clear();
            _hangingNodes.Clear();

            _rootHash = null;
            _numLeaves = 0;
            _depth = 0;
            _hasUnsavedChanges = false;
        }
        catch (Exception ex)
        {
            throw new DatabaseException($"Failed to clear tree: {ex.Message}", ex);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public bool ContainsKey(byte[] key)
    {
        ErrorIfClosed();
        if (key == null || key.Length == 0)
            throw new InvalidArgumentException("Key cannot be empty");

        _lock.EnterReadLock();
        try
        {
            var keyDataCol = _db.GetCollection<KeyDataRecord>("keyData");
            var record = keyDataCol.FindById(ToHexString(key));
            return record != null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void RevertUnsavedChanges()
    {
        if (!_hasUnsavedChanges)
            return;

        ErrorIfClosed();

        _lock.EnterWriteLock();
        try
        {
            // Clear caches
            _nodesCache.Clear();
            _hangingNodes.Clear();
            _keyDataCache.Clear();

            // Reload metadata from disk
            LoadMetadata();

            _hasUnsavedChanges = false;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public byte[]? GetRootHashSavedOnDisk()
    {
        ErrorIfClosed();

        _lock.EnterReadLock();
        try
        {
            var metadataCol = _db.GetCollection<MetadataRecord>("metadata");
            var record = metadataCol.FindById(KEY_ROOT_HASH);
            return record?.ByteValue;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    // Utility functions
    public static byte[] CalculateLeafHash(byte[] key, byte[] data)
    {
        return Keccac256TwoInputs(key, data);
    }

    private static byte[] Keccac256TwoInputs(byte[] input1, byte[] input2)
    {
        var digest = new KeccakDigest(256);
        var output = new byte[digest.GetDigestSize()];
        
        digest.BlockUpdate(input1, 0, input1.Length);
        digest.BlockUpdate(input2, 0, input2.Length);
        digest.DoFinal(output, 0);
        
        return output;
    }

    private static string ToHexString(byte[] bytes)
    {
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static byte[]? CopyBytes(byte[]? source)
    {
        if (source == null) return null;
        byte[] copy = new byte[source.Length];
        Array.Copy(source, copy, source.Length);
        return copy;
    }

    private static bool ByteArraysEqual(byte[]? a, byte[]? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        if (a.Length != b.Length) return false;

        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i]) return false;
        }
        return true;
    }

    public void Dispose()
    {
        Close();
        _lock?.Dispose();
    }
}
