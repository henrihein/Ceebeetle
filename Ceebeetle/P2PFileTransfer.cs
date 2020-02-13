using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;

namespace Ceebeetle
{
    public struct SCBP2PFileTransferEnvelope
    {
        public string m_uidSender;
        public string m_uidRecipient;
        public string m_filename;
        public int m_filesize;
        public readonly Guid m_fid;
        
        public SCBP2PFileTransferEnvelope(string sender, string recipient, string filename)
        {
            m_fid = Guid.NewGuid();
            m_uidSender = sender;
            m_uidRecipient = recipient;
            m_filename = filename;
            m_filesize = 0;
        }
        public SCBP2PFileTransferEnvelope(Guid guid, string sender, string recipient, string filename)
        {
            m_fid = guid;
            m_uidSender = sender;
            m_uidRecipient = recipient;
            m_filename = filename;
            m_filesize = 0;
        }
    }

    public struct SCBP2PFileBlob
    {
        static public uint BlobSize = 0x8000;

        public long m_start;
        public byte[] m_bytes;
        public long m_bytesSent, m_size;

        public SCBP2PFileBlob(long start)
        {
            m_start = start;
            m_bytes = null;
            m_bytesSent = 0;
            m_size = 0;
        }
        public void SetDataSize(long size)
        {
            m_size = size;
            m_bytes = new byte[size];
        }
        public bool HasWork()
        {
            if (null == m_bytes)
                return false;
            return m_bytesSent < m_size;
        }
        public bool IsLoaded()
        {
            if (null == m_bytes)
                return false;
            return 0 < m_bytes.Length;
        }
        public long Size()
        {
            return m_bytes.Length;
        }
        public void MarkSent(long sent)
        {
            m_bytesSent = sent;
            if (m_bytesSent == m_size)
                m_bytes = null;
        }
    }

    public class CCBP2PFile
    {
        public string LocalFile;
        public string RemoteFile;
        private long m_offsetCur;
        private List<SCBP2PFileBlob> Data;
        FileStream m_filePtr;
        public long m_filesize;

        public CCBP2PFile(string localFile, string remoteFile)
        {
            Data = null;
            LocalFile = localFile;
            RemoteFile = remoteFile;
            m_offsetCur = 0;
            m_filePtr = null;
            m_filesize = -1;
        }
        private void InitFileSize()
        {
            try
            {
                FileInfo fi = new FileInfo(LocalFile);

                m_filesize = fi.Length;
            }
            catch (IOException ioex)
            {
                System.Diagnostics.Debug.Write("IO Exception getting file size: " + ioex.Message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write("Exception getting file size: " + ex.Message);
            }
        }
        private void InitBlobs()
        {
            long totalBlobSize = 0;

            Data = new List<SCBP2PFileBlob>();
            while (totalBlobSize < m_filesize)
            {
                SCBP2PFileBlob nextBlob = new SCBP2PFileBlob(totalBlobSize);

                if ((SCBP2PFileBlob.BlobSize + totalBlobSize) < m_filesize)
                    nextBlob.SetDataSize(SCBP2PFileBlob.BlobSize);
                else
                    nextBlob.SetDataSize(m_filesize - totalBlobSize);
                Data.Add(nextBlob);
                totalBlobSize += nextBlob.Size();
            }
        }
        public bool HasWork()
        {
            foreach (SCBP2PFileBlob blob in Data)
                if (blob.HasWork())
                    return true;
            return false;
        }
        public bool IsLoaded()
        {
            if (null == Data)
                return false;
            foreach (SCBP2PFileBlob blob in Data)
                if (!blob.IsLoaded())
                    return false;
            return true;
        }
        public long LoadNext()
        {
            long totalCbRed = 0;

            if (-1 == m_filesize)
                InitFileSize();
            if ((null == Data) && (0 < m_filesize))
                InitBlobs();
            foreach (SCBP2PFileBlob blob in Data)
            {
                try
                {
                    if (!blob.IsLoaded())
                    {
                        if (null == m_filePtr)
                            m_filePtr = new FileStream(LocalFile, FileMode.Open);
                        m_filePtr.Read(blob.m_bytes, (int)m_offsetCur, (int)blob.m_bytes.Length);
                        m_offsetCur += blob.m_bytes.Length;
                        totalCbRed += blob.m_bytes.Length;
                        break;
                    }
                }
                catch (IOException ioex)
                {
                    System.Diagnostics.Debug.Write(string.Format("IO Exception reading file {0}: {1}", LocalFile, ioex.Message));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Write(string.Format("Exception reading file {0}: {1}", LocalFile, ex.Message));
                }
            }
            return totalCbRed;
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
                    System.Diagnostics.Debug.Write(string.Format("IO Exception closing file {0}: {1}", LocalFile, ioex.Message));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Write(string.Format("Exception closing file {0}: {1}", LocalFile, ex.Message));
                }
            }
        }
    }

    public class CCBP2PFileWorker
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
                    m_outbox[filename] = new CCBP2PFile(filename, null);
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
        private long LoadNext()
        {
            CCBP2PFile nextFileToLoad = GetNextFileToLoad();

            if (null != nextFileToLoad)
                return nextFileToLoad.LoadNext();
            return 0;
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
    }

}
