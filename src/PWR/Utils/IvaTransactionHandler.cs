using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using PWR.Models;

namespace PWR
{
    /// <summary>
    /// Functional interface for handling IVA transactions
    /// </summary>
    public delegate void IvaTransactionHandler(VmDataTxn transaction);

    /// <summary>
    /// Handles subscription to IVA transactions for a specific VM
    /// </summary>
    public class IvaTransactionSubscription
    {
        private readonly PwrApiSdk _pwrSdk;
        private readonly ulong _vmId;
        private readonly ulong _startingBlock;
        private ulong _latestCheckedBlock;
        private readonly IvaTransactionHandler _handler;
        private readonly int _pollInterval;

        // Thread control flags using atomics
        private int _isPaused;
        private int _isStopped;
        private int _isRunning;

        /// <summary>
        /// Creates a new IVA transaction subscription
        /// </summary>
        /// <param name="pwrSdk">The PWR SDK instance</param>
        /// <param name="vmId">The VM ID to subscribe to</param>
        /// <param name="startingBlock">The block number to start checking from</param>
        /// <param name="handler">The handler for processing transactions</param>
        /// <param name="pollInterval">Interval in milliseconds between polling for new blocks</param>
        public IvaTransactionSubscription(PwrApiSdk pwrSdk, ulong vmId, ulong startingBlock, IvaTransactionHandler handler, int pollInterval = 100)
        {
            _pwrSdk = pwrSdk ?? throw new ArgumentNullException(nameof(pwrSdk));
            _vmId = vmId;
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
                Console.WriteLine("ERROR: IvaTransactionSubscription is already running");
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
                            List<VmDataTxn> transactions = Task.Run(async () => 
                                await _pwrSdk.GetVmDataTransactions(currentBlock, effectiveLatestBlock, _vmId)).Result;

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
                        Console.WriteLine($"Error in IVA transaction subscription: {ex.Message}");
                        Console.WriteLine(ex.StackTrace);
                        break;
                    }

                    // Sleep between iterations
                    Thread.Sleep(_pollInterval);
                }

                Interlocked.Exchange(ref _isRunning, 0);
            });

            thread.Name = $"IvaTransactionSubscription:IVA-ID-{_vmId}";
            thread.IsBackground = true;
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
        /// Gets the VM ID that this subscription is for
        /// </summary>
        public ulong GetVidaId()
        {
            return _vmId;
        }

        /// <summary>
        /// Gets the handler that processes transactions
        /// </summary>
        public IvaTransactionHandler GetHandler()
        {
            return _handler;
        }

        /// <summary>
        /// Gets the PWR SDK instance used by this subscription
        /// </summary>
        public PwrApiSdk GetPwrApiSdk()
        {
            return _pwrSdk;
        }
    }
}