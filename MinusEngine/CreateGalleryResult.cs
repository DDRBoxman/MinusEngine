using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace BiasedBit.MinusEngine
{
    public class CreateGalleryResult
    {
        public CreateGalleryResult()
        {
        }
        
        public CreateGalleryResult(String editorId, String readerId, String key)
        {
            this.EditorId = editorId;
            this.ReaderId = readerId;
            this.Key = key;
        }

        [JsonProperty("editor_id")]
        public String EditorId
        {
            get;
            set;
        }

        [JsonProperty("reader_id")]
        public String ReaderId
        {
            get;
            set;
        }

        [JsonProperty("key")]
        public String Key
        {
            get;
            set;
        }

        public override string ToString()
        {
            return new StringBuilder("CreateGalleryResult{EditorId=")
                .Append(this.EditorId)
                .Append(", ReaderId=")
                .Append(this.ReaderId)
                .Append(", Key=")
                .Append(this.Key)
                .Append('}').ToString();
        }
    }
}
