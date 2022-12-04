namespace StackAndQueue.Model
{
    public class StackModel : BaseModel
    {
        public StackModel(int id) : base(id)
        {
            base.Name = "Stack " + base.Name;
        }
    }
}
