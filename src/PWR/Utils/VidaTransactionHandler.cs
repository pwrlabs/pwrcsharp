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
    /// Handles subscription to VIDA transactions for a specific VIDA
    /// </summary>
    public class VidaTransactionSubscription
    {
        private readonly RPC _pwrSdk;
        private readonly ulong _vidaId;
        private readonly ulong _startingBlock;
        private ulong _latestCheckedBlock;
        private readonly VidaTransactionHandler _handler;
        private readonly int _pollInterval;

        // Thread control flags using atomics
        private int _isPaused;
        private int _isStopped;
        private int _isRunning;

        /// <summary>
        /// Creates a new VIDA transaction subscription
        /// </summary>
        /// <param name="pwrSdk">The PWR SDK instance</param>
        /// <param name="vidaId">The VIDA ID to subscribe to</param>
        /// <param name="startingBlock">The block number to start checking from</param>
        /// <param name="handler">The handler for processing transactions</param>
        /// <param name="pollInterval">Interval in milliseconds between polling for new blocks</param>
        public VidaTransactionSubscription(RPC pwrSdk, ulong vidaId, ulong startingBlock, VidaTransactionHandler handler, int pollInterval = 100)
        {
            _pwrSdk = pwrSdk ?? throw new ArgumentNullException(nameof(pwrSdk));
            _vidaId = vidaId;
            _startingBlock = startingBlock;
            _latestCheckedBlock = startingBlock;
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _pollInterval = pollInterval;
        }

        /// <summary>
        /// Starts the subscription process
        /// </summary>
        public void Start()
        {
            if (Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0)
            {
                Console.WriteLine("ERROR: VidaTransactionSubscription is already running");
                return;
            }

            Interlocked.Exchange(ref _isPaused, 0);
            Interlocked.Exchange(ref _isStopped, 0);

            ulong currentBlock = _startingBlock;

            Thread thread = new Thread(() =>
            {
                while (!IsStopped())
                {
                    if (IsPaused())
                    {
                        Thread.Sleep(_pollInterval);
                        continue;
                    }

                    try
                    {
                        ulong latestBlock = Task.Run(async () => 
                            await _pwrSdk.GetLatestBlockNumber()).Result;

                        ulong effectiveLatestBlock = (latestBlock > currentBlock + 1000) 
                                ? currentBlock + 1000 
                                : latestBlock;

                        if (effectiveLatestBlock >= currentBlock)
                        {
                            List<VidaDataTransaction> transactions = Task.Run(async () => 
                                await _pwrSdk.GetVidaDataTransactions(currentBlock, effectiveLatestBlock, _vidaId)).Result;

                            foreach (var transaction in transactions)
                            {
                                _handler(transaction);
                            }

                            _latestCheckedBlock = effectiveLatestBlock;
                            currentBlock = effectiveLatestBlock + 1;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in VIDA transaction subscription: {ex.Message}");
                        Console.WriteLine(ex.StackTrace);
                        break;
                    }

                    // Sleep between iterations
                    Thread.Sleep(_pollInterval);
                }

                Interlocked.Exchange(ref _isRunning, 0);
            });

            thread.Name = $"VidaTransactionSubscription:VIDA-ID-{_vidaId}";
            thread.Start();
        }

        /// <summary>
        /// Pauses the subscription process
        /// </summary>
        public void Pause()
        {
            Interlocked.Exchange(ref _isPaused, 1);
        }

        /// <summary>
        /// Resumes the subscription process after being paused
        /// </summary>
        public void Resume()
        {
            Interlocked.Exchange(ref _isPaused, 0);
        }

        /// <summary>
        /// Stops the subscription process
        /// </summary>
        public void Stop()
        {
            Interlocked.Exchange(ref _isStopped, 1);
        }

        /// <summary>
        /// Gets whether the subscription is currently running
        /// </summary>
        public bool IsRunning()
        {
            return Interlocked.CompareExchange(ref _isRunning, 0, 0) == 1;
        }

        /// <summary>
        /// Gets whether the subscription is currently paused
        /// </summary>
        public bool IsPaused()
        {
            return Interlocked.CompareExchange(ref _isPaused, 0, 0) == 1;
        }

        /// <summary>
        /// Gets whether the subscription has been stopped
        /// </summary>
        public bool IsStopped()
        {
            return Interlocked.CompareExchange(ref _isStopped, 0, 0) == 1;
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
        public RPC GetPwrApiSdk()
        {
            return _pwrSdk;
        }
    }
}