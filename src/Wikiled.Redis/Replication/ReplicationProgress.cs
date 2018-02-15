using System.Linq;
using Wikiled.Common.Arguments;

namespace Wikiled.Redis.Replication
{
    public class ReplicationProgress
    {
        private ReplicationProgress()
        {
        }

        public static ReplicationProgress CreateActive(HostStatus master, params HostStatus[] slaves)
        {
            Guard.NotNull(() => master, master);
            Guard.NotNull(() => slaves, slaves);
            Guard.IsValid(() => slaves, slaves, statuses => statuses.Length > 0, "Minimum length is 1");
            ReplicationProgress instance = new ReplicationProgress();
            instance.IsActive = true;
            instance.Master = master;
            instance.Slaves = slaves;
            instance.InSync = slaves.All(item => item.Offset == master.Offset);
            return instance;
        }

        public static ReplicationProgress CreateInActive()
        {
            ReplicationProgress instance = new ReplicationProgress();
            return instance;
        }

        public bool IsActive { get; private set; }

        public bool InSync { get; private set; }

        public HostStatus Master { get; private set; }

        public HostStatus[] Slaves { get; private set; }
    }
}
