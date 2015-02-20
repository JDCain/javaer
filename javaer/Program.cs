using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using HtmlAgilityPack;
using System.Management;
using Microsoft.Win32;
using System.Text.RegularExpressions;

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
        private static string newestVersion;
        private static List<string> oArgs;

        static void Main(string[] args)
        {
            oArgs = args.ToList<string>();
            ExitCodes exitCode;

            Uri proxy = null;
            var proxyFile = Path.Combine(Directory.GetCurrentDirectory(), "proxy.config");
            if (File.Exists(proxyFile))
            {
                string read;
                using (StreamReader reader = new StreamReader(proxyFile))
                {
                    read = reader.ReadToEnd().Trim();
                }
                Uri uriResult;
                if (Uri.TryCreate(read, UriKind.Absolute, out uriResult))
                {
                    if (uriResult != null)
                    {
                        if (uriResult.Scheme == Uri.UriSchemeHttps || uriResult.Scheme == Uri.UriSchemeHttp)
                        {
                            proxy = new Uri(read);
                        }
                    }
                }
            }

            JavaToolSet javaTools;
            if (proxy != null)
            {
                Console.WriteLine("Using provided proxy server setting.");
                javaTools = new JavaToolSet(proxy);
            }
            else
            {
                javaTools = new JavaToolSet();
            }
            
            
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
            if (javas.Count > 0)
            {
                foreach(var j in javas)
                {
                    Console.WriteLine("Found: {0}.", j.FullVersion);
                }


                if (ArgsCheck(@"/uninstallall")) 
                {
                    Console.WriteLine("Removing all installed Java versions.");
                    UninstallList(javaTools, javas); 
                }
                else
                {
                    var oldJava = GetOldVersions(javas);
                    if (oldJava.Count > 0)
                    {
                        Console.WriteLine("Removing old versions");
                        UninstallList(javaTools, oldJava);
                    }
                }
                
                var results = CheckForNewest32bit(javaTools.GetInstalled());

                if (results == null)
                {
                    exitCode = GetAndInstall32(javaTools);
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
                exitCode = GetAndInstall32(javaTools);                
            }
            
            Exit(exitCode);
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

        private static void Exit(ExitCodes exitCode)
        {
            if (!ArgsCheck("/s"))
            {
                Console.WriteLine("Program exited with code {0} : {1}", (int)exitCode, exitCode.ToString());
                Console.ReadKey();
            }
            Environment.Exit((int)exitCode);
        }

        private static bool ArgsCheck(string sSwitch)
        {
            return oArgs.Any(s => s.Equals(sSwitch, StringComparison.OrdinalIgnoreCase));
        }

        private static JavaData CheckForNewest32bit(List<JavaData> javas)
        {
            var results = javas.Find(prod => prod.Version == newestVersion && prod.x64 == false);
            return results;
        }

        private static List<JavaData> GetOldVersions(List<JavaData> javas)
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

        private static ExitCodes GetAndInstall32(JavaToolSet java)
        {
            Console.WriteLine("Downloading Java {0} 32-bit.", newestVersion);
            if (java.Download(false))
            {
                Console.WriteLine("Installing Java {0} 32-bit.", newestVersion);
                var exitCode = java.InstallDownloaded();
                if (exitCode == 0)
                {
                    ;
                    var javas = java.GetInstalled();
                    if (CheckForNewest32bit(javas) != null)
                    {
                        Console.WriteLine("Java {0} 32-bit Installed Successfully.", newestVersion);
                        return ExitCodes.Success;
                    }
                    Console.WriteLine("Unknown Error: install ran without but new version not installed");
                    return ExitCodes.UnknownError;

                }
                Console.WriteLine("Java installer error: {0}.", exitCode);
                return ExitCodes.ErrorInstalling;
            }
            Console.WriteLine("Error Downloading.");
            return ExitCodes.ErrorDownloading;
        }
    }
}
