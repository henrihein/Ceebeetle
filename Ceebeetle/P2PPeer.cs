﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ServiceModel;

namespace Ceebeetle
{
    public interface INetworkListener
    {
        void OnMessage(string uid, string message);
        void OnUser(string uid);
        void OnConnected();
        void OnDisconnected();
        void OnReceivingFile(string uidFrom, string filename);
        void OnFileData(string uidFrom, string recipient, string filename, byte[] data);
        void OnFileComplete(string uidFrom, string recipient, string filename, byte[] hash);
    }

    [ServiceContract(CallbackContract = typeof(ICeebeetlePeer))]
    public interface ICeebeetlePeer
    {
        [OperationContract(IsOneWay = true)]
        void ChatMessage(string uid, string hello);
        [OperationContract(IsOneWay = true)]
        void OnUserConnected(string uid);
        [OperationContract(IsOneWay = true)]
        void PingAll(string requester);
        [OperationContract(IsOneWay = true)]
        void OnNewFile(string sender, string recipient, string filename);
        [OperationContract(IsOneWay = true)]
        void RequestFile(string sender, string recipient, string filename);
        [OperationContract(IsOneWay = true)]
        void CancelFile(string sender, string recipient, string filename);
        //Todo: actually sending the data should be done on a separate, 2-way channel.
        //This way, all data is sent to all the peers in the mesh. With a low number of peers, 
        //not a big problem.
        [OperationContract(IsOneWay = true)]
        void SendFileData(string sender, string recipient, string filename, byte[] bytes);
    }

    public class CeebeetlePeerImpl : ICeebeetlePeer
    {
        public delegate void OnPingedD();
        public delegate void OnFileTransferResponseD(string recipient, string filename, bool accept);
        private HashSet<INetworkListener> m_listeners;
        private HashSet<string> m_users;
        private string m_uid;
        private OnPingedD m_pingCallback;
        private OnFileTransferResponseD m_fileTransferResponseCallback;

        public OnPingedD PingCallback
        {
            set { m_pingCallback = value; }
        }
        public OnFileTransferResponseD FileTransferResponseCallback
        {
            set { m_fileTransferResponseCallback = value; }
        }
        public string UserID
        {
            set { m_uid = value; }
        }

        public CeebeetlePeerImpl()
        {
            m_listeners = new HashSet<INetworkListener>();
            m_users = new HashSet<string>();
            m_uid = "";
            m_pingCallback = null;
        }

        public void AddKnownUser(string uid)
        {
            lock (m_users)
            {
                m_users.Add(uid);
            }
        }
        public string[] GetKnownUsers(bool inclSelf = true)
        {
            HashSet<string> users = new HashSet<string>();

            lock (m_users)
            {
                foreach (string user in m_users)
                    users.Add(user);
            }
            if (!inclSelf)
                users.Remove(m_uid);
            return users.ToArray();
        }

        public void AddListener(INetworkListener listener)
        {
            lock (m_listeners)
            {
                m_listeners.Add(listener);
            }
        }
        public void RemoveListener(INetworkListener listener)
        {
            lock (m_listeners)
            {
                m_listeners.Remove(listener);
            }
        }

