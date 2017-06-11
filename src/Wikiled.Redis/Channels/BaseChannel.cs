using System;
using NLog;

namespace Wikiled.Redis.Channels
{
    public abstract class BaseChannel : IChannel
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly object syncRoot = new object();

        private ChannelState state;

        protected BaseChannel(string name)
        {
            Name = name;
        }

        public bool CanReOpen => true;

        public string Name { get; }

        public ChannelState State
        {
            get => state;
            private set
            {
                if (state == value)
                {
                    return;
                }

                logger.Debug("State changed from <{0}> to <{1}>", state, value);
                state = value;
            }
        }

        private bool IsDispossed { get; set; }

        private bool SupportRetry => false;

        public void Close()
        {
            lock (syncRoot)
            {
                logger.Debug("Closing {0}...", Name);
                if (State != ChannelState.Open && State != ChannelState.Opening)
                {
                    return;
                }

                State = ChannelState.Closing;
                CloseInternal();
                State = ChannelState.Closed;
            }
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (IsDispossed)
            {
                return;
            }

            logger.Debug("Disposing {0}", Name);
            IsDispossed = true;
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Open()
        {
            logger.Debug("Opening {0}...", Name);
            lock (syncRoot)
            {
                if (State == ChannelState.Open || State == ChannelState.Closing || State == ChannelState.Opening)
                {
                    return;
                }

                if (State == ChannelState.Closed && !CanReOpen)
                {
                    throw new ChannelException("This link can't be reopened");
                }

                State = ChannelState.Opening;
                var currentState = OpenInternal();
                switch (currentState)
                {
                    case ChannelState.Open:
                        State = ChannelState.Open;
                        break;
                    case ChannelState.Opening:
                        logger.Debug("Channel initializing");
                        break;
                    default:
                        if (!SupportRetry)
                        {
                            State = ChannelState.Closed;
                        }
                        break;
                }
            }
        }

        protected virtual void CloseInternal()
        {
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            Close();
        }

        protected virtual ChannelState OpenInternal()
        {
            return ChannelState.Open;
        }
    }
}
