using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace javaer
{
    public class JavaToolSet
    {
        private void RaiseDownloadProgressChanged(DownloadProgressChangedEventArgs e)
        {
            if (DownloadProgressChanged != null)
            {
                DownloadProgressChanged(this, e);
            }
        }
        public event DownloadProgressChangedEventHandler DownloadProgressChanged;
        private void RaiseDownloadComplete(AsyncCompletedEventArgs e)
        {
            if (DownloadCompleted != null)
            {
                DownloadCompleted(this, e);
            }
        }
        public event AsyncCompletedEventHandler DownloadCompleted;

        private string java32LinkText = "Download Java software for Windows Offline";
        private string java64LinkText = "Download Java software for Windows (64-bit)";
        private string installArguments = @"/s AUTO_UPDATE=0 WEB_ANALYTICS=0";
        private string uninstallArguments = @"/x {0} /q";
        private string downloadURL = "http://www.java.com/en/download/manual.jsp";
        private string newestVersionURL = "http://java.com/applet/JreCurrentVersion2.txt";
        private string downloadFilename = "CurrentJava.exe";
        private string eightKey = @"26A24AE4-039D-4CA4-87B4-2F8{0}{1}F0";
        private string sevenKey = @"26A24AE4-039D-4CA4-87B4-2F0{0}{1}FF";
        private string sixKey =   @"26A24AE4-039D-4CA4-87B4-2F8{0}{1}FF";
        private Uri proxy;

        public JavaToolSet(Uri Proxy = null)
        {
            if (Proxy != null)
            {
                proxy = Proxy;
            }
        }

        public static int ConvertVersion(string version)
        {
            return Convert.ToInt32(Regex.Replace(version, @"[^\d]", string.Empty));
        }

        public List<JavaData> GetInstalled()
        {
            var javas = new List<JavaData>();
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432")))
            {
                //Is 64 bit machine
                var java32 = JavaFolderCheck(false, @"Program Files (x86)");
                var java64 =JavaFolderCheck(true, "Program Files");
                javas = java32.Concat(java64).ToList();
            }
            else
            {
                //is 32 bit machine
                javas = JavaFolderCheck(false, @"Program Files");
            }
            return javas;
        }
        private List<JavaData> JavaFolderCheck(bool x64, string pf)
        {
            var javas = new List<JavaData>();
            var path = string.Format(@"c:\{0}\", pf);
            var javaPath = Path.Combine(path, "Java");
            if (Directory.Exists(javaPath))
            {
                var folders = Directory.GetDirectories(javaPath);
                foreach (string folder in folders)
                {
                    var fullpathfile = Path.Combine(folder, @"bin\java.exe");
                    if (File.Exists(fullpathfile))
                    {
                        DirectoryInfo folder1 = new DirectoryInfo(folder);
                        var fulllocalpath = string.Format(@"c:\{1}\Java\{0}\bin\java.exe", folder1.Name, pf);
                        var result = JavaVersion(fulllocalpath, "-version");
                        if (!result.Contains("ERROR"))
                        {
                            javas.Add(new JavaData("Java", result, FileVersionInfo.GetVersionInfo(fulllocalpath).ProductVersion.ToString(), fulllocalpath, folder, x64));
                        }
                    }
                }
            }
            return javas;
        }
        private string JavaVersion(string filename, string arguments)
        {
            string result = string.Empty;
            string standardError = string.Empty;
            string standardOutput = string.Empty;
            
            var exit = StartProcess(filename, arguments, ref standardError, ref standardOutput);      

            if (standardError.Contains("java version") == true)
            {
                result = standardError.Split(new char[] { '\"', '\"' })[1];
            }
            else
            {
                result = "ERROR";
            }
            return result;
        }

        private int StartProcess(string filename, string arguments, ref string standardError, ref string standardOutput)
        {
            int exit = 9002;
            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                //RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                FileName = filename,
                Arguments = arguments,
                WorkingDirectory = Directory.GetCurrentDirectory()
            };

            try
            {
                var pProcess = System.Diagnostics.Process.Start(startInfo);
                standardOutput = pProcess.StandardOutput.ReadToEnd();
                standardError = pProcess.StandardError.ReadToEnd();
                pProcess.WaitForExit();
                exit = pProcess.ExitCode;
            }
            catch
            {
                return exit;
            }
            return exit;
        }

        public string GetCurrentVersion()
        {
            using (WebClient client = new WebClient())
            {
                client.Proxy = new WebProxy(proxy);
                Stream stream = client.OpenRead(newestVersionURL);
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd().Trim();
                }
            }
        }

        public int InstallDownloaded()
        {
            string fileName = string.Empty;
            string myAppPath = Directory.GetCurrentDirectory();
            int exit;
            if (File.Exists(Path.Combine(myAppPath, downloadFilename)))
            {
                fileName = Path.Combine(myAppPath, downloadFilename);
            }
            else
            {
                return 9001;
            }
            string standardError = string.Empty;
            string standardOutput = string.Empty;
            exit = StartProcess(fileName, installArguments, ref standardError, ref standardOutput);
            File.Delete(fileName);
            return exit;

        }

        private bool ProxyOnPreRequest(HttpWebRequest request)
        {
            WebProxy myProxy = new WebProxy(proxy);
            request.Proxy = myProxy;
            return true; // ok, go on
        }

        public bool Download(bool x64)
        {
            string textLink;
            try
            {
                HtmlWeb htmlWeb = new HtmlWeb();
                htmlWeb.PreRequest = new HtmlAgilityPack.HtmlWeb.PreRequestHandler(ProxyOnPreRequest);

                var document = htmlWeb.Load(downloadURL);

                if (x64) { textLink = java64LinkText; }
                else { textLink = java32LinkText; }

                var directURL = document.DocumentNode.SelectSingleNode(string.Format("//a[@title='{0}']", textLink)).Attributes["href"].Value;

                    using (WebClient client = new WebClient())
                    {
                        // Create a timer with a two second interval.

                        
                        client.Proxy = new WebProxy(proxy);
                        // Specify that the DownloadFileCallback method gets called when the download completes.                        
                        client.DownloadFileCompleted += new AsyncCompletedEventHandler((sender, e) => { RaiseDownloadComplete(e); });
                        //Create a timer for a download timeout
                        var aTimer = new System.Timers.Timer(60000);
                        // Specify a progress notification handler.
                        client.DownloadProgressChanged += new DownloadProgressChangedEventHandler((sender, e) => 
                        {
                            // reset timer if there is progress
                            aTimer.Stop(); aTimer.Start(); 
                            RaiseDownloadProgressChanged(e); 
                        }); 
                        var downloadTask = client.DownloadFileTaskAsync(new Uri(directURL), downloadFilename);                       
                        aTimer.Elapsed += new System.Timers.ElapsedEventHandler((s, e) => { client.CancelAsync(); });
                        downloadTask.Wait();     
                    }
            }
            catch
            {
                return false;
            }
            return true;
        }

        public int Uninstall(JavaData java)
        {
            string standardError = string.Empty;
            string standardOutput = string.Empty;
            var guid = BuildGUID(java);
            if (!String.IsNullOrEmpty(guid))
            {
                return StartProcess("msiexec.exe", string.Format(uninstallArguments, guid), ref standardError, ref standardOutput);
            }
            else
            {
                return 9001;
            }
        }

        private string BuildGUID(JavaData java)
        {
            var key = string.Empty;
            var bits = "32";
            if (java.x64) { bits = "64"; }
            string strippedVersion = ConvertVersion(java.Version).ToString();
            var majorVersion = (int)char.GetNumericValue(strippedVersion[1]);

            switch (majorVersion)
            {
                case 8: key = eightKey;
                    break;
                case 7: key = sevenKey;
                    break;
                case 6: key = sixKey;
                    break;
                default:
                    return key;
            }

            var guid = "{" + string.Format(key, bits, strippedVersion) + "}";
            return guid;
        }
    }
}
