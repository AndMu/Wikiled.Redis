using System;
using System.Threading;
using NLog;
using Wikiled.Core.Utility.Arguments;

namespace Wikiled.Redis.Channels
{
    public abstract class TimerChannel : BaseChannel
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly TimeSpan period;

        private readonly object syncRoot = new object();

        private bool isDisabled;

        private Timer timer;

        protected TimerChannel(string name, TimeSpan period)
            : base(name)
        {
            Guard.IsValid(() => period, period, item => item.TotalMilliseconds > 0, nameof(period));
            this.period = period;
        }

        protected abstract void TimerEvent();

        protected override void CloseInternal()
        {
            lock (syncRoot)
            {
                if (timer != null)
                {
                    timer.Dispose();
                    timer = null;
                }
            }

            base.CloseInternal();
        }

        protected override ChannelState OpenInternal()
        {
            lock (syncRoot)
            {
                timer?.Dispose();
                timer = new Timer(TimerInternal, null, period, period);
            }

            return base.OpenInternal();
        }

        private void TimerInternal(object sender)
        {
            try
            {
                lock (syncRoot)
                {
                    if (isDisabled)
                    {
                        return;
                    }

                    isDisabled = true;
                    timer.Change(Timeout.Infinite, Timeout.Infinite);
                }

                TimerEvent();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
            finally
            {
                lock (syncRoot)
                {
                    isDisabled = false;
                    timer?.Change(period, period);
                }
            }
        }
    }
}