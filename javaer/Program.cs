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
        private static List<JavaData> javas;
        private static string newestVersion;
        private static List<string> oArgs;

        static void Main(string[] args)
        {
            oArgs = args.ToList<string>();

            ExitCodes exitCode;
            var javaTools = new JavaToolSet();
            
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
            
            GetInstalled(javaTools);
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
                    Console.WriteLine("Removing old versions");
                    UninstallList(javaTools, GetOldVersions()); 
                }
                
                
                GetInstalled(javaTools);            
                var results = CheckForNewest32bit();

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

        private static void GetInstalled(JavaToolSet javaTools)
        {
            Console.WriteLine("Checking for installed Java.");
            javas = new List<JavaData>();
            javas = javaTools.GetInstalled();
        }

        private static void UninstallList(JavaToolSet javaTools, List<JavaData> list)
        {
            if (list.Count > 0)
            {
                foreach (var java in list)
                {
                    Console.Write("Uninstalling {0}.", java.FullVersion);
                    var result = javaTools.Uninstall(java);
                    Console.WriteLine(" Uninstall Exit Code: {0}", result);
                }
            }
        }

        private static void Exit(ExitCodes exitCode)
        {
            if (!ArgsCheck("/s"))
            {
                Console.WriteLine("Program exited with code: {0}: {1}",exitCode, exitCode.ToString());
                Console.ReadLine();
            }
            Environment.Exit((int)exitCode);
        }

        private static bool ArgsCheck(string sSwitch)
        {
            return oArgs.Any(s => s.Equals(sSwitch, StringComparison.OrdinalIgnoreCase));
        }

        private static JavaData CheckForNewest32bit()
        {
            var results = javas.Find(prod => prod.Version == newestVersion && prod.x64 == false);
            return results;
        }

        private static List<JavaData> GetOldVersions()
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
                    javas.Clear();
                    javas = java.GetInstalled();
                    if (CheckForNewest32bit() != null)
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
