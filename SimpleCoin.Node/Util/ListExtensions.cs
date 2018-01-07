namespace SimpleCoin.Node.Util
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public static class ListExtensions
	{
		public static IList<T> Clone<T>(this IList<T> list) where T : ICloneable
		{
			return list?.Select(x => (T)x.Clone()).ToList();
		}
	}
}