        private INetworkListener[] GetListeners()
        {
            INetworkListener[] listeners = null;

            lock (m_listeners)
            {
                listeners = m_listeners.ToArray();
            }
            return listeners;
        }
        void ICeebeetlePeer.ChatMessage(string uid, string message)
        {
            try
            {
                INetworkListener[] listeners = GetListeners();

                foreach (INetworkListener listener in listeners)
                    listener.OnMessage(uid, message);
            }
            catch (System.IO.IOException ioex)
            {
                System.Diagnostics.Debug.Write(string.Format("IO Exception in ChatMessage: {0}", ioex.Message));
            }
            catch (System.ServiceModel.CommunicationException commEx)
            {
                System.Diagnostics.Debug.Write(string.Format("Comm Exception in ChatMessage: {0}", commEx.Message));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(string.Format("Exception in ChatMessage: {0}", ex.Message));
            }
        }
        //Sent in response to PingAll.
        void ICeebeetlePeer.OnUserConnected(string uid)
        {
            AddKnownUser(uid);
            if (0 != string.Compare(m_uid, uid))
                OnUserConnectedEvent(uid);
        }
        void ICeebeetlePeer.PingAll(string requester)
        {
            if (null != m_pingCallback)
                m_pingCallback();
        }
        void ICeebeetlePeer.OnNewFile(string sender, string recipient, string filename)
        {
            if (0 == string.Compare(m_uid, recipient))
            {
                try
                {
                    INetworkListener[] listeners = GetListeners();

                    foreach (INetworkListener listener in listeners)
                        listener.OnReceivingFile(sender, filename);
                }
                catch (System.IO.IOException ioex)
                {
                    System.Diagnostics.Debug.Write(string.Format("IO Exception in OnNewFile: {0}", ioex.Message));
                }
                catch (System.ServiceModel.CommunicationException commEx)
                {
                    System.Diagnostics.Debug.Write(string.Format("Comm Exception in OnNewFile: {0}", commEx.Message));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Write(string.Format("Exception in OnNewFile: {0}", ex.Message));
                }
            }
        }
        void ICeebeetlePeer.RequestFile(string sender, string recipient, string filename)
        {
            System.Diagnostics.Debug.Write("Requested file: " + filename);
            if (null != m_fileTransferResponseCallback)
            {
                if (0 == string.Compare(sender, m_uid))
                    m_fileTransferResponseCallback(recipient, filename, true);
            }
        }
        void ICeebeetlePeer.CancelFile(string sender, string recipient, string filename)
        {
            System.Diagnostics.Debug.Write("Canceling file: " + filename);
            if (null != m_fileTransferResponseCallback)
            {
                if (0 == string.Compare(sender, m_uid))
                    m_fileTransferResponseCallback(recipient, filename, false);
            }
        }
        void ICeebeetlePeer.SendFileData(string sender, string recipient, string filename, byte[] data)
        {
            if (0 == string.Compare(m_uid, recipient))
            {
                try
                {
                    INetworkListener[] listeners = GetListeners();

                    foreach (INetworkListener listener in listeners)
                        listener.OnFileData(sender, m_uid, filename, data);
                }
                catch (System.IO.IOException ioex)
                {
                    System.Diagnostics.Debug.Write(string.Format("IO Exception in SendFileData: {0}", ioex.Message));
                }
                catch (System.ServiceModel.CommunicationException commEx)
                {
                    System.Diagnostics.Debug.Write(string.Format("Comm Exception in SendFileData: {0}", commEx.Message));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Write(string.Format("Exception in SendFileData: {0}", ex.Message));
                }
            }
        }
        public void OnConnected()
        {
            try
            {
                INetworkListener[] listeners = GetListeners();

                foreach (INetworkListener listener in listeners)
                    listener.OnConnected();
            }
            catch (System.IO.IOException ioex)
            {
                System.Diagnostics.Debug.Write(string.Format("Exception in OnConnected: {0}", ioex.Message));
            }
            catch (System.ServiceModel.CommunicationException commEx)
            {
                System.Diagnostics.Debug.Write(string.Format("Exception in OnConnected: {0}", commEx.Message));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(string.Format("Exception in OnConnected: {0}", ex.Message));
            }
        }
        public void OnDisconnected()
        {
            try
            {
                INetworkListener[] listeners = GetListeners();

                foreach (INetworkListener listener in listeners)
                    listener.OnDisconnected();
            }
            catch (System.IO.IOException ioex)
            {
                System.Diagnostics.Debug.Write(string.Format("Exception in OnDisconnected: {0}", ioex.Message));
            }
            catch (System.ServiceModel.CommunicationException commEx)
            {
                System.Diagnostics.Debug.Write(string.Format("Exception in OnDisconnected: {0}", commEx.Message));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(string.Format("Exception in OnDisconnected: {0}", ex.Message));
            }
        }
        private void OnUserConnectedEvent(string uid)
        {
            try
            {
                INetworkListener[] listeners = GetListeners();

                foreach (INetworkListener listener in listeners)
                    listener.OnUser(uid);
            }
            catch (System.IO.IOException ioex)
            {
                System.Diagnostics.Debug.Write(string.Format("IO Exception in OnUser: {0}", ioex.Message));
            }
            catch (System.ServiceModel.CommunicationException commEx)
            {
                System.Diagnostics.Debug.Write(string.Format("Comm Exception in OnUser: {0}", commEx.Message));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(string.Format("Exception in OnUser: {0}", ex.Message));
            }
        }
    }

}
