using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossHapticsLauncher {
    interface ExecutableFile {
        string name { get; }
        string executablePath { get; }
        string Inlets { get; set; }
        string Outlets { get; set; }
        void Launch();
    }
    interface Components {

    }
    
}
