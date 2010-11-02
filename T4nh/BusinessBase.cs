using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Genius.Northwind.BusinessServices
{
	/// <summary>
	/// Base for all business objects.
	/// </summary>
	/// <typeparam name="T">Type of the primary key.</typeparam>
	/// <typeparam name="C">Type of the entity</typeparam>
	[Serializable]
	public abstract class BusinessBase<T, C> : IComparable
	{
		private T _id = default(T);

		public virtual C Clone(bool inclSubcollections)
		{
			return Clone(inclSubcollections, new Dictionary<string, object>());
		}
		public abstract C Clone(bool inclSubcollections, Dictionary<string, object> usedObjects);

		public abstract void WriteXml(XmlTextWriter writer);
		public abstract void WriteXml(XmlTextWriter writer, params string[] moreFields);

		public virtual int CompareTo(object other)
		{
			if (other as BusinessBase<T, C> == null) return 0;
			return (this.Id as IComparable).CompareTo((other as BusinessBase<T, C>).Id as IComparable);
		}

		public override bool Equals(object obj)
		{
			return (obj as BusinessBase<T, C> != null)																// 1) Object is of same Type / Object is not null.
				&& (
					//MatchingHashCodes(obj)
					this.GetHashCode().Equals(obj.GetHashCode())													// 1) Hashcodes match.
					|| MatchingIds(obj as BusinessBase<T, C>));														// 2) Ids match.
		}

		public override int GetHashCode()
		{
			if (_id.Equals(0)) return base.GetHashCode();
			return (this.GetType().FullName + "#" + _id).GetHashCode();
		}

		private bool MatchingIds(BusinessBase<T, C> obj)
		{
			return (this._id != null && !this._id.Equals(default(T)))									// 1) this.Id is not null/default.
				&& (obj.Id != null && !obj.Id.Equals(default(T)))												// 1.5) obj.Id is not null/default.
				&& (this._id.Equals(obj.Id));																						// 2) Ids match.
		}

		private bool MatchingHashCodes(object obj)
		{
			return this.GetHashCode().Equals(obj.GetHashCode());											// 1) Hashcodes match.
		}

		public virtual T Id
		{
			get { return _id; }
			set { _id = value; }
		}
	}
}
