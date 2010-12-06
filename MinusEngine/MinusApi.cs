using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.Specialized;

namespace BiasedBit.MinusEngine
{
    public delegate void CreateGalleryCompleteHandler(MinusApi sender, CreateGalleryResult result);
    public delegate void CreateGalleryFailedHandler(MinusApi sender, Exception e);

    public delegate void UploadItemCompleteHandler(MinusApi sender, UploadItemResult result);
    public delegate void UploadItemFailedHandler(MinusApi sender, Exception e);

    public delegate void SaveGalleryCompleteHandler(MinusApi sender);
    public delegate void SaveGalleryFailedHandler(MinusApi sender, Exception e);

    public class MinusApi
    {
        #region Constants
        public static readonly String USER_AGENT = "MinusEngine_0.1";
        public static readonly Uri CREATE_GALLERY_URI = new Uri("http://min.us/api/CreateGallery");
        public static readonly Uri UPLOAD_ITEM_URI = new Uri("http://min.us/api/UploadItem");
        public static readonly Uri SAVE_GALLERY_URI = new Uri("http://min.us/api/SaveGallery");
        #endregion

        #region Public fields
        public event CreateGalleryCompleteHandler CreateGalleryComplete;
        public event CreateGalleryFailedHandler CreateGalleryFailed;

        public event UploadItemCompleteHandler UploadItemComplete;
        public event UploadItemFailedHandler UploadItemFailed;

        public event SaveGalleryCompleteHandler SaveGalleryComplete;
        public event SaveGalleryFailedHandler SaveGalleryFailed;

        public IWebProxy Proxy;
        public String ApiKey
        {
            get { return this.apiKey; }
        }
        #endregion

        #region Private fields
        private readonly String apiKey;
        #endregion

        #region Constructors
        public MinusApi(String apiKey)
        {
            if (String.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentException("API key argument cannot be null");
            }

            this.apiKey = apiKey;
        }
        #endregion

        #region Public methods
        public void CreateGallery()
        {
            WebClient client = this.CreateAndSetupWebClient();
            client.DownloadStringCompleted += delegate(object sender, DownloadStringCompletedEventArgs e) {
                if (e.Error != null)
                {
                    Debug.WriteLine("CreateGallery operation failed: " + e.Error.Message);
                    this.TriggerCreateGalleryFailed(e.Error);
                    client.Dispose();
                    return;
                }

                CreateGalleryResult result = JsonConvert.DeserializeObject<CreateGalleryResult>(e.Result);
                Debug.WriteLine("CreateGallery operation successful: " + result);
                this.TriggerCreateGalleryComplete(result);
                client.Dispose();
            };

            try
            {
                ThreadPool.QueueUserWorkItem((object state) =>
                {
                    try
                    {
                        client.DownloadStringAsync(CREATE_GALLERY_URI);
                    }
                    catch (WebException e)
                    {
                        Debug.WriteLine("Failed to access CreateGallery API: " + e.Message);
                        this.TriggerCreateGalleryFailed(e);
                        client.Dispose();
                    }
                });
            }
            catch (Exception e)
            {
                Debug.WriteLine("Failed to submit task to thread pool: " + e.Message);
                this.TriggerCreateGalleryFailed(e);
                client.Dispose();
            }
        }

        public Cancellable UploadItem(String editorId, String key, String filename, String desiredFilename = null)
        {
            // Not worth checking for file existence or other stuff, as either Path.GetFileName or the upload
            // will check & fail
            String name = desiredFilename == null ? Path.GetFileName(filename) : desiredFilename;

            WebClient client = this.CreateAndSetupWebClient();
            client.QueryString.Add("editor_id", editorId);
            client.QueryString.Add("key", key);
            client.QueryString.Add("filename", UrlEncode(name));

            client.UploadFileCompleted += delegate(object sender, UploadFileCompletedEventArgs e)
            {
                if (e.Error != null)
                {
                    Debug.WriteLine("UploadItem operation failed: " + e.Error.Message);
                    this.TriggerUploadItemFailed(e.Error);
                    client.Dispose();
                    return;
                }

                String response = System.Text.Encoding.UTF8.GetString(e.Result);
                Debug.WriteLine(response);
                UploadItemResult result = JsonConvert.DeserializeObject<UploadItemResult>(response);
                Debug.WriteLine("UploadItem operation successful: " + result);
                this.TriggerUploadItemComplete(result);
                client.Dispose();
            };

            Cancellable cancellable = new CancellableUpload(client);

            try
            {
                ThreadPool.QueueUserWorkItem((object state) =>
                {
                    if (cancellable.IsCancelled())
                    {
                        Debug.WriteLine("Upload already cancelled!");
                        this.TriggerUploadItemFailed(new OperationCanceledException("Cancelled by request"));
                        client.Dispose();
                        return;
                    }

                    try
                    {
                        client.UploadFileAsync(UPLOAD_ITEM_URI, filename);
                    }
                    catch (WebException e)
                    {
                        Debug.WriteLine("Failed to access UploadItem API: " + e.Message);
                        this.TriggerUploadItemFailed(e);
                        client.Dispose();
                    }
                });
            }
            catch (Exception e)
            {
                Debug.WriteLine("Failed to submit task to thread pool: " + e.Message);
                this.TriggerUploadItemFailed(e);
                client.Dispose();
            }

            return cancellable;
        }

