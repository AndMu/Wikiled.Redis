using System;
using System.Linq;

namespace Wikiled.Redis.Replication
{
    public class ReplicationProgress
    {
        private ReplicationProgress()
        {
        }

        public static ReplicationProgress CreateActive(HostStatus master, params HostStatus[] slaves)
        {
            if (master == null)
            {
                throw new ArgumentNullException(nameof(master));
            }

            if (slaves == null)
            {
                throw new ArgumentNullException(nameof(slaves));
            }

            if (slaves.Length == 0)
            {
                throw new ArgumentException("Value cannot be an empty collection.", nameof(slaves));
            }

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
