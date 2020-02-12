using System;
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
        void OnConnected();
        void OnDisconnected();
        void OnReceivingFile(string uidFrom, string filename);
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
    }

    public class CeebeetlePeerImpl : ICeebeetlePeer
    {
        public delegate void OnPingedD();
        private HashSet<INetworkListener> m_listeners;
        private HashSet<string> m_users;
        private string m_uid;
        private OnPingedD m_pingCallback;

        public OnPingedD PingCallback
        {
            set { m_pingCallback = value; }
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
        public string[] GetKnownUsers()
        {
            string[] knownUsers;

            lock (m_users)
            {
                knownUsers = m_users.ToArray<string>();
            }
            return knownUsers;
        }

        public void AddListener(INetworkListener listener)
        {
            lock (m_listeners)
            {
                m_listeners.Add(listener);
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
            System.Diagnostics.Debug.Write("Requesting file: " + filename);
        }
        void ICeebeetlePeer.CancelFile(string sender, string recipient, string filename)
        {
            System.Diagnostics.Debug.Write("Canceling file: " + filename);
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
    }

}
