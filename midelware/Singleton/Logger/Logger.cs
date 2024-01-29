using NLog;

namespace midelware.Singleton.Logger
{
    public class AppLogger : ILogger
    {
        private static AppLogger instance;
        private static NLog.Logger logger;


        private AppLogger() {        
        
        
        }


        public static AppLogger GetInstance()
        {
            if (instance == null)
            {
                instance = new AppLogger();
            }

            return instance;
        }

        public static NLog.Logger GetLogger( string theLogger)
        {

            if (logger == null)
            {
                logger = NLog.LogManager.GetLogger(theLogger);
            };
            
            return logger;
        }

        public void Debug(string message, string arg = null)
        {
            if (arg is null )
                GetLogger("AppLogger").Debug(message);
            else
                GetLogger("AppLogger").Debug(message, arg);
        }

        public void Error(string message, string arg = null)
        {
            if (arg is null)
                GetLogger("AppLogger").Error(message);
            else
                GetLogger("AppLogger").Error(message, arg);
        }

        public void Info(string message, string arg = null)
        {
            if (arg is null)
                GetLogger("AppLogger").Info(message);
            else
                GetLogger("AppLogger").Info(message, arg);
        }

        public void Warning(string message, string arg = null)
        {
            if (arg is null)
                GetLogger("AppLogger").Warn(message);
            else
                GetLogger("AppLogger").Warn(message, arg);
        }
    }
}
