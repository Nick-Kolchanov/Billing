using Grpc.Core;

namespace Billing.Services
{
    public class BillingService : Billing.BillingBase
    {
        private readonly ILogger<BillingService> _logger;

        private static long _nextCoinId = 1;
        private static List<User> users = new List<User>();
        private static List<UserCoin> userCoins = new List<UserCoin>();

        public BillingService(ILogger<BillingService> logger)
        {
            _logger = logger;
            
            if (users.Count == 0)
            {
                users = new List<User>()
                {
                    new User { Name = "boris", Rating = 5000},
                    new User { Name = "maria", Rating = 1000},
                    new User { Name = "oleg", Rating = 800}
                };
            }
        }

        public override async Task ListUsers(None _, IServerStreamWriter<UserProfile> responseStream, ServerCallContext context)
        {
            foreach (var user in users)
            {
                var userProfile = new UserProfile { Name = user.Name, Amount = user.Coins.Count };
                await responseStream.WriteAsync(userProfile);
            }
        }

        public override Task<Response> CoinsEmission(EmissionAmount request, ServerCallContext context)
        {
            if (request.Amount < users.Count)
            {
                return Task.FromResult(new Response()
                {
                    Status = Response.Types.Status.Failed,
                    Comment = "Unable to provide at least 1 coin to everyone"
                });
            }

            long amount = request.Amount - users.Count;
            long totalRating = users.Sum(u => u.Rating);
            double total = 0;
            double oldTotal = total;
            foreach (var user in users)
            {
                total += 1.0 * user.Rating / totalRating * amount;
                var coinAmount = (int)total - (int)oldTotal + 1;
                for (int i = 0; i < coinAmount; i++)
                {
                    var newCoin = new UserCoin(_nextCoinId, user);
                    userCoins.Add(newCoin);
                    user.Coins.Add(newCoin);

                    _nextCoinId++;
                }
                oldTotal = total;
            }

            return Task.FromResult(new Response()
            {
                Status = Response.Types.Status.Ok,
                Comment = "Coins emissioned"
            });
        }

        public override Task<Response> MoveCoins(MoveCoinsTransaction request, ServerCallContext context)
        {
            var srcUser = users.Find(u => u.Name == request.SrcUser);
            var dstUser = users.Find(u => u.Name == request.DstUser);

            if (srcUser == null || dstUser == null)
            {
                return Task.FromResult(new Response
                {
                    Status = Response.Types.Status.Failed,
                    Comment = "Unable to find users"
                });
            }

            if (srcUser == dstUser)
            {
                return Task.FromResult(new Response
                {
                    Status = Response.Types.Status.Failed,
                    Comment = "Unable to move coins to the same user"
                });
            }

            if (srcUser.Coins.Count < request.Amount)
            {
                return Task.FromResult(new Response
                {
                    Status = Response.Types.Status.Failed,
                    Comment = "Not enough coins on the source user balance"
                });
            }

            for (int i = 0; i < request.Amount; i++)
            {
                var coin = srcUser.Coins[0];
                coin.ChangeOwner(dstUser);

                srcUser.Coins.RemoveAt(0);
                dstUser.Coins.Add(coin);
            }

            return Task.FromResult(new Response
            {
                Status = Response.Types.Status.Ok,
                Comment = "Coins moved"
            });
        }

        public override Task<Coin> LongestHistoryCoin(None _, ServerCallContext context)
        {            
            if (userCoins.Count == 0)
            {
                var coin = new Coin { Id = -1, History = "-" };
                return Task.FromResult(coin);
            }

            UserCoin longestHistoryCoin = userCoins[0];
            long historyLength = longestHistoryCoin.HistoryLength;

            foreach (var coin in userCoins)
            {
                if (coin.HistoryLength > historyLength)
                {
                    longestHistoryCoin = coin;
                    historyLength = coin.HistoryLength;                    
                }
            }

            var foundCoin = new Coin() { Id = longestHistoryCoin.Id, History = longestHistoryCoin.History };
            return Task.FromResult(foundCoin);
        }
    }
}