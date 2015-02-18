using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace javaer
{
    public class RemoteProgramData
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string FullPath { get; set; }
        public string InstallFolder { get; set; }
        public string Bit { get; set; }
        public string FullVersion { get { return string.Format("{0} {1}", Version, Bit); } }
    }
}
