namespace Billing
{
    public class User
    {
        public string Name { get; set; } = string.Empty;
        public long Rating { get; set; }
        public List<UserCoin> Coins { get; set; } = new List<UserCoin>();
    }
}
