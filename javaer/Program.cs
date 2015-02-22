using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using HtmlAgilityPack;
using System.Management;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace javaer
{
    enum ExitCodes : int
    {
        Success = 0,
        CannotConnect = 1,
        ErrorUninstalling = 2,
        ErrorDownloading = 3,
        ErrorInstalling = 4,
        UnknownError = 9001
    }

    class Program
    {        
        [DllImport("kernel32.dll")]
        public static extern Int32 AllocConsole(); //Used to enable console if application is not silent
        
        private static string silentArg = "-s";
        private static string uninstallAllArg = "-uninstallall";
        private static string x64Arg = "-64";
        private static List<string> oArgs;
        private static bool ArgsCheck(string sSwitch) // checks if a arg is used. ignores case.
        {
            return oArgs.Any(s => s.Equals(sSwitch, StringComparison.OrdinalIgnoreCase));
        }

        private static void Exit(ExitCodes exitCode)
        {
            if (!ArgsCheck(silentArg))
            {
                Console.WriteLine("Program exited with code {0} : {1}", (int)exitCode, exitCode.ToString());
                Console.ReadKey();
            }
            Environment.Exit((int)exitCode);
        }
        


        static void Main(string[] args)
        {
            oArgs = args.ToList<string>();
            if (!ArgsCheck(silentArg)) { AllocConsole(); } //Open console window if not silent.
            ExitCodes exitCode;


            Uri proxyServer = null; // If this stays null then it is ignored.
            var proxyFile = Path.Combine(Directory.GetCurrentDirectory(), "proxy.config");
            if (File.Exists(proxyFile)) //Check for proxy data file
            {
                string read;
                using (StreamReader reader = new StreamReader(proxyFile))
                {
                    read = reader.ReadToEnd().Trim();
                }
                proxyServer = TestUrl(read);
            }
            else if (ArgsCheck("-proxy:")) //Check for command line proxy data
            {                
                proxyServer = TestUrl(oArgs.Find(stringToCheck => stringToCheck.ToLower().Contains(@"-proxy:")).Replace("-proxy:", string.Empty));
            }

            JavaToolSet javaTools;
            if (proxyServer != null)
            {
                Console.WriteLine("Using provided proxy server setting.");
                javaTools = new JavaToolSet(proxyServer);
            }
            else
            {
                javaTools = new JavaToolSet();
            }
            
            var newestVersion = string.Empty;
            //Get most recent version from Java site.
            try
            {
                Console.WriteLine("Checking Java release version.");
                newestVersion = javaTools.MostRecent();
            }
            catch
            {
                Console.WriteLine("Cannot reach Java.com. Exiting.");
                Exit(ExitCodes.CannotConnect);                
            }

            Console.WriteLine("Checking for installed Java.");
            var javas = javaTools.GetInstalled();
            var bitWise = ArgsCheck(x64Arg);
            if (javas.Count > 0)
            {
                foreach(var j in javas)
                {
                    Console.WriteLine("Found: {0}.", j.FullVersion);
                }


                if (ArgsCheck(uninstallAllArg)) 
                {
                    Console.WriteLine("Removing all installed Java versions.");
                    UninstallList(javaTools, javas); 
                }
                else
                {
                    var oldJava = GetOldVersions(javas, newestVersion);
                    if (oldJava.Count > 0)
                    {
                        Console.WriteLine("Removing old versions");
                        UninstallList(javaTools, oldJava);
                    }
                }

                var results = CheckListForVersion(javaTools.GetInstalled(), newestVersion, bitWise);

                if (results == null)
                {
                    exitCode = DownloadAndInstall(javaTools, newestVersion, bitWise);
                }
                else
                {
                    Console.WriteLine("Newest 32 bit Java already installed.");
                    exitCode = ExitCodes.Success;
                }
            }
            else
            {
                Console.WriteLine("No Java instances found.");
                exitCode = DownloadAndInstall(javaTools, newestVersion, bitWise);                
            }
            
            Exit(exitCode);
        }

        private static Uri TestUrl(string read)
        {
            Uri uriResult;
            if (Uri.TryCreate(read, UriKind.Absolute, out uriResult))
            {
                if (uriResult != null)
                {
                    if (uriResult.Scheme == Uri.UriSchemeHttps || uriResult.Scheme == Uri.UriSchemeHttp)
                    {
                        return new Uri(read);
                    }
                }
            }
            return null;
        }

        private static void UninstallList(JavaToolSet javaTools, List<JavaData> list)
        {
            if (list.Count > 0)
            {
                foreach (var java in list)
                {
                    Console.Write("Uninstalling {0}.", java.FullVersion);
                    var result = javaTools.Uninstall(java);
                    Console.WriteLine(" Exit Code: {0}", result);
                }
            }
        }

        private static JavaData CheckListForVersion(List<JavaData> javas, string newestVersion, bool bit)
        {
            var results = javas.Find(prod => prod.Version == newestVersion && prod.x64 == bit);
            return results;
        }

        private static List<JavaData> GetOldVersions(List<JavaData> javas, string newestVersion)
        {
            var results = new List<JavaData>();
            if (javas != null)
            {
                foreach (var java in javas)
                {
                    if (JavaToolSet.ConvertVersion(newestVersion) > JavaToolSet.ConvertVersion(java.Version))
                    {
                        results.Add(java);
                    }
                }
            }
            return results;
        }

        private static ExitCodes DownloadAndInstall(JavaToolSet java, string newestVersion, bool bit)
        {
            var version = GetVersionString(newestVersion, bit);

            if (!ArgsCheck(silentArg)) //attach to download progress indicator event if not silent.
            {
                java.DownloadProgressChanged += DownloadProgressCallback;
                java.DownloadCompleted += new System.ComponentModel.AsyncCompletedEventHandler((s, e) => Console.WriteLine("\rDownloaded complete.                                        "));
            }

            Console.WriteLine("Downloading {0}", version);
            if (java.Download(bit))
            {
                Console.Write("Installing {0}... ", version);
                var exitCode = java.InstallDownloaded();
                if (exitCode == 0)
                {                    
                    var javas = java.GetInstalled();
                    if (CheckListForVersion(javas, newestVersion, bit) != null)
                    {
                        Console.WriteLine("Installed Successfully.");
                        return ExitCodes.Success;
                    }
                    Console.WriteLine("Unknown Error. install ran without but new version not installed");
                    return ExitCodes.UnknownError;

                }
                Console.WriteLine("Installer Error {0}.", exitCode);
                return ExitCodes.ErrorInstalling;
            }
            Console.WriteLine("Error Downloading.");
            return ExitCodes.ErrorDownloading;
        }

        private static void DownloadProgressCallback(object sender, DownloadProgressChangedEventArgs e)
        {
            // Displays the operation identifier, and the transfer progress.
            Console.Write("\rDownloaded {0} of {1} bytes. {2} % complete...",
                e.BytesReceived,
                e.TotalBytesToReceive,
                e.ProgressPercentage);
        }

        private static string GetVersionString(string newestVersion, bool bit)
        {
            var version = string.Format("Java {0} {1}.", newestVersion, BoolToStringBits(bit));
            return version;
        }

        private static string BoolToStringBits(bool bit)
        {
            string bits;
            if (bit) { bits = "64-bit"; }
            else { bits = "32-bit"; }
            return bits;
        }
    }
}
