using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        Guid m_fid;
        uint m_seq;
        Byte[] m_bytes;

        SCBP2PFileBlob(Guid id, uint seq)
        {
            m_fid = id;
            m_seq = seq;
            m_bytes = null;
        }
        public SCBP2PFileBlob(Guid id, uint seq, Byte[] bytes)
        {
            m_fid = id;
            m_seq = seq;
            if (null == bytes)
                m_bytes = null;
            else
                m_bytes = (Byte[])bytes.Clone();
        }
    }

    public class CCBP2PFile
    {
        private SCBP2PFileTransferEnvelope m_envelope;
        private List<SCBP2PFileBlob> m_content;
        private uint m_seq;

        public CCBP2PFile(SCBP2PFileTransferEnvelope envelope)
        {
            m_envelope = envelope;
            m_seq = 0;
            m_content = new List<SCBP2PFileBlob>();
        }

        public void AddData(Byte[] bytes)
        {
            m_content.Add(new SCBP2PFileBlob(m_envelope.m_fid, m_seq++, bytes));
        }
        public void AddData(SCBP2PFileBlob data)
        {
            m_content.Add(data);
        }
    }

}
