using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexxys.States.Tests
{
	[TestClass]
	public class TokenFactoryTest
	{
		[TestMethod]
		public void CreateFactoryTest()
		{
			var scope = TokenScope.Create("collection");
			Assert.IsNotNull(scope);
		}

		[TestMethod]
		public void CreateTest()
		{
			var scope = TokenScope.Create("collection");
			Assert.IsNotNull(scope);
			var same = TokenScope.Create("collection");
			Assert.AreEqual(scope, same);
		}

		[TestMethod]
		public void Create2Test()
		{
			var original = TokenScope.Create("collection", "generic");
			Assert.IsNotNull(original);
			var copy = TokenScope.Create("collection", "generic");
			Assert.IsNotNull(copy);
			var t1 = original.Token("token123");
			var t2 = copy.Token("token123");
			Assert.AreEqual(t1, t2);
		}

		[TestMethod]
		public void Create3Test()
		{
			var root = TokenScope.Create("collection");
			Assert.IsNotNull(root);
			var original = root.WithDomain("generic");
			Assert.IsNotNull(original);
			var copy = TokenScope.Create("collection", "generic");
			var t1 = original.Token("token123");
			var t2 = copy.Token("token123");
			Assert.AreEqual(t1, t2);
		}
	}
}
