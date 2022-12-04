namespace StackAndQueue.Model
{
    public class QueueModel : BaseModel
    {
        public QueueModel(int id) : base(id)
        {
            base.Name = "Queue " + base.Name;
        }
    }
}
