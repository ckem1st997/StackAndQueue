using Microsoft.AspNetCore.Mvc;
using StackAndQueue.Model;
using StackAndQueue.QueueService;

namespace StackAndQueue.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StackAndQueueController : Controller
    {
        private readonly IBackgroundTaskQueue<QueueModel> _taskQueue;
        private readonly IBackgroundTaskQueue<Func<CancellationToken, ValueTask>> backgroundTaskQueue;
        private readonly IBackgroundTaskStack<StackModel> _taskStack;
        private readonly CancellationToken _cancellationToken;


        public StackAndQueueController(IBackgroundTaskQueue<QueueModel> taskQueue, IHostApplicationLifetime applicationLifetime, IBackgroundTaskStack<StackModel> taskStack, IBackgroundTaskQueue<Func<CancellationToken, ValueTask>> backgroundTaskQueue)
        {
            _taskQueue = taskQueue;
            _cancellationToken = applicationLifetime.ApplicationStopping;
            _taskStack = taskStack;
            this.backgroundTaskQueue = backgroundTaskQueue;
        }


        [HttpGet]
        public async Task<IActionResult> StackAsync(int index)
        {
            for (int i = 0; i < index; i++)
            {
                await backgroundTaskQueue.QueueBackgroundWorkItemAsync(x => RunRegistrationCompanyMainAsync(i.ToString(), x));
            }
            return Ok();
        }

        private async ValueTask RunRegistrationCompanyMainAsync(string tenantId, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
                await Task.Run(() => Console.WriteLine("Func<CancellationToken, ValueTask>: " + tenantId));
        }
    }
}






















