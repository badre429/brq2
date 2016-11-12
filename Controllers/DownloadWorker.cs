

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Humanizer;

namespace WebApplication.Controllers
{
    public class DownloadWorker
    {

        public async Task DownloadFileAsync( )
        {
            var cancellationToken = new CancellationTokenSource();var token =cancellationToken.Token;
            var client = new HttpClient();
            var handler = new HttpClientHandler();
            if (!string.IsNullOrWhiteSpace(this.cookie))
            {

                client.DefaultRequestHeaders.Add("Cookie", this.cookie);
            }
            var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);

            if (!response.IsSuccessStatusCode)
            {

                var e = new Exception(string.Format("The request returned with HTTP status code {0}", response.StatusCode));

                this.Completed(e);
                throw e;
            }

            var total = response.Content.Headers.ContentLength.HasValue ? response.Content.Headers.ContentLength.Value : -1L;
            var canReportProgress = total != -1;
            using (var sw = System.IO.File.Create(System.IO.Path.Combine(sm, this.filename)))
            {


                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    var totalRead = 0L;
                    var buffer = new byte[4096];
                    var isMoreToRead = true;

                    do
                    {
                        token.ThrowIfCancellationRequested();

                        var read = await stream.ReadAsync(buffer, 0, buffer.Length, token);

                        if (read == 0)
                        {
                            isMoreToRead = false;
                        }
                        else
                        {
                            var data = new byte[read];
                            buffer.ToList().CopyTo(0, data, 0, read);

                            // TODO: put here the code to write the file to disk
                            await sw.WriteAsync(data, 0, read);
                            totalRead += read;

                            if (canReportProgress)
                            {
                                //progress.Report((totalRead * 1d) / (total * 1d) * 100);
                                this.ProgressChanged((totalRead * 1d) / (total * 1d) * 100, total, totalRead);
                            }
                        }
                    } while (isMoreToRead);
                }
            }
            this.Completed();
        }
        private string cookie;
        private string url;
        private string filename;
        private string sm;
        public DownloadWorker(string Url, String Filename, string Mainforlder, string Cookie)
        {
            url = Url;
            cookie = Cookie;
            filename = Filename;
            sm = Mainforlder;
            var tsk = new Task(() => download());
            tsk.Start();
        }
        private async void download()
        {

            // var sm = Server.MapPath("~/tfs");
            try
            {
                 
                var uri = new Uri(url);
                if (string.IsNullOrWhiteSpace(filename))
                {
                    filename = uri.LocalPath;
                    if (filename.Contains("/"))
                    {
                        var i = filename.LastIndexOf("/");
                        filename = filename.Substring(i + 1);
                    }
                }
               
                
                DownloaderController.currentFiles.Add(new DownFiles() { Name = filename, Url = url, lastDate = DateTime.Now });

                await DownloadFileAsync();
                /*  webClient.reque += new AsyncCompletedEventHandler(Completed);
                  webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
                  webClient.DownloadFileAsync(uri, sm + "/" + filename);*/
            }
            catch (Exception ex)
            {
                DownloaderController.LastError = ex.Message;
                this.Completed();
            }

        }

        private void ProgressChanged(double ProgressPercentage, long TotalBytesToReceive, long BytesReceived)
        {
            //   progressBar.Value = e.ProgressPercentage;
            var x = DownloaderController.currentFiles.FirstOrDefault(o => o.Name.ToLower() == filename.ToLower());
            if (x != null)
            {
                x.Progress = ProgressPercentage;
                x.Size = Convert.ToDouble(TotalBytesToReceive).Bytes().Humanize(".00");
                var time = DateTime.Now - x.lastDate;
                var size = BytesReceived - x.last;
                x.Speed = (Convert.ToDouble(size) / time.TotalSeconds).Bytes().Humanize(".00") + "/sec";
            }

        }

        private void Completed(Exception e = null)
        {
            var x = DownloaderController.currentFiles.FirstOrDefault(o => o.Name.ToLower() == filename.ToLower());

            if (e != null)
            {

                DownloaderController.LastError = e.Message;
                if (System.IO.File.Exists(sm + "/" + filename))
                {
                    System.IO.File.Delete(sm + "/" + filename);
                }

            }
            if (x != null)
            {

                x.Progress = 100;
                DownloaderController.currentFiles.Remove(x);
            };
        }
    }
}