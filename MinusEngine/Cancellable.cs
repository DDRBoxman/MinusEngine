using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace BiasedBit.MinusEngine
{
    public interface Cancellable
    {
        Boolean Cancel();

        Boolean IsCancelled();
    }

    public class CancellableUpload : Cancellable
    {
        private WebClient client;
        private Boolean cancelled;

        public CancellableUpload(WebClient client)
        {
            this.client = client;
        }

        public Boolean Cancel()
        {
            lock (this)
            {
                if (this.cancelled)
                {
                    return false;
                }

                if (client != null)
                {
                    this.cancelled = true;
                }

                if (client.IsBusy)
                {
                    client.CancelAsync();
                }

                return true;
            }
        }

        public Boolean IsCancelled()
        {
            lock (this)
            {
                return this.cancelled;
            }
        }
    }
}
