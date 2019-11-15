using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Ceebeetle
{
    class CCBConfig
    {
        public string DocPath { get; set; }

        public void Initialize()
        {
            DocPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                                            @"ceebeetle.xml");
                        
        }

    }
}
