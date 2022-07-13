using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.States.Tests
{
	public class EchoAction<T>: IStateAction<T>
	{
		public string Text { get; }

		public EchoAction(string text)
		{
			Text = text;
		}

		public Func<T, Statechart<T>, State<T>?, Transition<T>?, Task> GetAsyncDelegate()
		{
			return (e, c, s, t) =>
			{
				var text = new StringBuilder();
				text.Append(c.Name);
				if (s != null)
					text.Append(".").Append(s.Name);
				if (t != null)
					text.Append(">").Append(t.Destination?.Name ?? "*");
				text.Append(": ").Append(Text);
				Console.WriteLine(text.ToString());
				return Task.CompletedTask;
			};
		}

		public Action<T, Statechart<T>, State<T>?, Transition<T>?> GetDelegate()
		{
			return (e, c, s, t) =>
			{
				var text = new StringBuilder();
				text.Append(c.Name);
				if (s != null)
					text.Append(".").Append(s.Name);
				if (t != null)
					text.Append(">").Append(t.Destination?.Name ?? "*");
				text.Append(": ").Append(Text);
				Console.WriteLine(text.ToString());
			};
		}
	}
}
