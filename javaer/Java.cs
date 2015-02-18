using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace javaer
{
    public class Java
    {
        private string java32LinkText = "Download Java software for Windows Offline";
        private string java64LinkText = "Download Java software for Windows (64-bit)";
        private string installString = @"/s AUTO_UPDATE=0 WEB_ANALYTICS=0";
        private string downloadURL = "http://www.java.com/en/download/manual.jsp";
        private string newestVersionURL = "http://java.com/applet/JreCurrentVersion2.txt";
        private string downloadFilename = "CurrentJava.exe";
        
        
        private bool is64bit;
        private List<RemoteProgramData> javas = new List<RemoteProgramData>();
        
        public Java()
        {
        is64bit = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"));
        }

        public List<RemoteProgramData> GetInstalled()
        {
            if (is64bit)
            {
                //Is 64 bit machine
                JavaFolderCheck("32-Bit", @"Program Files (x86)");
                JavaFolderCheck("64-Bit", "Program Files");
            }
            else
            {
                //is 32 bit machine
                JavaFolderCheck(", 32-Bit", @"Program Files");
            }
            return javas;
        }
        private void JavaFolderCheck(string bits, string pf)
        {
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
                            javas.Add(new RemoteProgramData { Version = result, Name = "Java", FullPath = fulllocalpath, InstallFolder = folder, Bit = bits });
                        }
                    }
                }
            }
        }
        private string JavaVersion(string filename, string arguments)
        {
            string result = string.Empty;
            string standardError = string.Empty;
            string standardOutput = string.Empty;
            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                //RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                FileName = filename,
                Arguments = arguments
            };

            try
            {
                var pProcess = System.Diagnostics.Process.Start(startInfo);
                standardOutput = pProcess.StandardOutput.ReadToEnd();
                standardError = pProcess.StandardError.ReadToEnd();
                pProcess.WaitForExit();
            }
            catch (Exception e)
            {
                Console.Write(e.ToString());
            }            

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

        public string MostRecent()
        {            
            WebClient client = new WebClient();
            Stream stream = client.OpenRead(newestVersionURL);
            StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd().Trim();
        }

        public bool InstallDownloaded()
        {
            string fileName = string.Empty;
            string myAppPath = Directory.GetCurrentDirectory();
            var test = Path.Combine(myAppPath, downloadFilename);
            if (File.Exists(Path.Combine(myAppPath, downloadFilename)))
            {
                fileName = Path.Combine(myAppPath, downloadFilename);
            }
            else
            {
                return false;
            }
            try
            {
                

                string result = string.Empty;
                string standardError = string.Empty;
                string standardOutput = string.Empty;
                var startInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    //RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    FileName = fileName,
                    Arguments = installString,
                    WorkingDirectory = myAppPath
                };

                    var pProcess = System.Diagnostics.Process.Start(startInfo);
                    standardOutput = pProcess.StandardOutput.ReadToEnd();
                    standardError = pProcess.StandardError.ReadToEnd();
                    pProcess.WaitForExit();
                    var exit = pProcess.ExitCode;

            }
            catch(Exception e)
            {
                Console.Write(e.ToString());
                return false;
            }
            return true;

        }

        public bool Download(bool x64)
        {
            string textLink;

            try
            {
                HtmlWeb htmlWeb = new HtmlWeb();
                var document = htmlWeb.Load(downloadURL);

                if (x64) { textLink = java64LinkText; }
                else { textLink = java32LinkText; }

                var directURL = document.DocumentNode.SelectSingleNode(string.Format("//a[@title='{0}']", textLink)).Attributes["href"].Value;

                using (WebClient Client = new WebClient())
                {
                    Client.DownloadFile(directURL, downloadFilename);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

    }
}
