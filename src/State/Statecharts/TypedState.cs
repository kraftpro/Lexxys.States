using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace State.Statecharts
{
	class TypedState<TState, TEvent, TEntity>
	{
		public TypedState()
		{
		}

		public TState Id { get; }
		public string Permission { get; }
		public IReadOnlyCollection<Event<TState, TEvent, TEntity>> Events { get; }
	}

	public class Event<TState, TEvent, TEntity>
	{
	}
}
