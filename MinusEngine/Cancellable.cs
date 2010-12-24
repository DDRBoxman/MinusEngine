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

    public class CancellableAsyncUpload : Cancellable
    {
        #region Private fields
        private WebClient client;
        private Boolean cancelled;
        #endregion

        #region Constructors
        public CancellableAsyncUpload(WebClient client)
        {
            this.client = client;
        }
        #endregion

        #region Public methods
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
        #endregion
    }
}
