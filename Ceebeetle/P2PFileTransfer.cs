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
        private bool m_finalized;
        private bool m_error;
        private bool m_closed;

        public byte[] Hash
        {
            get { return m_hash.Hash; }
        }
        public bool Finalized
        {
            get { return m_finalized; }
        }
        public string LocalName
        {
            get { return m_localFile; }
        }
        public string RemoteName
        {
            get { return m_remoteFile; }
        }
        public bool Error
        {
            get { return m_error; }
        }
        public bool Closed
        {
            get { return m_closed; }
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
            m_finalized = false;
            m_bytesSent = 0;
            m_hash = null;
            m_error = false;
            m_closed = false;
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
        public void OnError()
        {
            m_error = true;
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
        public bool HasLoadWork()
        {
            //An uninitialized file has work.
            if (-1 == m_filesize)
                return true;
            return m_offsetCur < m_filesize;
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
                if (null == m_hash)
                    m_hash = MD5.Create();
                if (m_offsetCur == m_filesize)
                    m_hash.TransformFinalBlock(m_data, 0, curBlobSize);
                else
                    m_hash.TransformBlock(m_data, 0, curBlobSize, null, 0);
            }
            catch (CryptographicUnexpectedOperationException unexpected)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Crypto Exception hashing file {0}: {1}", m_localFile, unexpected.Message));
            }
            catch (IOException ioex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("IO Exception reading file {0}: {1}", m_localFile, ioex.Message));
                m_error = true;
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
        public void MarkFinalized()
        {
            m_finalized = true;
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
        public bool OnFinalized(byte[] hash)
        {
            m_finalized = true;
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
            m_closed = true;
        }
    }

    public class CCBP2PFileList : Dictionary<string, CCBP2PFile>
    {
        public enum FileListType
        {
            flt_none = 0,
            flt_local,
            flt_remote
        }
    }

    public class CCBP2PFileWorker : INetworkListener
    {
        private CCBP2PFileList m_inbox;
        private CCBP2PFileList m_outbox;
        AutoResetEvent m_signal;
        private ManualResetEvent m_closeSignal;
        private Thread m_dataPumpThread;

        public CCBP2PFileWorker(ManualResetEvent closeEvent) : base()
        {
            m_closeSignal = closeEvent;
            m_inbox = new CCBP2PFileList();
            m_outbox = new CCBP2PFileList();
            m_signal = new AutoResetEvent(false);
            m_dataPumpThread = null;
        }

        private void MaybeStart()
        {
            if (null == m_dataPumpThread)
            {
                m_dataPumpThread = new Thread(FileDataPump);
                m_dataPumpThread.Priority = ThreadPriority.BelowNormal;
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
        public void FileFinalized(string filename)
        {
            lock (m_outbox)
            {
                if (m_outbox.ContainsKey(filename))
                    m_outbox[filename].MarkFinalized();
            }

        }
        public void FileOnError(string filename)
        {
            lock (m_outbox)
            {
                if (m_outbox.ContainsKey(filename))
                    m_outbox[filename].OnError();
            }
        }
        public void Stop()
        {
            m_signal.Set();
            if (null != m_dataPumpThread)
                m_dataPumpThread.Join();
        }

        public bool HasWork()
        {
            lock (m_outbox)
            {
                if (0 < m_outbox.Keys.Count)
                    return true;
            }
            lock (m_inbox)
            {
                if (0 < m_inbox.Keys.Count)
                        return true;
            }
            return false;
        }
        public string HasData()
        {
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
                if (!nextFileToLoad.HasDataToSend())
                    return nextFileToLoad.LoadNextBlob();
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
            WaitHandle[] waitors = new WaitHandle[2]{m_signal, m_closeSignal};

            for (; ; )
            {
                if (HasWork())
                {
                    LoadNext();
                    ScanForWork();
                    //Don't starve others
                    if (m_closeSignal.WaitOne(23))
                        break;
                }
                else
                {
                    if (1 == WaitHandle.WaitAny(waitors))
                        break;
                }
            }
            //Not necessary to close file handles if we are exiting, but closing here
            //for cleanliness in case we ever need to restart the file data pump.
            CloseAll();
        }

        private void CloseAll(CCBP2PFileList files)
        {
            lock (files)
            {
                foreach (CCBP2PFile file in files.Values)
                {
                    try
                    {
                        file.Close();
                    }
                    catch (IOException ioex)
                    {
                        System.Diagnostics.Debug.WriteLine(string.Format("IO Exception closing file {0}: {1}", file.LocalName, ioex.Message));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(string.Format("Exception closing file {0}: {1}", file.LocalName, ex.Message));
                    }
                }
            }
        }
        private void CloseAll()
        {
            CloseAll(m_inbox);
            CloseAll(m_outbox);
        }
        private List<CCBP2PFile> ScanForWork(CCBP2PFileList filelist)
        {
            List<CCBP2PFile> files = new List<CCBP2PFile>();

            lock (filelist)
            {
                foreach (CCBP2PFile file in filelist.Values)
                    files.Add(file);
            }
            return files;
        }
        private void RemoveFile(CCBP2PFileList filelist, string file)
        {
            lock (filelist)
            {
                filelist.Remove(file);
            }
        }
        private void ScanForWork()
        {
            List<CCBP2PFile> inWork = ScanForWork(m_inbox);
            List<CCBP2PFile> outWork = ScanForWork(m_outbox);

            foreach (CCBP2PFile infile in inWork)
            {
                if (infile.Closed)
                    RemoveFile(m_inbox, infile.RemoteName);
                else if (infile.Error || infile.Finalized)
                    infile.Close();
            }
            foreach (CCBP2PFile outfile in outWork)
            {
                if (outfile.Closed)
                    RemoveFile(m_outbox, outfile.LocalName);
                else if (outfile.Error || outfile.Finalized)
                    outfile.Close();
            }
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
                    infile.OnFinalized(hash);
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
