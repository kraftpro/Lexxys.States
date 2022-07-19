#define ASYN
#if ASYNC

using System.Threading.Tasks;
namespace Lexxys.States.Tests.Sample;

class Program
{
	static async Task Main(string[] arg) => await Sample.Runner.GoAsync(arg);
}

#else

using Lexxys.States.Tests.Sample;

Runner.Go(args);

#endif

