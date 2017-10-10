Redis library 

![Nuget](https://img.shields.io/nuget/v/Wikiled.Redis.svg)

Full example of repository [github](https://github.com/AndMu/TwitterMonitor/blob/master/src/Wikiled.Twitter/Persistency/RedisPersistency.cs)
Snapshot:
```
var config = new RedisConfiguration("localhost", 6370);
config.SyncTimeout = 50000;
using (RedisLink redis = new RedisLink("Wikiled", new RedisMultiplexer(config)))
{
	link.RegisterHashType<TweetData>().IsSingleInstance = true;
	redis.Open();
	var observable = redis.Client.GetRecords<TweetData>("key", 0, 10);
	....
}
	
```