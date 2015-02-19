using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace javaer
{
    public class JavaData
    {
        public string Name { get; protected set; }
        public string Version { get; protected set; }
        public string FileVersion { get; protected set; }
        public string FullPath { get; protected set; }
        public string InstallFolder { get; protected set; }
        public bool x64 { get; protected set; }
        public string FullVersion { get { return string.Format("{0}{1}", Version, StringBit(x64)); } }

        public JavaData(string name, string version, string fileVersion, string fullPath, string installFolder, bool X64)
        {
            Name = name;
            Version = version;
            FileVersion = fileVersion;
            FullPath = fullPath;
            InstallFolder = installFolder;
            x64 = X64;
        }

        private string StringBit(bool x64)
        {
            if (x64) { return ", 64-bit"; }
            else { return ", 32-bit"; }
        }
    }
}
