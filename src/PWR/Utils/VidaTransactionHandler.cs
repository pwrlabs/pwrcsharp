using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using PWR.Models;

namespace PWR.Utils
{
    /// <summary>
    /// Functional interface for handling VIDA transactions
    /// </summary>
    public delegate void VidaTransactionHandler(VidaDataTransaction transaction);

    /// <summary>
    /// Delegate for saving block numbers to external storage (supports both sync and async)
    /// </summary>
    /// <example>
    /// // Using with synchronous function:
    /// var subscription = new VidaTransactionSubscription(rpc, vidaId, startBlock, handler, 100, 
    ///     BlockSaverHelper.FromSync(blockNumber => File.WriteAllText("block.txt", blockNumber.ToString())));
    /// 
    /// // Using with asynchronous function:
    /// var subscription = new VidaTransactionSubscription(rpc, vidaId, startBlock, handler, 100,
    ///     BlockSaverHelper.FromAsync(async blockNumber => await SaveToDbAsync(blockNumber)));
    /// 
    /// // Direct async lambda:
    /// var subscription = new VidaTransactionSubscription(rpc, vidaId, startBlock, handler, 100,
    ///     async blockNumber => { await SaveToDbAsync(blockNumber); });
    /// </example>
    public delegate Task BlockSaver(ulong blockNumber);

    /// <summary>
    /// Helper class for creating BlockSaver delegates from both sync and async functions
    /// </summary>
    public static class BlockSaverHelper
    {
        /// <summary>
        /// Creates a BlockSaver from a synchronous function
        /// </summary>
        /// <param name="syncAction">The synchronous action to wrap</param>
        /// <returns>A BlockSaver delegate</returns>
        public static BlockSaver FromSync(Action<ulong> syncAction)
        {
            return blockNumber =>
            {
                syncAction(blockNumber);
                return Task.CompletedTask;
            };
        }

        /// <summary>
        /// Creates a BlockSaver from an asynchronous function
        /// </summary>
        /// <param name="asyncFunc">The asynchronous function to wrap</param>
        /// <returns>A BlockSaver delegate</returns>
        public static BlockSaver FromAsync(Func<ulong, Task> asyncFunc)
        {
            return blockNumber => asyncFunc(blockNumber);
        }
    }

    /// <summary>
    /// Handles subscription to VIDA transactions for a specific VIDA
    /// </summary>
    public class VidaTransactionSubscription
    {
        private readonly RPC _pwrSdk;
        private readonly ulong _vidaId;
        private readonly ulong _startingBlock;
        private readonly VidaTransactionHandler _handler;
        private readonly int _pollInterval;
        private readonly BlockSaver? _blockSaver;

        // Atomic state management using volatile booleans and proper locking
        private volatile bool _wantsToPause;
        private volatile bool _stop;
        private volatile bool _paused;
        private volatile bool _running;
        private ulong _latestCheckedBlock;

        private readonly object _lockObject = new object();

        /// <summary>
        /// Creates a new VIDA transaction subscription with block persistence support
        /// </summary>
        /// <param name="pwrSdk">The PWR SDK instance</param>
        /// <param name="vidaId">The VIDA ID to subscribe to</param>
        /// <param name="startingBlock">The block number to start checking from</param>
        /// <param name="handler">The handler for processing transactions</param>
        /// <param name="pollInterval">Interval in milliseconds between polling for new blocks</param>
        /// <param name="blockSaver">Optional callback for saving latest block number to external storage</param>
        public VidaTransactionSubscription(RPC pwrSdk, ulong vidaId, ulong startingBlock, VidaTransactionHandler handler, int pollInterval = 100, BlockSaver? blockSaver = null)
        {
            _pwrSdk = pwrSdk ?? throw new ArgumentNullException(nameof(pwrSdk));
            _vidaId = vidaId;
            _startingBlock = startingBlock;
            _latestCheckedBlock = startingBlock;
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _pollInterval = pollInterval;
            _blockSaver = blockSaver;

            // Initialize state
            _wantsToPause = false;
            _stop = false;
            _paused = false;
            _running = false;

            // Add shutdown hook equivalent for C#
            Console.CancelKeyPress += (sender, e) => {
                Console.WriteLine($"Shutting down VidaTransactionSubscription for VIDA-ID: {_vidaId}");
                Pause();
                Console.WriteLine($"VidaTransactionSubscription for VIDA-ID: {_vidaId} has been stopped.");
            };

            AppDomain.CurrentDomain.ProcessExit += (sender, e) => {
                Console.WriteLine($"Process exiting - shutting down VidaTransactionSubscription for VIDA-ID: {_vidaId}");
                Pause();
            };
        }

