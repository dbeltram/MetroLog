﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetroLog.Targets;

namespace MetroLog.Internal
{
    public abstract class LogManagerFactoryBase
    {
        private static ILogManager _defaultLogManager;

        private LoggingConfiguration DefaultSettings
        {
            get { return CreateDefaultSettings(); }
        }

        protected ILogManager CreateNew(ILoggingEnvironment environment, LoggingConfiguration configuration = null)
        {
            return new LogManager(environment, configuration ?? DefaultSettings);
        }

        protected virtual LoggingConfiguration CreateDefaultSettings()
        {
            // default logging config...
            var configuration = new LoggingConfiguration();
            configuration.AddTarget(LogLevel.Trace, LogLevel.Fatal, new DebugTarget());
            configuration.AddTarget(LogLevel.Trace, LogLevel.Fatal, new EtwTarget());

            return configuration;
        }

        public static ILogManager DefaultLogManager
        {
            get
            {
                if (_defaultLogManager == null)
                    throw new InvalidOperationException("LogManagerFactory.Initialize() must be called first.");

                return _defaultLogManager;
            }
            private set { _defaultLogManager = value; }
        }

        protected static void SetDefaultLogManager(ILogManager instance, bool allowReinit = false)
        {
            if (instance == null)
                throw new ArgumentNullException("instance");

            if (_defaultLogManager != null && !(allowReinit))
                throw new InvalidOperationException("Already Initalized. Cannot call LogManagerFactory.Initialize() multiple times.");

            DefaultLogManager = instance;
        }
    }
}