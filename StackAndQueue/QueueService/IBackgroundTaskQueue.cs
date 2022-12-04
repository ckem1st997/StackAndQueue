using StackAndQueue.Model;
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
        Task StackBackgroundWorkItem(T workItem);

        Task<T> Dequeue(CancellationToken cancellationToken);
        Task<bool> CheckStack(CancellationToken cancellationToken);
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
            _queue = new Stack<T>(10000);
        }

        public async Task StackBackgroundWorkItem(T workItem)
        {
            if (workItem is null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            _queue.Push(workItem);
            await Task.Delay(1);

        }

        public async Task<T> Dequeue(CancellationToken cancellationToken)
        {
            T? workItem = _queue.Pop();
            await Task.Delay(1, cancellationToken);
            return await Task.FromResult(workItem);
        }

        public async Task<bool> CheckStack(CancellationToken cancellationToken)
        {
            return await Task.FromResult(_queue.Count > 0);
        }
    }


    public sealed class QueueHostedService : BackgroundService
    {
        private readonly IBackgroundTaskQueue<QueueModel> _taskQueue;
        private readonly ILogger<QueueHostedService> _logger;

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

                    Console.WriteLine("Queue : " + workItem.Name);
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
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (await _taskQueue.CheckStack(stoppingToken))
                    {
                        StackModel? workItem = await _taskQueue.Dequeue(stoppingToken);
                        if (workItem is not null)
                            Console.WriteLine("Stack : " + workItem.Name);
                    }

                    await Task.Delay(1, stoppingToken);


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
