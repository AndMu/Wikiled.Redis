using System;
using System.Collections.Generic;
using System.Text;

namespace Wikiled.Redis.Persistency
{
    public sealed class UserRepository : IUserRepository
    {
        private const string UserTag = "User";

        private const string RoleTag = "Roles";

        private const string AllUserTag = "All.Users";

        private readonly ILogger<UserRepository> _log;

        private readonly IRedisLink _redis;

        private readonly IndexKey _allUsersIndex;

        public UserRepository(ILogger<UserRepository> log, IRedisLink redis)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            redis.RegisterHashType<UserData>().IsSingleInstance = true;
            redis.RegisterHashType<TradingRoles>().IsSingleInstance = true;
            _allUsersIndex = new IndexKey(this, AllUserTag, true);
        }

        public string Name => "Users";

        public async Task SaveRoles(UserData user, params TradingRoles[] roles)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var roleKey = GetRoleKey(user);
            await _redis.Database.KeyDeleteAsync(roleKey).ConfigureAwait(false);

            var tasks = new List<Task>();
            foreach (var role in roles)
            {
                tasks.Add(_redis.Database.SetAddAsync(roleKey, $"{(int)role.Asset}:{(int)role.Strategy}:{(int)role.DataSource}"));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        public async Task<TradingRoles[]> GetRoles(UserData user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var roleKey = GetRoleKey(user);
            var set = await _redis.Database.SetMembersAsync(roleKey).ConfigureAwait(false);
            var roles = new TradingRoles[set.Length];

            int current = 0;
            foreach (string redisValue in set)
            {
                var blocks = redisValue.Split(':');

                if (blocks.Length != 3)
                {
                    _log.LogWarning("Invalid data in entitlements: {0}", redisValue);
                    continue;
                }

                var role = new TradingRoles();
                role.Asset = (Asset)int.Parse(blocks[0]);
                role.Strategy = (Strategy)int.Parse(blocks[1]);
                role.DataSource = (MarketSource)int.Parse(blocks[2]);
                roles[current] = role;
                current++;
            }

            return roles;
        }

        public async Task SaveUser(UserData user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            _log.LogDebug("Saving user: {0}", user.UserId);
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var key = GetUserKey(user.UserId);
            key.AddIndex(_allUsersIndex);

            var contains = await _redis.Client.ContainsRecord<UserData>(key).ConfigureAwait(false);
            if (contains)
            {
                await _redis.Client.DeleteAll<UserData>(key).ConfigureAwait(false);
            }

            await _redis.Client.AddRecord(key, user).ConfigureAwait(false);
        }

        public async Task<UserData[]> LoadAllUsers()
        {
            return await _redis.Client.GetRecords<UserData>(_allUsersIndex, 0, -1).ToArray();
        }

        public async Task<UserData> LoadUser(string id)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            var key = GetUserKey(id);
            return await _redis.Client.GetRecords<UserData>(key).LastOrDefaultAsync();
        }

        private IDataKey GetUserKey(string id)
        {
            string[] key = { UserTag, id.ToLower() };
            return new RepositoryKey(this, new ObjectKey(key));
        }

        private string GetRoleKey(UserData user)
        {
            var key = GetUserKey(user.UserId?.ToLower());
            var roleKey = $"{_redis.Name}:{key.FullKey}:{RoleTag}";
            return roleKey;
        }
    }
}
