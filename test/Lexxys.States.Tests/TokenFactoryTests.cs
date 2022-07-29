using System.Linq;

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
			var original = TokenScope.Create("collection").Scope("generic");
			Assert.IsNotNull(original);
			var copy = TokenScope.Create("collection").Scope("generic");
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
			var original = root.Scope("generic");
			Assert.IsNotNull(original);
			var copy = TokenScope.Create("collection").Scope("generic");
			var t1 = original.Token("token123");
			var t2 = copy.Token("token123");
			Assert.AreEqual(t1, t2);
		}

		[TestMethod]
		public void CannotDuplicateToken()
		{
			var root = TokenScope.Create("collection");
			Assert.IsNotNull(root);
			var t1 = root.Token(1, "One", "First Token");
			var t2 = root.Token(t1);
			Assert.AreEqual(t1, t2);
			t2 = root.Token(1, "Two", "Second Token");
			Assert.AreEqual(t1, t2);
		}

		[TestMethod]
		public void CanFindTokenScope()
		{
			var root = TokenScope.Create("root");
			Assert.IsNotNull(root);
			var r1 = root.Token(1, "One");
			
			var scope1 = root.Scope(r1);
			var actual = TokenScope.Scope(r1);

			Assert.AreEqual(scope1, actual);

			var t3 = scope1.Token(3, "Three");
			var scope3 = TokenScope.Scope(t3);

			Assert.IsNotNull(scope3);
			Assert.AreEqual(t3, scope3.Domain);
		}
	}
}