        /// <summary>
        /// Saves a gallery and makes it publicly accessible.
        /// </summary>
        /// <param name="name">Desired name for the gallery</param>
        /// <param name="galleryEditorId">Gallery editor ID (obtained when created)</param>
        /// <param name="key">Editor key for the gallery (obtained when created)</param>
        /// <param name="items">The order in which the items will be displayed in the gallery</param>
        public void SaveGallery(String name, String galleryEditorId, String key, String[] items)
        {
            // Get a pre-configured web client
            WebClient client = this.CreateAndSetupWebClient();

            // build the item list (the order in which the items will be shown)
            StringBuilder builder = new StringBuilder().Append('[');
            for (int i = 0; i < items.Length; i++)
            {
                builder.Append("'").Append(items[i]).Append("',");
            }
            builder.Remove(builder.Length - 1, 1).Append(']');

            // Add the post data - must be as a string because WebClient doesn't do UrlEncode on all the
            // characters it's supposed to do. If I do UrlEncode() before submitting the webclient will
            // also perform url encoding on stuff that's already url encoded.
            StringBuilder data = new StringBuilder();
            data.Append("name=").Append(name)
            .Append("&id=").Append(galleryEditorId)
            .Append("&key=").Append(key)
            .Append("&items=").Append(UrlEncode(builder.ToString()));

            client.Headers["Content-Type"] = "application/x-www-form-urlencoded";

            // register the completion/error listener
            client.UploadStringCompleted += delegate(object sender, UploadStringCompletedEventArgs e)
            {
                if (e.Error != null)
                {
                    Debug.WriteLine("SaveGallery operation failed: " + e.Error.Message);
                    this.TriggerUploadItemFailed(e.Error);
                    client.Dispose();
                    return;
                }

                Debug.WriteLine("SaveGallery operation successful.");
                this.TriggerSaveGalleryComplete();
                client.Dispose();
            };

            // submit as an asynchronous task
            try
            {
                ThreadPool.QueueUserWorkItem((object state) =>
                {
                    try
                    {
                        client.UploadStringAsync(SAVE_GALLERY_URI, "POST", data.ToString());
                    }
                    catch (WebException e)
                    {
                        Debug.WriteLine("Failed to access SaveGallery API: " + e.Message);
                        this.TriggerSaveGalleryFailed(e);
                        client.Dispose();
                    }
                });
            }
            catch (Exception e)
            {
                Debug.WriteLine("Failed to submit task to thread pool: " + e.Message);
                this.TriggerSaveGalleryFailed(e);
                client.Dispose();
            }
        }

        #endregion

        #region Event Triggering
        private void TriggerCreateGalleryComplete(CreateGalleryResult result)
        {
            if (this.CreateGalleryComplete != null)
            {
                this.CreateGalleryComplete.Invoke(this, result);
            }
        }

        private void TriggerCreateGalleryFailed(Exception e)
        {
            if (this.CreateGalleryFailed != null)
            {
                this.CreateGalleryFailed.Invoke(this, e);
            }
        }

        private void TriggerUploadItemComplete(UploadItemResult result)
        {
            if (this.UploadItemComplete != null)
            {
                this.UploadItemComplete.Invoke(this, result);
            }
        }

        private void TriggerUploadItemFailed(Exception e)
        {
            if (this.UploadItemFailed != null)
            {
                this.UploadItemFailed.Invoke(this, e);
            }
        }

        private void TriggerSaveGalleryComplete()
        {
            if (this.SaveGalleryComplete != null)
            {
                this.SaveGalleryComplete.Invoke(this);
            }
        }

        private void TriggerSaveGalleryFailed(Exception e)
        {
            if (this.SaveGalleryFailed != null)
            {
                this.SaveGalleryFailed.Invoke(this, e);
            }
        }

        public static String UrlEncode(String parameter)
        {
            if (string.IsNullOrEmpty(parameter))
            {
                return string.Empty;
            }

            String value = Uri.EscapeDataString(parameter);

            // Uri.EscapeDataString escapes with lowercase characters, convert to uppercase
            value = Regex.Replace(value, "(%[0-9a-f][0-9a-f])", c => c.Value.ToUpper());

            // not escaped by Uri.EscapeDataString() but needed to be escaped
            value = value
                .Replace("(", "%28")
                .Replace(")", "%29")
                .Replace("$", "%24")
                .Replace("!", "%21")
                .Replace("*", "%2A")
                .Replace("'", "%27");

            // characters escaped by Uri.EscapeDataString() that needs to be sent unescaped
            value = value.Replace("%7E", "~");

            return value;
        }
        #endregion

        #region Private helpers

        private WebClient CreateAndSetupWebClient()
        {
            WebClient client = new WebClient();
            if (this.Proxy != null)
            {
                client.Proxy = this.Proxy;
            }
            client.Headers["User-Agent"] = USER_AGENT;
            return client;
        }
        #endregion
    }
}
