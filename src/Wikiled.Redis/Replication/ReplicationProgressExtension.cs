using System.Collections.Generic;

namespace Wikiled.Redis.Replication
{
    public static class ReplicationProgressExtension
    {
        public static IEnumerable<string> GenerateProgress(this ReplicationProgress progress)
        {
            if (progress.Slaves != null)
            {
                foreach (var progressSlave in progress.Slaves)
                {
                    yield return $"Replication progress from [{progress.Master.EndPoint}] to [{progressSlave.EndPoint} - {progressSlave.Offset / progress.Master.Offset * 100:F2}";
                }
            }
        }
    }
}
