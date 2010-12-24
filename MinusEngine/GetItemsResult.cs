using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace BiasedBit.MinusEngine
{
    public class GetItemsResult
    {
        #region Constructors
        public GetItemsResult()
        {
        }

        public GetItemsResult(String readonlyUrl, String title, String[] items)
        {
            this.ReadonlyUrl = readonlyUrl;
            this.Title = title;
            this.Items = items;
        }
        #endregion

        #region Fields
        [JsonProperty("read_only_url_for_gallery")]
        public String ReadonlyUrl { get; set; }

        [JsonProperty("gallery_title")]
        public String Title { get; set; }

        [JsonProperty("items_gallery")]
        public String[] Items { get; set; }
        #endregion

        #region Low level overrides
        public override string ToString()
        {
            return new StringBuilder("GetItemsResult{ReadonlyUrl=")
                .Append(this.ReadonlyUrl)
                .Append(", GalleryTitle=")
                .Append(this.Title)
                .Append(", Items=")
                .Append(String.Join(", ", this.Items))
                .Append('}').ToString();
        }
        #endregion
    }
}
