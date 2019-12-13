using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Ceebeetle
{
    class CCBConfig
    {
        private static readonly uint m_version = 9;
        private static readonly uint m_minVersion = 6;

        private string m_filename;
        public string DocPath { get; set; }

        public CCBConfig()
        {
            m_filename = MakeFileName(m_version);
        }
        private string MakeFileName(uint version)
        {
            return String.Format(@"ceebeetle{0:D2}.xml", version);
        }
        private string MakeDocPath(string filename)
        {
            return System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), filename);
        }
        public void Initialize()
        {
            DocPath = MakeDocPath(m_filename);
        }
        public string GetLoadFile()
        {
            //Check if there are previous versions we can load.
            string fileToCheck = DocPath;
            uint prevVer = m_version;

            while (prevVer >= m_minVersion)
            {
                if (File.Exists(fileToCheck))
                    return fileToCheck;
                prevVer--;
                fileToCheck = MakeDocPath(MakeFileName(prevVer));
            }
            //Lastly check undecorated
            fileToCheck = MakeDocPath(@"ceebeetle.xml");
            if (File.Exists(fileToCheck))
                return fileToCheck;
            //No file found, return default path.
            return DocPath;
        }
    }
}
