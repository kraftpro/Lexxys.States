using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

using Lexxys;

#nullable enable

namespace State.Test1
{
	public static class IPrincipalExtensions
	{
		public static bool IsInRole(this IPrincipal? principal, IEnumerable<string?>? roles)
		{
			if (principal == null)
				return true;
			if (roles == null)
				return true;
			var result = true;
			foreach (var item in roles)
			{
				if (String.IsNullOrEmpty(item))
					continue;
				if (principal.IsInRole(item))
					return true;
				result = false;
			}
			return result;
		}
	}
}
