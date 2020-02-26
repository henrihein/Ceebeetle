﻿using System;
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
        private string m_sender;
        private string m_recipient;
        private int m_offsetCur;
        private byte[] m_data;
        FileStream m_filePtr;
        private long m_filesize;
        private long m_bytesSent;
        private MD5 m_hash;
        private byte[] m_remoteHash;
        private bool m_finalized;
        private bool m_error;
        private bool m_closed;
        private DateTime m_tick;

        public byte[] Hash
        {
            get { return m_hash.Hash; }
        }
        public bool Finalized
        {
            get { return m_finalized; }
        }
        public bool NeedHashCheck
        {
            get { return (null == m_hash) && (null != m_remoteHash); }
        }
        public string LocalName
        {
            get { return m_localFile; }
        }
        public string RemoteName
        {
            get { return m_remoteFile; }
        }
        public string Sender
        {
            get { return m_recipient; }
        }
        public string Recipient
        {
            get { return m_recipient; }
        }
        public bool Error
        {
            get { return m_error; }
        }
        public bool Closed
        {
            get { return m_closed; }
        }

        public CCBP2PFile(string localFile, string remoteFile, string sender, string recipient)
        {
            m_data = new byte[BlobSize];
            m_localFile = localFile;
            m_remoteFile = remoteFile;
            m_sender = sender;
            m_recipient = recipient;
            m_offsetCur = 0;
            m_filePtr = null;
            m_filesize = -1;
            m_finalized = false;
            m_bytesSent = 0;
            m_hash = null;
            m_remoteHash = null;
            m_error = false;
            m_closed = false;
            m_tick = DateTime.Now;
        }
        public virtual bool IsDone()
        {
            if (m_finalized)
                return CheckHash();
            return false;
        }
        private long GetFileSize(string localFile)
        {
            try
            {
                FileInfo fi = new FileInfo(localFile);

                return fi.Length;
            }
            catch (IOException ioex)
            {
                System.Diagnostics.Debug.WriteLine("IO Exception getting file size: " + ioex.Message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception getting file size: " + ex.Message);
            }
            return 0;
        }
        private void InitFileSize()
        {
            m_filesize = GetFileSize(m_localFile);
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
            if (m_error)
                return false;
            return m_bytesSent < m_offsetCur;
        }
        public bool HasLoadWork()
        {
            if (m_error)
                return false;
            //An uninitialized file has work.
            if (-1 == m_filesize)
                return true;
            return m_offsetCur < m_filesize;
        }
        public bool CheckNode(string sender, string recipient)
        {
            return (0 == string.Compare(m_sender, sender)) && (0 == string.Compare(m_recipient, recipient));
        }
        public long LoadNextBlob()
        {
            int curBlobSize = 0;

            try
            {
                if (-1 == m_filesize)
                    InitFileSize();
                if (null == m_filePtr)
                    m_filePtr = new FileStream(m_localFile, FileMode.Open, FileAccess.Read, FileShare.Read);
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
                m_error = true;
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
            m_tick = DateTime.Now;
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
            m_tick = DateTime.Now;
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
            m_tick = DateTime.Now;
        }
        public bool OnFinalized(byte[] hash)
        {
            m_remoteHash = new byte[hash.Length];
            hash.CopyTo(m_remoteHash, 0);
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
        protected bool CheckHash()
        {
            if ((null != m_remoteHash) && (null != m_hash) && (m_remoteHash.Length == m_hash.Hash.Length))
            {
                for (uint ixb = 0; ixb < m_remoteHash.Length; ixb++)
                    if (m_remoteHash[ixb] != m_hash.Hash[ixb])
                        return false;
                return true;
            }
            return false;
        }
        public bool CalcLocalHash(WaitHandle closeEvent)
        {
            m_tick = DateTime.Now;
            try
            {
                FileStream filePtr = null;
                byte[] dataBlock = new byte[BlobSize];
                long filelen = GetFileSize(m_localFile);
                long offset = 0;

                m_hash = MD5.Create();
                if (!m_closed)
                    Close();
                filePtr = new FileStream(m_localFile, FileMode.Open);
                while (offset < filelen)
                {
                    int curBlockSize = (int)(((long)offset + (long)BlobSize) > filelen ? (filelen - offset) : (long)BlobSize);

                    filePtr.Read(dataBlock, 0, curBlockSize);
                    if ((offset + curBlockSize) >= filelen)
                        m_hash.TransformFinalBlock(dataBlock, 0, curBlockSize);
                    else
                        m_hash.TransformBlock(dataBlock, 0, curBlockSize, null, 0);
                    if (closeEvent.WaitOne(0))
                        break;
                }
                filePtr.Close();
            }
            catch (IOException ioex)
            {
                System.Diagnostics.Debug.Write(string.Format("IO Exception in CheckHash {0}: {1}", m_localFile, ioex.Message));
                m_error = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(string.Format("Exception in CheckHash {0}: {1}", m_localFile, ex.Message));
                m_error = true;
            }
            return false;
        }
        public bool IsObsolete()
        {
            DateTime dtComp = m_tick.AddSeconds(61 * 5);

            return dtComp < DateTime.Now;
        }
    }

    public class CCBP2POutFile : CCBP2PFile
    {
        public CCBP2POutFile(string filename, string sender, string recipient)
            : base(filename, null, sender, recipient)
        {
        }

        public override bool IsDone()
        {
            if (IsSent())
                return Finalized;
            return false;
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
        private ManualResetEvent m_signal;
        private ManualResetEvent m_closeSignal;
        private Thread m_dataPumpThread;

        public CCBP2PFileWorker(ManualResetEvent closeEvent) : base()
        {
            m_closeSignal = closeEvent;
            m_inbox = new CCBP2PFileList();
            m_outbox = new CCBP2PFileList();
            m_signal = new ManualResetEvent(false);
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
        public void FileRequested(string sender, string recipient, string filename)
        {
            lock(m_outbox)
            {
                if (!m_outbox.ContainsKey(filename))
                {
                    m_outbox[filename] = new CCBP2POutFile(filename, sender, recipient);
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
        public string HasUploadWork()
        {
            lock (m_outbox)
            {
                foreach (string filename in m_outbox.Keys)
                {
                    if (!m_outbox[filename].Finalized)
                        return filename;
                }
            }
            return null;
        }
        public string HasErrorFile(ref string sender, ref string recipient)
        {
            lock (m_outbox)
            {
                foreach (string filename in m_outbox.Keys)
                {
                    CCBP2PFile outfile = m_outbox[filename];

                    if (outfile.Error)
                    {
                        sender = outfile.Sender;
                        recipient = outfile.Recipient;
                        //Remove object first, so we don't go in a tizzy when 
                        //getting the notification back.
                        m_outbox.Remove(filename);
                        return outfile.LocalName;
                    }
                }
            }
            lock (m_inbox)
            {
                foreach(string filename in m_inbox.Keys)
                {
                    CCBP2PFile infile = m_inbox[filename];

                    if (infile.Error)
                    {
                        sender = infile.Sender;
                        recipient = infile.Recipient;
                        m_inbox.Remove(filename);
                        return infile.RemoteName;
                    }
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
        public bool IsSent(string filename, ref byte[] hash, ref string recipient)
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
                    recipient = file.Recipient;
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
        public void PrepareInFile(string sender, string recipient, string remoteFile, string localFile)
        {
            CCBP2PFile newFile = new CCBP2PFile(localFile, remoteFile, sender, recipient);

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
                    m_signal.Reset();
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
        //We want to report errors to the mesh. So the files with errors are 
        //removed in HasErrorFile, which are called by the networker thread.
        private void ScanForWork()
        {
            List<CCBP2PFile> inWork = ScanForWork(m_inbox);
            List<CCBP2PFile> outWork = ScanForWork(m_outbox);

            foreach (CCBP2PFile infile in inWork)
            {
                if (infile.NeedHashCheck)
                {
                    infile.CalcLocalHash(m_closeSignal);
                }
                if (infile.IsDone())
                    RemoveFile(m_inbox, infile.RemoteName);
                else if (infile.Error || infile.Finalized)
                    infile.Close();
                else if (infile.IsObsolete())
                    infile.OnError();
            }
            foreach (CCBP2PFile outfile in outWork)
            {
                if (outfile.IsDone())
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
        //We write the data directly on the networker thread. The reason is that if the write is fast, it's ok;
        //if it's slow, the file worker thread would back up while the chunks came in, leaving significant parts
        //of the file in memory. With only 2 threads, it's better to write the data out immediately. If the file
        //transfer system needs to scale up, this should be changed and more file worker threads should be added.
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
        //After having written the file, we mark it as finalized, which will make the file worker thread
        //calculate and check the hash. 
        void INetworkListener.OnFileComplete(string filename, byte[] hash)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("Completing file: {0}\n", filename));
            try
            {
                CCBP2PFile infile = GetInFile(filename);

                if (null == infile)
                    System.Diagnostics.Debug.WriteLine(string.Format("No file data for {0}, ignoring file completion event.", filename));
                else
                {
                    infile.OnFinalized(hash);
                    m_signal.Set();
                }
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
        void INetworkListener.OnFileError(string sender, string recipient, string filename)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("Completing file: {0}\n", filename));
            try
            {
                CCBP2PFile infile = GetInFile(filename);

                if ((null != infile) && infile.CheckNode(sender, recipient))
                {
                    infile.OnError();
                    m_signal.Set();
                }
                lock (m_outbox)
                {
                    foreach (CCBP2PFile outfile in m_outbox.Values)
                    {
                        if ((0 == string.Compare(outfile.LocalName, filename)) && outfile.CheckNode(sender, recipient))
                        {
                            outfile.OnError();
                            m_signal.Set();
                        }
                    }
                }
            }
            catch (IOException ioex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("IO Exception OnFileError {0}: {1}", filename, ioex.Message));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Exception OnFileError {0}: {1}", filename, ex.Message));
            }
        }
        #endregion
    }

}
