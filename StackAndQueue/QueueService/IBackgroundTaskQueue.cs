using StackAndQueue.Model;
using System.Threading;
using System.Threading.Channels;

namespace StackAndQueue.QueueService
{
    public interface IBackgroundTaskQueue<T>
    {
        ValueTask QueueBackgroundWorkItemAsync(T workItem);

        ValueTask<T> DequeueAsync(CancellationToken cancellationToken);
    }

    public interface IBackgroundTaskStack<T>
    {
        Task StackBackgroundWorkItem(T workItem, CancellationToken cancellationToken = default);

        Task<T> Dequeue(CancellationToken cancellationToken);
        Task<bool> CheckStack();
        Task<int> COUNT();

    }


    public sealed class DefaultBackgroundTaskQueue<T> : IBackgroundTaskQueue<T>
    {
        private readonly Channel<T> _queue;

        public DefaultBackgroundTaskQueue()
        {
            BoundedChannelOptions options = new(10000)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<T>(options);
        }

        public async ValueTask QueueBackgroundWorkItemAsync(T workItem)
        {
            if (workItem is null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            await _queue.Writer.WriteAsync(workItem);
        }

        public async ValueTask<T> DequeueAsync(
            CancellationToken cancellationToken)
        {
            T? workItem = await _queue.Reader.ReadAsync(cancellationToken);

            return workItem;
        }
    }


    public sealed class DefaultBackgroundTaskStack<T> : IBackgroundTaskStack<T>
    {
        private readonly Stack<T> _queue;

        public DefaultBackgroundTaskStack()
        {
            _queue = new Stack<T>();
        }

        public async Task StackBackgroundWorkItem(T workItem, CancellationToken cancellationToken = default)
        {
            if (workItem is null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }
            if (cancellationToken.IsCancellationRequested)
            {
                await Task.FromCanceled<T>(cancellationToken);
            }
            try
            {
                _queue.Push(workItem);
                //  await Task.Delay(TimeSpan.FromMilliseconds(10), cancellationToken);
                await Task.CompletedTask;
            }
            catch (Exception e)
            {
                await Task.FromException(e);
            }
        }

        public async Task<T> Dequeue(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return await Task.FromCanceled<T>(cancellationToken);
            }

            try
            {
                if (_queue.TryPop(out T? fastItem))
                {
                    return await Task.FromResult(fastItem);
                }
            }
            catch (Exception exc) when (exc is OperationCanceledException)
            {
                return await Task.FromException<T>(exc);
            }

            return await Task.FromCanceled<T>(cancellationToken);

        }

        public async Task<bool> CheckStack()
        {
            //  await Task.Delay(1, cancellationToken);
            return await Task.FromResult(_queue.Count > 0);

        }
        public async Task<int> COUNT()
        {
            //  await Task.Delay(1, cancellationToken);
            return await Task.FromResult(_queue.Count);
        }

    }


    public sealed class QueueHostedService : BackgroundService
    {
        private readonly IBackgroundTaskQueue<QueueModel> _taskQueue;
        private readonly ILogger<QueueHostedService> _logger;
        private int IdBefore;

        public QueueHostedService(
            IBackgroundTaskQueue<QueueModel> taskQueue,
            ILogger<QueueHostedService> logger) =>
            (_taskQueue, _logger) = (taskQueue, logger);

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                $"{nameof(QueueHostedService)} is running.{Environment.NewLine}" +
                $"{Environment.NewLine}Tap W to add a work item to the " +
                $"background queue.{Environment.NewLine}");

            return ProcessTaskQueueAsync(stoppingToken);
        }

        private async Task ProcessTaskQueueAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("////////// Queue ////////////");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    QueueModel? workItem = await _taskQueue.DequeueAsync(stoppingToken);
                    if (workItem?.Id is not null && workItem.Id > IdBefore)
                        IdBefore = workItem.Id;
                    // Console.WriteLine("Queue : " + workItem.Name);
                    //  Console.WriteLine(workItem?.Id);
                    Console.WriteLine(IdBefore);

                }
                catch (OperationCanceledException)
                {
                    // Prevent throwing if stoppingToken was signaled
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing task work item.");
                }
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                $"{nameof(QueueHostedService)} is stopping.");

            await base.StopAsync(stoppingToken);
        }
    }



    public sealed class StackHostedService : BackgroundService
    {
        private readonly IBackgroundTaskStack<StackModel> _taskQueue;
        private readonly ILogger<QueueHostedService> _logger;
        private int IdBefore;
        private int IdAfter;

        public StackHostedService(
            IBackgroundTaskStack<StackModel> taskQueue,
            ILogger<QueueHostedService> logger) =>
            (_taskQueue, _logger) = (taskQueue, logger);

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                $"{nameof(QueueHostedService)} is running.{Environment.NewLine}" +
                $"{Environment.NewLine}Tap W to add a work item to the " +
                $"background Stack.{Environment.NewLine}");

            return ProcessTaskQueueAsync(stoppingToken);
        }

        private async Task ProcessTaskQueueAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("////////// Stack ////////////");
            int delay = 1;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (await _taskQueue.CheckStack())
                    {
                        StackModel? workItem = await _taskQueue.Dequeue(stoppingToken);
                        if (workItem?.Id is not null && workItem.Id > IdBefore)
                            IdBefore = workItem.Id;
                        // Console.WriteLine("Queue : " + workItem.Name);
                        // Console.WriteLine(workItem?.Id);
                        Console.WriteLine(IdBefore);
                    }
                    else
                        Console.WriteLine("max: " + IdBefore);

                    // này sẽ delay
                    //  await Task.Delay(TimeSpan.FromMilliseconds(10), stoppingToken);
                    await Task.Delay(TimeSpan.FromMilliseconds(delay), stoppingToken);
                    //await Task.CompletedTask;
                    if (delay > 0)
                    {
                        Console.WriteLine(delay);
                        delay /= 2;

                    }

                }
                catch (OperationCanceledException)
                {
                    // Prevent throwing if stoppingToken was signaled
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing task work item.");
                }
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                $"{nameof(StackHostedService)} is stopping.");

            await base.StopAsync(stoppingToken);
        }
    }
}
