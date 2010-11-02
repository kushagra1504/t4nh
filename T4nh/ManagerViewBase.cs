using System.Collections.Generic;
using NHibernate;
using NHibernate.Criterion;

namespace Genius.Northwind.BusinessServices
{
	public abstract partial class ManagerViewBase<T>
	{
		protected ISession _session;
		protected const int DEFAULT_MAX_RESULTS = -1;

		protected INwContext UserContext { get; private set; }

		public ManagerViewBase(ISession session, INwContext userContext)
		{
			this._session = session;
			this.UserContext = userContext;
		}

		#region Get Methods
		public IList<T> GetAll()
		{
			return GetByCriteria(DEFAULT_MAX_RESULTS);
		}

		public IList<T> GetAll(int maxResults)
		{
			return GetByCriteria(maxResults);
		}

		public IList<T> GetByCriteria(params ICriterion[] criterionList)
		{
			return GetByCriteria(DEFAULT_MAX_RESULTS, criterionList);
		}

		public IList<T> GetByCriteriaOrderBy(ICriterion[] criterionList, Order[] orderList)
		{
			return GetByCriteriaOrderBy(DEFAULT_MAX_RESULTS, criterionList, orderList, false);
		}

		public IList<T> GetByCriteriaOrderBy(int maxResults, ICriterion[] criterionList, Order[] orderList)
		{
			return GetByCriteriaOrderBy(maxResults, criterionList, orderList, false);
		}

		public IList<T> GetByCriteria(int maxResults, params ICriterion[] criterionList)
		{
			return GetByCriteriaOrderBy(maxResults, criterionList, null, false);
		}

		public IList<T> GetByCriteriaOrderBy(int maxResults, ICriterion[] criterionList, Order[] orderList, bool cacheThisQuery)
		{
			ICriteria criteria = (maxResults == DEFAULT_MAX_RESULTS ? _session.CreateCriteria(typeof(T)) : _session.CreateCriteria(typeof(T)).SetMaxResults(maxResults)).SetCacheable(cacheThisQuery);
			if (criterionList != null)
			{
				foreach (ICriterion criterion in criterionList)
					criteria.Add(criterion);
			}
			if (orderList != null)
			{
				foreach (Order order in orderList)
					criteria.AddOrder(order);
			}
			return criteria.List<T>();
		}

		public T GetUniqueByCriteria(params ICriterion[] criterionList)
		{
			ICriteria criteria = _session.CreateCriteria(typeof(T));

			foreach (ICriterion criterion in criterionList)
				criteria.Add(criterion);

			return criteria.UniqueResult<T>();
		}

		public IList<T> GetByExample(T exampleObject, params string[] excludePropertyList)
		{
			ICriteria criteria = _session.CreateCriteria(typeof(T));
			Example example = Example.Create(exampleObject);

			foreach (string excludeProperty in excludePropertyList)
				example.ExcludeProperty(excludeProperty);

			criteria.Add(example);

			return criteria.List<T>();
		}

		public IList<T> GetByQuery(string query)
		{
			return GetByQuery(DEFAULT_MAX_RESULTS, query);
		}

		public IList<T> GetByQuery(int maxResults, string query)
		{
			IQuery iQuery = _session.CreateQuery(query).SetMaxResults(maxResults);
			return iQuery.List<T>();
		}

		public T GetUniqueByQuery(string query)
		{
			IQuery iQuery = _session.CreateQuery(query);
			return iQuery.UniqueResult<T>();
		}
		#endregion

		/// <summary>
		/// The NHibernate Session object is exposed only to the Manager class.
		/// It is recommended that you...
		/// ...use the the NHibernateSession methods to control Transactions (unless you specifically want nested transactions).
		/// ...do not directly expose the Flush method (to prevent open transactions from locking your DB).
		/// </summary>
		public System.Type Type
		{
			get { return typeof(T); }
		}

		public ISession Session
		{
			get { return _session; }
		}
	}
}
