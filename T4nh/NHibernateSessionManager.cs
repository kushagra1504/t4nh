using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Web;
using System.Xml;

using NHibernate;
using NHibernate.Cfg;
using NHibernate.Event;
using System.Web.Caching;

namespace Genius.Northwind.BusinessServices
{
	/// <summary>
	/// A Singleton that creates and persits a single SessionFactory for the to program to access globally.
	/// This uses the .Net CallContext to store a session for each thread.
	/// 
	/// This is heavely based on 'NHibernate Best Practices with ASP.NET' By Billy McCafferty.
	/// http://www.codeproject.com/KB/architecture/NHibernateBestPractices.aspx
	/// </summary>
	public sealed class NHibernateSessionManager
	{
		#region Static Content

		private static NHibernateSessionManager _nHibernateSessionManager = null;

		/// <summary>
		/// Set method is exposed so that the INHibernateSessionManager can be swapped out for Unit Testing.
		/// NOTE: Cannot set Instance after it has been initialized, and calling Get will automatically intialize the Instance.
		/// </summary>
		public static NHibernateSessionManager Instance
		{
			get
			{
				if (_nHibernateSessionManager == null)
					_nHibernateSessionManager = new NHibernateSessionManager();
				return _nHibernateSessionManager;
			}
			set
			{
				if (_nHibernateSessionManager != null)
					throw new Exception("Cannot set Instance after it has been initialized.");
				_nHibernateSessionManager = value;
			}
		}
		#endregion

		#region Declarations

		private const string _sessionContextKey = "NHibernateSession-ContextKey";

		#endregion

		#region Constructors & Finalizers

		/// <summary>
		/// This will load the NHibernate settings from the App.config.
		/// Note: This can/should be expanded to support multiple databases.
		/// </summary>
		private NHibernateSessionManager()
		{
		}

		private static ISessionFactory GetSessionFactoryFor(string sessionFactoryConfigPath)
		{
			if (string.IsNullOrEmpty(sessionFactoryConfigPath))
				throw new ArgumentNullException("sessionFactoryConfigPath may not be null nor empty");

			//  Attempt to retrieve a cached SessionFactory from the HttpRuntime's cache.

			ISessionFactory sessionFactory = (ISessionFactory)HttpRuntime.Cache.Get(sessionFactoryConfigPath);

			//  Failed to find a cached SessionFactory so make a new one.

			if (sessionFactory == null)
			{
				if (!File.Exists(sessionFactoryConfigPath))
					// It would be more appropriate to throw a more specific exception than ApplicationException

					throw new ApplicationException(
						"The config file at '" + sessionFactoryConfigPath + "' could not be found");

				Configuration cfg = new Configuration();
				cfg.Configure(sessionFactoryConfigPath);

				//IFlushEntityEventListener[] stack = new IFlushEntityEventListener[] { new ModifiedDirtyListener() };
				//cfg.EventListeners.FlushEntityEventListeners = stack;

				//cfg.SetInterceptor(new NwAuditInterceptor());
				
				//  Now that we have our Configuration object, create a new SessionFactory
                sessionFactory = cfg.BuildSessionFactory();


				if (sessionFactory == null)
				{
					throw new InvalidOperationException("cfg.BuildSessionFactory() returned null.");
				}

				HttpRuntime.Cache.Add(sessionFactoryConfigPath, sessionFactory, null, DateTime.Now.AddDays(7),
					TimeSpan.Zero, CacheItemPriority.High, null);
			}

			return sessionFactory;
		}
		public ISession GetSessionFrom(string sessionFactoryConfigPath, INwContext userContext)
		{
			var sess = GetSessionFromInternal(sessionFactoryConfigPath, null /*new NwAuditInterceptor(userContext)*/);
            //sess.GetSessionImplementation().Listeners.PreUpdateEventListeners = new IPreUpdateEventListener[] { new UpdateModifiedPreUpdateListener(userContext) };
            return sess;
		}
		private static ISession GetSessionFromInternal(string sessionFactoryConfigPath, IInterceptor interceptor)
		{
			ISession session = (ISession)contextSessions[sessionFactoryConfigPath];

			if (session == null)
			{
				if (interceptor != null)
				{
					session = GetSessionFactoryFor(sessionFactoryConfigPath).OpenSession(interceptor);
				}
				else
				{
					session = GetSessionFactoryFor(sessionFactoryConfigPath).OpenSession();
				}

				contextSessions[sessionFactoryConfigPath] = session;
			}

			if (session == null)
				// It would be more appropriate to throw a more specific exception than ApplicationException

				throw new ApplicationException("session was null");

			return session;
		}

		~NHibernateSessionManager()
		{
			Dispose(true);
		}

		#endregion

		#region IDisposable

		private bool _isDisposed = false;

		public void Dispose()
		{
			Dispose(false);
		}
		private void Dispose(bool finalizing)
		{
			/*
            if (!_isDisposed)
            {
                // Close SessionFactory
                _sessionFactory.Close();
                _sessionFactory.Dispose();

                // Flag as disposed.
                _isDisposed = true;
                if (!finalizing)
                    GC.SuppressFinalize(this);
            }
			*/
		}

		#endregion

		#region Methods

		/*
        public INHibernateSession CreateSession()
        {
            return new NHibernateSession(CreateISession());
        }
        public ISession CreateISession()
        {
            ISession iSession;
            lock (_sessionFactory)
            {
                iSession = _sessionFactory.OpenSession();
            }
            return iSession;
        }
		*/
		#endregion

		#region Properties
		/// <summary>

		/// Since multiple databases may be in use, there may be one session per database 

		/// persisted at any one time.  The easiest way to store them is via a hashtable

		/// with the key being tied to session factory.

		/// </summary>

		private static Hashtable contextSessions
		{
			get
			{
				/*if (IsWebContext)
				{
					var sessid = HttpContext.Current.Session.SessionID + "_NH";
					if (HttpContext.Current.Cache[sessid] == null)
					{
						HttpContext.Current.Cache.Add(sessid, new Hashtable(),
							null, DateTime.MaxValue, TimeSpan.FromMinutes(HttpContext.Current.Session.Timeout),
							CacheItemPriority.Low, null);
					}
					return (Hashtable)HttpContext.Current.Cache[sessid];
				}
				else*/
				{
					if (CallContext.GetData("CONTEXT_SESSIONS") == null)
						CallContext.SetData("CONTEXT_SESSIONS", new Hashtable());
					return (Hashtable)CallContext.GetData("CONTEXT_SESSIONS");
				}
			}
		}

		private static bool IsWebContext
		{
			get { return (HttpContext.Current != null); }
		}

		#endregion
	}
}
