
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexxys.States.Tests
{
	[TestClass]
	public class TokenFactoryTests
	{
		[TestMethod]
		public void CreateFactoryTest()
		{
			var scope = TokenFactory.Create("collection");
			Assert.IsNotNull(scope);
		}

		[TestMethod]
		public void CreateTest()
		{
			var scope = TokenFactory.Create("collection");
			Assert.IsNotNull(scope);
			var same = TokenFactory.Create("collection");
			Assert.AreEqual(scope, same);
		}

		[TestMethod]
		public void Create2Test()
		{
			var scope = TokenFactory.Create("collection", "generic");
			Assert.IsNotNull(scope);
			var same = TokenFactory.Create("collection", "generic");
			Assert.AreEqual(scope, same);
		}

		[TestMethod]
		public void Create3Test()
		{
			var scope1 = TokenFactory.Create("collection");
			Assert.IsNotNull(scope1);
			var scope = TokenFactory.Create(scope1, "generic");
			Assert.IsNotNull(scope);
			var same = TokenFactory.Create("collection", "generic");
			Assert.AreEqual(scope, same);
		}
	}
}
