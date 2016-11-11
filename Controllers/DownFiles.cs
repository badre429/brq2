using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication.Controllers
{
    public class DownFiles
    {
        
        [DataType(DataType.MultilineText)]
        public string Url { get; set; }

        [DataType(DataType.MultilineText)]
        public string Name { get; set; }
        public string Size { get; set; }
        public string Speed { get; set; }
        public string Cookiee { get; set; } 
        private double _progress;
        public double Progress
        {
            get { return _progress;}
            set { 
                double v=value;
                v=Math.Ceiling(value*100);

                _progress = v/100;
                
                }
        }
        
        public DateTime DownloadDate { get; set; }

        public DateTime lastDate { get; set; }
        public long last { get; set; }
    }
}