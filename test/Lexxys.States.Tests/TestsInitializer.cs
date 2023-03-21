using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Lexxys.Logging;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexxys.States.Tests;

[TestClass]
public class TestsInitializer
{
	[AssemblyInitialize]
	public static void Initialize(TestContext _)
	{
		Statics.AddServices(o => o
			.AddConfigService()
			.AddLoggingService(o => o.AddConsole()));
	}

}
