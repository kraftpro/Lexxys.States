//#define TRACE_ROSLYN
using System;
using System.Threading.Tasks;

namespace Lexxys.States;

public interface IStateAction<T>
{
	Action<T, Statechart<T>, State<T>?, Transition<T>?> GetDelegate();
	Func<T, Statechart<T>, State<T>?, Transition<T>?, Task> GetAsyncDelegate();
}