namespace Billing
{
    public class UserCoin
    {
        public long Id { get; private set; }
        public string History { get; private set; }
        public long HistoryLength { get; private set; }
        public User Owner { get; private set; }

        public UserCoin(long id, User owner)
        {
            Id = id;
            Owner = owner;
            History = owner.Name;
            HistoryLength = 1;
        }

        public void ChangeOwner(User newOwner)
        {
            Owner = newOwner;
            History = History + '-' + newOwner.Name;
            HistoryLength++;
        }
    }
}
