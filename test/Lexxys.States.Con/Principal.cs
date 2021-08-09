using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;

namespace Lexxys.States.Con
{
	public class Principal: IPrincipal
	{
		public static readonly Principal Empty = new Principal();

		private IReadOnlyCollection<string> _roles;
		public IIdentity Identity { get; }

		private Principal()
		{
			Identity = new DirectIdentity("auto", "anonymous", false);
			_roles = Array.Empty<string>();
		}

		public Principal(string? authenticationType, string name, IEnumerable<string> roles)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			if (roles == null)
				throw new ArgumentNullException(nameof(roles));

			Identity = new DirectIdentity(authenticationType ?? "auto", name, true);
			_roles = roles.ToList();
		}

		public Principal(string name, IEnumerable<string> roles)
			: this(null, name, roles)
		{
		}

		public Principal(string? authenticationType, string name)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			Identity = new DirectIdentity(authenticationType ?? "auto", name, true);
			_roles = Array.Empty<string>();
		}

		public Principal(string name)
			: this(null, name)
		{
		}

		public bool IsInRole(string role)
			=> _roles == null || _roles.Any(o => String.Equals(o, role, StringComparison.OrdinalIgnoreCase));

		class DirectIdentity: IIdentity
		{
			public string AuthenticationType { get; }
			public bool IsAuthenticated { get; }
			public string Name { get; }

			public DirectIdentity(string authenticationType, string name, bool isAuthenticated)
			{
				AuthenticationType = authenticationType ?? throw new ArgumentNullException(nameof(authenticationType));
				Name = name ?? throw new ArgumentNullException(nameof(name));
				IsAuthenticated = isAuthenticated;
			}
		}
	}
}
