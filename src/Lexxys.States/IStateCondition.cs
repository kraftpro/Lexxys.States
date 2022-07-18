//#define TRACE_ROSLYN
using System;
using System.Threading.Tasks;

namespace Lexxys.States;

public interface IStateCondition<T>
{
	Func<T, Statechart<T>, State<T>?, Transition<T>?, bool> GetDelegate();
	Func<T, Statechart<T>, State<T>?, Transition<T>?, Task<bool>> GetAsyncDelegate();
}