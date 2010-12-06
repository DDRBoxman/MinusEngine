using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BiasedBit.MinusEngine
{
    public class UploadItemResult
    {
        public UploadItemResult()
        {
        }

        [JsonProperty("id")]
        public String Id
        {
            get;
            set;
        }

        [JsonProperty("height")]
        public int Height
        {
            get;
            set;
        }

        [JsonProperty("width")]
        public int Width
        {
            get;
            set;
        }

        [JsonProperty("filesize")]
        public String Filesize
        {
            get;
            set;
        }

        public override string ToString()
        {
            return new StringBuilder("UploadItemResult{")
                .Append("Id=").Append(this.Id)
                .Append(", Height=").Append(this.Height)
                .Append(", Width=").Append(this.Width)
                .Append(", Filesize=").Append(this.Filesize)
                .Append('}').ToString();
        }
    }
}
