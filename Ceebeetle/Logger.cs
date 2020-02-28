using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;

namespace Ceebeetle
{
    public enum LOGLEVEL
    {
        LOGMIN = 0,
        LOGERROR = 2,
        LOGDEBUG = 5,
        LOGVERBOSE = 8
    }

    class CCBLogConfig
    {
        static string m_logfilepath;
        static CCBLogger m_logger = null;
        static CCBLogger m_nulllogger = null;
        static bool m_exiting;

        static public void InitLogging(CCBConfig mainConf)
        {
            m_logfilepath = mainConf.MakeLogFilePath(DateTime.Now.Ticks);
            m_logger = new CCBLogger(m_logfilepath, mainConf.LogLevel);
            m_logger.LogTime("Ceebeetle Start of log\n---------------------------------------");
            m_nulllogger = new CCBLogger();
        }

        static public CCBLogger GetLogger()
        {
            if (m_exiting)
                return m_nulllogger;
            return m_logger;
        }
        static public void Close()
        {
            m_exiting = true;
            if (null != m_logger)
                m_logger.Close();
        }
    }

    class CCBLogger
    {
        private StreamWriter m_writer;
        private LOGLEVEL m_loglevel;

        public CCBLogger()
        {
            m_writer = null;
        }
        public CCBLogger(string logfilepath, LOGLEVEL loglevel)
        {
            m_loglevel = loglevel;
            try
            {
                m_writer = new StreamWriter(logfilepath);
                m_writer.AutoFlush = true;
            }
            catch (IOException ioex)
            {
                System.Diagnostics.Debug.WriteLine("Could not open log file " + logfilepath);
                System.Diagnostics.Debug.WriteLine("  Error: " + ioex.Message);
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error when opening log file " + logfilepath);
                System.Diagnostics.Debug.WriteLine("  Error: " + ex.Message);
            }
        }
        public void Close()
        {
            if (null == m_writer)
                return;
            try
            {
                m_writer.Close();
                m_writer = null;
            }
            catch (IOException ioex)
            {
                System.Diagnostics.Debug.WriteLine("Could not close log file.");
                System.Diagnostics.Debug.WriteLine("  Error: " + ioex.Message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error when closing log file.");
                System.Diagnostics.Debug.WriteLine("  Error: " + ex.Message);
            }
        }

        public void Log(string text)
        {
            if (null != m_writer)
            {
                try
                {
                    m_writer.WriteLine(text);
                }
                catch (IOException ioex)
                {
                    System.Diagnostics.Debug.WriteLine("Could not write to log file.");
                    System.Diagnostics.Debug.WriteLine("  Error: " + ioex.Message);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error when writing to log file.");
                    System.Diagnostics.Debug.WriteLine("  Error: " + ex.Message);
                }
            }
            System.Diagnostics.Debug.WriteLine(text);
        }
        public void Log(string text, params object[] args)
        {
            string outtext = string.Format(text, args);
            Log(outtext);
        }
        public void LogTime(string text)
        {
            Log(string.Format("{0} {1}", DateTime.Now.ToString(), text));
        }
        private string Prefix(string strLevel, string text)
        {
            string strPrefix = string.Format("CEEBEETLE-{0} {1}:", strLevel, DateTime.Now.ToString());

            return strPrefix + text;
        }
        public void Debug(string text)
        {
            if (LOGLEVEL.LOGDEBUG <= m_loglevel)
                Log(Prefix("Debug", text));
        }
        public void Debug(string text, params object[] args)
        {
            if (LOGLEVEL.LOGDEBUG <= m_loglevel)
                Log(Prefix("Debug", text), args);
        }
        public void Error(string text)
        {
            if (LOGLEVEL.LOGERROR <= m_loglevel)
                Log(Prefix("Error", text));
        }
        public void Error(string text, params object[] args)
        {
            if (LOGLEVEL.LOGERROR <= m_loglevel)
                Log(Prefix("Error", text), args);
        }
        public void Fatal(string text)
        {
            Log("\nFATAL\n");
            Log(Prefix("Fatal", text));
        }
        public void Fatal(string text, params object[] args)
        {
            Log("\nFATAL\n");
            Log(Prefix("Fatal", text), args);
        }
    }

    public abstract class CCBLogging
    {
        public void Log(string text)
        {
            CCBLogConfig.GetLogger().Log(text);
        }
        public void Log(string text, params object[] args)
        {
            CCBLogConfig.GetLogger().Log(text, args);
        }
        public void Debug(string text)
        {
            CCBLogConfig.GetLogger().Debug(text);
        }
        public void Debug(string text, params object[] args)
        {
            CCBLogConfig.GetLogger().Debug(text, args);
        }
        public void Error(string text)
        {
            CCBLogConfig.GetLogger().Error(text);
        }
        public void Error(string text, params object[] args)
        {
            CCBLogConfig.GetLogger().Error(text, args);
        }
        public void Fatal(string text)
        {
            CCBLogConfig.GetLogger().Fatal(text);
        }
        public void Fatal(string text, params object[] args)
        {
            CCBLogConfig.GetLogger().Fatal(text, args);
        }
    }
}
