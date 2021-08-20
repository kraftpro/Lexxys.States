
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
			var original = TokenFactory.Create("collection", "generic");
			Assert.IsNotNull(original);
			var copy = TokenFactory.Create("collection", "generic");
			Assert.IsNotNull(copy);
			var t1 = original.Token("token123");
			var t2 = copy.Token("token123");
			Assert.AreEqual(t1, t2);
		}

		[TestMethod]
		public void Create3Test()
		{
			var root = TokenFactory.Create("collection");
			Assert.IsNotNull(root);
			var original = TokenFactory.Create(root, "generic");
			Assert.IsNotNull(original);
			var copy = TokenFactory.Create("collection", "generic");
			var t1 = original.Token("token123");
			var t2 = copy.Token("token123");
			Assert.AreEqual(t1, t2);
		}
	}
}
