using System.Security.Principal;

namespace State
{
	internal class EmptyPrincipal: IPrincipal
	{
		public static readonly IPrincipal Instance = new EmptyPrincipal();

		private EmptyPrincipal()
		{
			
		}

		public IIdentity Identity => EmptyIdentity.IdentityInstance;

		public bool IsInRole(string role)
		{
			return true;
		}

		private class EmptyIdentity: IIdentity
		{
			public static readonly IIdentity IdentityInstance = new EmptyIdentity();

			private EmptyIdentity()
			{
			}

			public string AuthenticationType => "empty";
			public bool IsAuthenticated => true;
			public string Name => "empty";
		}
	}
}