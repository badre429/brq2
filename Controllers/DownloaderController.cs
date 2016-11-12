using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Humanizer;
namespace WebApplication.Controllers
{
    public class DownloaderController : Controller
    {
        private IHostingEnvironment _env;
        public DownloaderController(IHostingEnvironment env)
        {
            
            _env = env;
        }
        public static string LastError { get; set; }

        public static string LastUrl { get; set; }
        // GET: ApplicationUsers/Delete/5
        public ActionResult Delete(string filename)
        {
            try
            {
                var sm = _env.WebRootPath + "/tfs";
                if (System.IO.File.Exists(sm + "/" + filename))
                {
                    System.IO.File.Delete(sm + "/" + filename);
                }
                var x = currentFiles.FirstOrDefault(o => o.Name.ToLower() == filename.ToLower());
                if (x != null)
                {
                    currentFiles.Remove(x);
                }
            }
            catch (Exception ex)
            {
                DownloaderController.LastError = DateTime.Now.ToString() + " :" + DateTime.Now.ToString() + " :" + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // POST: ApplicationUsers/Delete/5

        public ActionResult DeleteConfirmed(string filename)
        {
            try
            {
                var sm = _env.WebRootPath + "/tfs";
                if (System.IO.File.Exists(sm + "/" + filename))
                {
                    System.IO.File.Delete(sm + "/" + filename);
                }
                var x = currentFiles.FirstOrDefault(o => o.Name.ToLower() == filename.ToLower());
                if (x != null)
                {
                    currentFiles.Remove(x);
                }
            }
            catch (Exception ex)
            {
                DownloaderController.LastError = DateTime.Now.ToString() + " :" + DateTime.Now.ToString() + " :" + ex.Message;
            }

            return RedirectToAction("Index");
        }

        public static List<DownFiles> currentFiles = new List<DownFiles>();
        // GET: Downloder
        public ActionResult Index()
        {
            try
            {
                ViewBag.LastError = LastError;

                currentFiles = currentFiles.Where(o => o.Progress < 100).ToList();
                var sm = _env.WebRootPath + "/tfs/";
                ViewBag.Path=sm;
                
                var di = new System.IO.DirectoryInfo(sm);
                var list =
                    Directory.EnumerateFiles(sm,"*.*",SearchOption.TopDirectoryOnly ).Where(o => !currentFiles.Any(fl => fl.Name.ToLower() == o.Name.ToLower()))
                        .Select(
                            o =>
                                new DownFiles()
                                {
                                    Name = o.Name,
                                    Size = Convert.ToDouble(o.Length).Bytes().Humanize(".00"),
                                    DownloadDate = o.CreationTimeUtc
                                }).OrderByDescending(o => o.DownloadDate);
                var ret = list.Where(o => o.DownloadDate > DateTime.Now.AddDays(-8)).ToList();

                var del = list.Where(o => o.DownloadDate <= DateTime.Now.AddDays(-8)).ToList();
                foreach (var fl in del)
                {
                    var Path = sm + fl.Name;
                    if (System.IO.File.Exists(Path))
                    {
                        System.IO.File.Delete(Path);
                    }
                }
                return View(ret.Where(o => !o.Name.ToLower().Contains("web.config")).ToList());
            }
            catch (Exception ex)
            {

                DownloaderController.LastError = DateTime.Now.ToString() + " :" + ex.Message;
                LastError = ex.Message;
                return View("Error");
            }
        }
        public ActionResult Direct()
        {
            try
            {
                ViewBag.LastError = LastError;

                currentFiles = currentFiles.Where(o => o.Progress < 100).ToList();
                var sm = _env.WebRootPath + "/tfs/";
                var di = new System.IO.DirectoryInfo(sm);
                
                var list =
                    di.EnumerateFiles().Where(o => !currentFiles.Any(fl => fl.Name.ToLower() == o.Name.ToLower()))
                        .Select(
                            o =>
                                new DownFiles()
                                {
                                    Name = o.Name,
                                    Size = Convert.ToDouble(o.Length).Bytes().Humanize(".00"),
                                    DownloadDate = o.CreationTimeUtc
                                }).OrderByDescending(o => o.DownloadDate);
                var ret = list.Where(o => o.DownloadDate > DateTime.Now.AddDays(-8)).ToList();

                var del = list.Where(o => o.DownloadDate <= DateTime.Now.AddDays(-8)).ToList();
                foreach (var fl in del)
                {
                    var Path = sm + fl.Name;
                    if (System.IO.File.Exists(Path))
                    {
                        System.IO.File.Delete(Path);
                    }
                }
                return View(ret.Where(o => !o.Name.ToLower().Contains("web.config")).OrderBy(o => o.DownloadDate).ToList());
            }
            catch (Exception ex)
            {

                DownloaderController.LastError = DateTime.Now.ToString() + " :" + ex.Message;
                LastError = ex.Message;
                return View("Error");
            }
        }
        public ActionResult MultiDownload(DownFiles model)
        {
            try
            {

                if (model == null)
                {
                    model = new DownFiles();
                    return View(model);
                }
                if (string.IsNullOrWhiteSpace(model.Url))
                {
                    return View(model);
                }
                LastUrl = model.Url +" "+ model.Name;

                if (model.Name == null) model.Name = "";
                var allurls = model.Url.Split('\n').ToList();
                var allnames = model.Name.Split('\n').ToList();
                for (int i = 0; i < allurls.Count(); i++)
                {
                    string cname = null;
                    if (i < allnames.Count()) cname = allnames[i].Trim();
                    LastUrl+=" "+cname+" "+allurls[i].Trim();
                    var x = new DownloadWorker(allurls[i].Trim(), cname, _env.WebRootPath + "/tfs", model.Cookiee);
                }
                // var x = new DownloadWorker(model.Url,  model.Name, _env.WebRootPath+"/tfs",model.Cookiee);
                return RedirectToAction("Progress");
            }
            catch (Exception ex)
            {

                DownloaderController.LastError = DateTime.Now.ToString() + " :" + ex.Message;
                LastError = ex.Message;
                return View("Error");
            }
        }
        public ActionResult Download(DownFiles model)
        {
            try
            {


                LastUrl = model.Url;
                if (model == null)
                {
                    model = new DownFiles();
                    return View(model);
                }
                if (string.IsNullOrWhiteSpace(model.Url))
                {
                    return View(model);
                }

                var x = new DownloadWorker(model.Url, model.Name, _env.WebRootPath + "/tfs", model.Cookiee);
                return RedirectToAction("Progress");
            }
            catch (Exception ex)
            {

                DownloaderController.LastError = DateTime.Now.ToString() + " :" + ex.Message;
                LastError = ex.Message;
                return View("Error");
            }
        }
        public ActionResult Progress()
        {
            
            var all = currentFiles;
            ViewBag.LastUrl = LastUrl;
            return View(all.ToList());
        }


    }
}