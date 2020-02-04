using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Ceebeetle
{
    public class CCBConfig
    {
        private static readonly uint m_version = 11;
        private static readonly uint m_minVersion = 10;
        private static readonly uint m_backupCount = 8;
        private static readonly string m_filenameTemplate = @"ceebeetle{0:D2}.{1}";
        private static readonly string m_storenameTemplate = @"ceebeetleStore{0:D2}.{1}";

        private string m_filename;
        private string m_tmpFilename;
        private string m_docLocation = null;
        private string m_docFullPath;
        private string m_tmpFullPath;
        public string DocPath { get { return m_docFullPath; } }
        public string TmpPath { get { return m_tmpFullPath; } }

        public CCBConfig()
        {
            m_filename = MakeFileName(m_version);
            m_tmpFilename = MakeTempFileName(m_version);
            m_docLocation = null;
            m_docFullPath = null;
        }
        private string MakeFileName(uint version)
        {
            return String.Format(m_filenameTemplate, version, "xml");
        }
        private string MakeTempFileName(uint version)
        {
            return String.Format(m_filenameTemplate, version, "tmp");
        }
        private void InitializeDocLocation()
        {
            if (null == m_docLocation)
            {
                string appDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);

                if (!System.IO.Directory.Exists(appDataPath))
                    System.IO.Directory.CreateDirectory(appDataPath);
                m_docLocation = System.IO.Path.Combine(appDataPath, "ceebeetle");
                if (!System.IO.Directory.Exists(m_docLocation))
                    System.IO.Directory.CreateDirectory(m_docLocation);
            }
        }
        private string MakeDocPath(string filename)
        {
            InitializeDocLocation();
            return System.IO.Path.Combine(m_docLocation, filename);
        }
        public void Initialize()
        {
            m_tmpFullPath = MakeDocPath(m_tmpFilename);
            m_docFullPath = MakeDocPath(m_filename);
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
        public string GetStoreTmpFilePath()
        {
            return MakeDocPath(String.Format(m_storenameTemplate, m_version, "tmp"));
        }
        public string GetStoreFilePath()
        {
            return MakeDocPath(String.Format(m_storenameTemplate, m_version, "xml"));
        }

        private bool MaybeBackup(string dirPath, string fileName, uint ixBak)
        {
            string fileNameSrc = string.Format("{0}-{1}.bak", fileName, ixBak);
            string fileNameDst = string.Format("{0}-{1}.bak", fileName, ixBak + 1);
            string fullPathSrc = Path.Combine(dirPath, fileNameSrc);
            string fullPathDst = Path.Combine(dirPath, fileNameDst);

            if (File.Exists(fullPathSrc))
            {
                if (File.Exists(fullPathDst) && (ixBak < m_backupCount))
                {
                    DateTime dtSrc = File.GetLastWriteTime(fullPathSrc);
                    DateTime dtDst = File.GetLastWriteTime(fullPathDst);

                    //Don't backup files less than 4 hours apart.
                    dtSrc.Subtract(new TimeSpan(4, 0, 0));
                    if (dtSrc > dtDst)
                        MaybeBackup(dirPath, fileName, ixBak + 1);
                }
                File.Copy(fullPathSrc, fullPathDst);
                return true;
            }
            return false;
        }
        public bool MaybeBackup(string path)
        {
            if (File.Exists(path))
            {
                string dirName = Path.GetDirectoryName(path);
                string fileName = Path.GetFileNameWithoutExtension(path);

                return MaybeBackup(dirName, fileName, 0);
            }
            return false;
        }
    }
}
