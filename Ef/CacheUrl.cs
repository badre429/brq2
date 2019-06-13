namespace GeoMapDownloader
{


    // GeoMapDownloader.CacheUrl
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;

    public class CacheUrl
    {
        public long Id
        {
            get;
            set;
        }

        public string Url
        {
            get;
            set;
        }

        public byte[] Data
        {
            get;
            set;
        }
        [Column("_Headers")]
        public string _Headers { get; set; }
        private Dictionary<string, string> _headers;
        [NotMapped]
        public Dictionary<string, string> Headers
        {
            get
            {
                if (_headers == null)
                {
                    if (!string.IsNullOrEmpty(_Headers))
                    {
                        this._headers = JsonSerializer.Parse<Dictionary<string, string>>(_Headers);// JsonConvert.DeserializeObject<Dictionary<string, string>>(_Headers);
                    }
                    else
                    {
                        this._headers = new Dictionary<string, string>();
                    }
                }
                return _headers;
            }
            set
            {
                this._Headers = JsonSerializer.ToString<Dictionary<string, string>>(value);
                _headers = value;
            }
        }


        public string Action
        {
            get;
            set;
        }

        public string Mime
        {
            get;
            set;
        }
    }
}
