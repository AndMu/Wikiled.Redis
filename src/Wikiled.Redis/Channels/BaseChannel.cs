using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Wikiled.Redis.Channels
{
    public abstract class BaseChannel : IChannel
    {
        private ChannelState state;

        protected BaseChannel(string name)
        {
            Name = name;
        }

        protected abstract ILogger Logger { get; }

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

                Logger.LogDebug("State changed from <{0}> to <{1}>", state, value);
                state = value;
            }
        }

        private bool IsDisposed { get; set; }

        public void Close()
        {
            Logger.LogDebug("Closing {0}...", Name);

            if (State != ChannelState.Open &&
                State != ChannelState.Opening)
            {
                return;
            }

            State = ChannelState.Closing;
            CloseInternal();
            State = ChannelState.Closed;
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            Logger.LogDebug("Disposing {0}", Name);
            IsDisposed = true;
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task Open()
        {
            Logger.LogDebug("Opening {0}...", Name);

            if (State == ChannelState.Open ||
                State == ChannelState.Closing ||
                State == ChannelState.Opening)
            {
                return;
            }

            await OpenAndSetState().ConfigureAwait(false);
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

        protected virtual Task<ChannelState> OpenInternal()
        {
            return Task.FromResult(ChannelState.Open);
        }

        private async Task OpenAndSetState()
        {
            try
            {
                State = ChannelState.Opening;
                State = await OpenInternal().ConfigureAwait(false);
            }
            catch
            {
                State = ChannelState.Closed;
                throw;
            }
        }
    }
}