        /// <summary>
        /// Starts the subscription process with synchronized access
        /// </summary>
        public void Start()
        {
            lock (_lockObject)
            {
                if (_running)
                {
                    Console.WriteLine("ERROR: VidaTransactionSubscription is already running");
                    return;
                }

                _running = true;
                _wantsToPause = false;
                _stop = false;
                _paused = false;
            }

            _latestCheckedBlock = _startingBlock - 1;

            Thread thread = new Thread(() =>
            {
                while (!_stop)
                {
                    if (_wantsToPause)
                    {
                        if (!_paused)
                        {
                            _paused = true;
                        }
                        Thread.Sleep(10);
                        continue;
                    }

                    if (_paused)
                    {
                        _paused = false;
                    }

                    try
                    {
                        ulong latestBlock = Task.Run(async () => 
                            await _pwrSdk.GetLatestBlockNumber()).Result;

                        if (latestBlock == _latestCheckedBlock)
                        {
                            Thread.Sleep(_pollInterval);
                            continue;
                        }

                        ulong maxBlockToCheck = Math.Min(latestBlock, _latestCheckedBlock + 1000);

                        List<VidaDataTransaction> transactions = Task.Run(async () => 
                            await _pwrSdk.GetVidaDataTransactions(_latestCheckedBlock + 1, maxBlockToCheck, _vidaId)).Result;

                        foreach (var transaction in transactions)
                        {
                            try
                            {
                                _handler(transaction);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to process VIDA transaction: {transaction.Hash} - {ex.Message}");
                                Console.WriteLine(ex.StackTrace);
                            }
                        }

                        _latestCheckedBlock = maxBlockToCheck;

                        // Save latest checked block if block saver is provided
                        if (_blockSaver != null)
                        {
                            try
                            {
                                var saveTask = _blockSaver(_latestCheckedBlock);
                                if (!saveTask.IsCompleted)
                                {
                                    // Don't block the main thread for long-running async operations
                                    _ = Task.Run(async () => 
                                    {
                                        try
                                        {
                                            await saveTask;
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"Async block save failed for block {_latestCheckedBlock}: {ex.Message}");
                                        }
                                    });
                                }
                                else
                                {
                                    // Wait for already completed tasks (likely sync operations)
                                    saveTask.Wait();
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to save latest checked block: {_latestCheckedBlock} - {ex.Message}");
                                Console.WriteLine(ex.StackTrace);
                            }
                        }
                    }
                    catch (System.IO.IOException ex)
                    {
                        Console.WriteLine($"Failed to fetch VIDA transactions: {ex.Message}");
                        Console.WriteLine(ex.StackTrace);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Unexpected error in VidaTransactionSubscription: {ex.Message}");
                        Console.WriteLine(ex.StackTrace);
                    }

                    // Sleep between iterations
                    Thread.Sleep(_pollInterval);
                }

                _running = false;
            });

            thread.Name = $"VidaTransactionSubscription:VIDA-ID-{_vidaId}";
            thread.Start();
        }

        /// <summary>
        /// Sets the latest checked block number manually
        /// </summary>
        /// <param name="blockNumber">The block number to set as latest checked</param>
        public void SetLatestCheckedBlock(ulong blockNumber)
        {
            _latestCheckedBlock = blockNumber;
        }

        /// <summary>
        /// Pauses the subscription process and waits for confirmation
        /// </summary>
        public void Pause()
        {
            _wantsToPause = true;

            // Wait until the thread is actually paused
            while (!_paused && _running)
            {
                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// Resumes the subscription process after being paused
        /// </summary>
        public void Resume()
        {
            _wantsToPause = false;
        }

        /// <summary>
        /// Stops the subscription process
        /// </summary>
        public void Stop()
        {
            Pause();
            _stop = true;
        }

        /// <summary>
        /// Gets whether the subscription is currently running
        /// </summary>
        public bool IsRunning()
        {
            return _running;
        }

        /// <summary>
        /// Gets whether the subscription is currently paused
        /// </summary>
        public bool IsPaused()
        {
            return _wantsToPause;
        }

        /// <summary>
        /// Gets whether the subscription has been stopped
        /// </summary>
        public bool IsStopped()
        {
            return _stop;
        }

        /// <summary>
        /// Gets the latest block that has been checked for transactions
        /// </summary>
        public ulong GetLatestCheckedBlock()
        {
            return _latestCheckedBlock;
        }

        /// <summary>
        /// Gets the block number where the subscription started checking from
        /// </summary>
        public ulong GetStartingBlock()
        {
            return _startingBlock;
        }

        /// <summary>
        /// Gets the VIDA ID that this subscription is for
        /// </summary>
        public ulong GetVidaId()
        {
            return _vidaId;
        }

        /// <summary>
        /// Gets the handler that processes transactions
        /// </summary>
        public VidaTransactionHandler GetHandler()
        {
            return _handler;
        }

        /// <summary>
        /// Gets the PWR SDK instance used by this subscription
        /// </summary>
        public RPC GetPwrj()
        {
            return _pwrSdk;
        }
    }
}