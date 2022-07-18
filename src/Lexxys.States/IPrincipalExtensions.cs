using System;
using System.Collections.Generic;
using System.Security.Principal;

namespace Lexxys.States;

static class IPrincipalExtensions
{
	public static bool IsInRole(this IPrincipal? principal, IEnumerable<string?>? roles)
	{
		if (principal == null || roles == null)
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
