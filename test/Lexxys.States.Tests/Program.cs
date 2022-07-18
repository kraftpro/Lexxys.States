#define ASYNC

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.States.Tests;

class Program
{
#if ASYNC

	static async Task Main(string[] arg)
	{
		await Sample.Runner.GoAsync(arg);
	}

#else

	static void Main(string[] args)
	{
		Sample.Runner.Go(args);
	}

#endif
}

