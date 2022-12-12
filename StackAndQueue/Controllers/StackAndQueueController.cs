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
        private readonly IBackgroundTaskStack<StackModel> _taskStack;
        private readonly CancellationToken _cancellationToken;


        public StackAndQueueController(IBackgroundTaskQueue<QueueModel> taskQueue, IHostApplicationLifetime applicationLifetime, IBackgroundTaskStack<StackModel> taskStack)
        {
            _taskQueue = taskQueue;
            _cancellationToken = applicationLifetime.ApplicationStopping;
            _taskStack = taskStack;
        }


        [HttpGet]
        public async Task<IActionResult> StackAsync(int index)
        {
            for (int i = 0; i < index; i++)
            {
              // await _taskQueue.QueueBackgroundWorkItemAsync(new QueueModel(new Random().Next(i,i+999)));
                 await _taskStack.StackBackgroundWorkItem(new StackModel(new Random().Next(i, i + 999)));
            }
            //await Task.Delay(TimeSpan.FromMilliseconds(1));

            //for (int i = 0; i < index; i++)
            //{
            //  // await _taskQueue.QueueBackgroundWorkItemAsync(new QueueModel(new Random().Next(i,i+999)));
            //     await _taskStack.StackBackgroundWorkItem(new StackModel(new Random().Next(i, i + 999)));
            //}
            return Ok();
        }
    }
}
