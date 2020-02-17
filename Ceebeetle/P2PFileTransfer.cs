using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Ceebeetle
{
    public class CCBP2PFileDataEnvelope
    {
        public readonly string m_recipient;
        public readonly string m_localFileName;
        public readonly string m_remoteFileName;
        public readonly long m_start;
        public readonly long m_datasize;
        public readonly long m_filesize;
        public byte[] m_bytes;

        public int Size
        {
            get { return m_bytes.Length; }
        }

        public CCBP2PFileDataEnvelope(long start, long datasize, long filesize)
        {
            m_start = start;
            m_datasize = datasize;
            m_filesize = filesize;
            m_bytes = new byte[datasize];
        }
        public CCBP2PFileDataEnvelope(long start, long datasize, long filesize,
                                        string recipient,
                                        string localFileName, string remoteFileName)
        {
            m_start = start;
            m_datasize = datasize;
            m_filesize = filesize;
            m_recipient = recipient;
            m_localFileName = localFileName;
            m_remoteFileName = remoteFileName;
            m_bytes = null;
        }
        public bool Put(byte[] data, int dataSize)
        {
            m_bytes = new byte[dataSize];
            if (null != m_bytes)
            {
                Array.Copy(data, m_bytes, dataSize);
                return true;
            }
            return false;
        }
    }

    public class CCBP2PFile
    {
        static public readonly int BlobSize = 0x8000;

        private string m_localFile;
        private string m_remoteFile;
        private string m_recipient;
        private int m_offsetCur;
        private byte[] m_data;
        FileStream m_filePtr;
        private long m_filesize;
        private long m_bytesSent;
        private MD5 m_hash;
        private bool m_complete;

        public byte[] Hash
        {
            get { return m_hash.Hash; }
        }
        public bool Complete
        {
            get { return m_complete; }
        }
        public string LocalName
        {
            get { return m_localFile; }
        }

        public CCBP2PFile(string localFile, string remoteFile, string recipient)
        {
            m_data = new byte[BlobSize];
            m_localFile = localFile;
            m_remoteFile = remoteFile;
            m_recipient = recipient;
            m_offsetCur = 0;
            m_filePtr = null;
            m_filesize = -1;
            m_bytesSent = 0;
            m_hash = MD5.Create();
            m_complete = false;
        }
        private void InitFileSize()
        {
            try
            {
                FileInfo fi = new FileInfo(m_localFile);

                m_filesize = fi.Length;
            }
            catch (IOException ioex)
            {
                System.Diagnostics.Debug.WriteLine("IO Exception getting file size: " + ioex.Message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception getting file size: " + ex.Message);
            }
        }
        public bool HasWork()
        {
            //An uninitialized file has work.
            if (-1 == m_filesize)
                return true;
            return m_offsetCur < m_filesize;
        }
        public bool GetWork(ref byte[] data)
        {
            return false;
        }
        public bool IsLoaded()
        {
            return m_offsetCur == m_filesize;
        }
        public bool IsSent()
        {
            return m_bytesSent == m_filesize;
        }
        public bool HasDataToSend()
        {
            return m_bytesSent < m_offsetCur;
        }
        public long LoadNextBlob()
        {
            int curBlobSize = 0;

            try
            {
                if (-1 == m_filesize)
                    InitFileSize();
                if (null == m_filePtr)
                    m_filePtr = new FileStream(m_localFile, FileMode.Open);
                if ((m_offsetCur + BlobSize) < m_filesize)
                    curBlobSize = BlobSize;
                else
                    curBlobSize = (int)(m_filesize - m_offsetCur);
                m_offsetCur += m_filePtr.Read(m_data, 0, curBlobSize);
                if (m_offsetCur == m_filesize)
                    m_hash.TransformFinalBlock(m_data, 0, curBlobSize);
                else
                    m_hash.TransformBlock(m_data, 0, curBlobSize, null, 0);
            }
            catch (IOException ioex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("IO Exception reading file {0}: {1}", m_localFile, ioex.Message));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Exception reading file {0}: {1}", m_localFile, ex.Message));
            }
            return curBlobSize;
        }
        public int RetrieveDataToSend(ref CCBP2PFileDataEnvelope data)
        {
            try
            {
                if (m_bytesSent < m_offsetCur)
                {
                    int dataSize = (int)(m_offsetCur - m_bytesSent);

                    data = new CCBP2PFileDataEnvelope(m_bytesSent, dataSize, m_filesize, 
                                                    m_recipient, m_localFile, m_remoteFile);
                    data.Put(m_data, dataSize);
                    return dataSize;
                }
            }
            catch (Exception unex)
            {
                System.Diagnostics.Debug.WriteLine("Unexpected exception in RetrieveDataToSend: " + unex.Message);
            }
            return 0;
        }
        public void MarkDataSent(CCBP2PFileDataEnvelope data)
        {
            m_bytesSent += data.Size;
        }
        public void WriteData(long offset, byte[] bytes)
        {
            try
            {
                if (null == m_filePtr)
                    m_filePtr = new FileStream(m_localFile, FileMode.Create);
                m_filePtr.Seek(offset, SeekOrigin.Begin);
                m_filePtr.Write(bytes, 0, bytes.Length);
            }
            catch (IOException ioex)
            {
                System.Diagnostics.Debug.Write(string.Format("IO Exception writing file {0}: {1}", m_localFile, ioex.Message));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(string.Format("Exception writing file {0}: {1}", m_localFile, ex.Message));
            }
        }
        public bool OnComplete(byte[] hash)
        {
            Close();
            return true;
        }
        public void Close()
        {
            if (null != m_filePtr)
            {
                try
                {
                    m_filePtr.Close();
                }
                catch (IOException ioex)
                {
                    System.Diagnostics.Debug.Write(string.Format("IO Exception closing file {0}: {1}", m_localFile, ioex.Message));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Write(string.Format("Exception closing file {0}: {1}", m_localFile, ex.Message));
                }
            }
            if (null != m_hash)
                m_hash.Clear();
            m_complete = true;
        }
    }

    public class CCBP2PFileWorker : INetworkListener
    {
        private Dictionary<string, CCBP2PFile> m_inbox;
        private Dictionary<string, CCBP2PFile> m_outbox;
        AutoResetEvent m_signal;
        private bool m_working;
        private Thread m_dataPumpThread;

        public CCBP2PFileWorker() : base()
        {
            m_inbox = new Dictionary<string, CCBP2PFile>();
            m_outbox = new Dictionary<string, CCBP2PFile>();
            m_signal = new AutoResetEvent(false);
            m_working = true;
            m_dataPumpThread = null;
        }

        private void MaybeStart()
        {
            if (null == m_dataPumpThread)
            {
                m_dataPumpThread = new Thread(FileDataPump);
                m_dataPumpThread.Start();
            }
        }
        public void FileRequested(string recipient, string filename)
        {
            lock(m_outbox)
            {
                if (!m_outbox.ContainsKey(filename))
                {
                    m_outbox[filename] = new CCBP2PFile(filename, null, recipient);
                    m_signal.Set();
                }
            }
            MaybeStart();
        }
        public void FileCanceled(string recipient, string filename)
        {
            lock (m_outbox)
            {
                if (m_outbox.ContainsKey(filename))
                {
                    m_outbox[filename].Close();
                    m_outbox.Remove(filename);
                }
            }
        }
        public void Stop()
        {
            m_working = false;
            m_signal.Set();
            if (null != m_dataPumpThread)
                m_dataPumpThread.Join();
        }

        public bool HasWork()
        {
            if (!m_working)
                return false;
            lock (m_outbox)
            {
                foreach (string filename in m_outbox.Keys)
                {
                    if (m_outbox[filename].HasWork())
                        return true;
                }
            }
            return false;
        }
        public string HasData()
        {
            if (!m_working)
                return null;
            lock (m_outbox)
            {
                foreach (string filename in m_outbox.Keys)
                {
                    if (m_outbox[filename].HasDataToSend())
                        return filename;
                }
            }
            return null;
        }
        public int RetrieveDataToSend(string filename, ref CCBP2PFileDataEnvelope data)
        {
            lock (m_outbox)
            {
                CCBP2PFile fileToSend = m_outbox[filename];

                if (null != fileToSend)
                    return fileToSend.RetrieveDataToSend(ref data);
            }
            return 0;
        }
        public void MarkDataSent(string filename, CCBP2PFileDataEnvelope data)
        {
            lock (m_outbox)
            {
                CCBP2PFile fileToSend = m_outbox[filename];

                if (null != fileToSend)
                    fileToSend.MarkDataSent(data);
            }
        }
        public bool IsSent(string filename, ref byte[] hash)
        {
            CCBP2PFile file = null;

            lock (m_outbox)
            {
                if (m_outbox.ContainsKey(filename))
                    file = m_outbox[filename];
            }
            if (null != file)
            {
                if (file.IsSent())
                {
                    hash = new byte[file.Hash.Length];
                    file.Hash.CopyTo(hash, 0);
                    return true;
                }
            }
            return false;
        }
        private CCBP2PFile GetNextFileToLoad()
        {
            lock (m_outbox)
            {
                foreach (CCBP2PFile file in m_outbox.Values)
                {
                    if (!file.IsLoaded())
                        return file;
                }
            }
            return null;
        }
        private CCBP2PFile GetInFile(string filename)
        {
            lock (m_inbox)
            {
                if (m_inbox.ContainsKey(filename))
                    return m_inbox[filename];
            }
            return null;
        }
        private long LoadNext()
        {
            CCBP2PFile nextFileToLoad = GetNextFileToLoad();

            if (null != nextFileToLoad)
            {
                //Do not load while file has data yet to be sent.
                if (nextFileToLoad.HasDataToSend())
                    m_signal.Set();
                else
                {
                    long cbLoaded = nextFileToLoad.LoadNextBlob();

                    //If 0 bytes were loaded, the file is done, or has an error -- either case, close it out.
                    if (0 == cbLoaded)
                        nextFileToLoad.Close();
                    else
                        return cbLoaded;
                }
            }
            return 0;
        }
        public void PrepareInFile(string recipient, string remoteFile, string localFile)
        {
            CCBP2PFile newFile = new CCBP2PFile(localFile, remoteFile, recipient);

            lock (m_inbox)
            {
                m_inbox[remoteFile] = newFile;
            }
        }
        public void FileDataPump()
        {
            while (m_working)
            {
                m_signal.WaitOne();
                if (m_working)
                {
                    LoadNext();
                }
            }
        }

        public bool ScanForWork()
        {
            bool hasWork = false;
            List<CCBP2PFile> files = new List<CCBP2PFile>();

            lock (m_outbox)
            {
                foreach (CCBP2PFile file in m_outbox.Values)
                    files.Add(file);
            }
            foreach (CCBP2PFile scanFile in files)
            {
                if (scanFile.HasWork())
                    hasWork = true;
                else if (scanFile.Complete)
                {
                    lock (m_outbox)
                    {
                        m_outbox.Remove(scanFile.LocalName);
                    }
                }
            }
            if (hasWork)
                m_signal.Set();
            return hasWork;
        }

        #region INetworkListener
        void INetworkListener.OnMessage(string uid, string message)
        {
        }
        void INetworkListener.OnConnected()
        {
        }
        void INetworkListener.OnDisconnected()
        {
        }
        void INetworkListener.OnUser(string uid)
        {
        }
        void INetworkListener.OnReceivingFile(string uidFrom, string filename)
        {
        }
        void INetworkListener.OnFileData(string filename, long offset, byte[] data)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("Receiving {0} bytes of {1}\n", data.Length, filename));
            try
            {
                CCBP2PFile infile = GetInFile(filename);

                if (null == infile)
                    System.Diagnostics.Debug.WriteLine(string.Format("No file data for {0}, ignoring file data.", filename));
                else
                    infile.WriteData(offset, data);
            }
            catch (IOException ioex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("IO Exception OnFileData {0}: {1}", filename, ioex.Message));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Exception OnFileData {0}: {1}", filename, ex.Message));
            }
        }
        void INetworkListener.OnFileComplete(string filename, byte[] hash)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("Completing file: {0}\n", filename));
            try
            {
                CCBP2PFile infile = GetInFile(filename);

                if (null == infile)
                    System.Diagnostics.Debug.WriteLine(string.Format("No file data for {0}, ignoring file completion event.", filename));
                else
                    infile.OnComplete(hash);
            }
            catch (IOException ioex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("IO Exception OnFileData {0}: {1}", filename, ioex.Message));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Exception OnFileData {0}: {1}", filename, ex.Message));
            }
        }
        #endregion
    }

}
