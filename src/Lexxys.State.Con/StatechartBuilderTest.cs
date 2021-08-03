using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.State.Con
{
	using States;

	class StatechartBuilderTest
	{
		public static void Go()
		{
			//var a0 = new StatechartBuilder0();
			//var a1 = new StatechartBuilder<Expense>();
			//var a2 = new StatechartBuilder<Expense, ExpenseStep>();
			//var a3 = new StatechartBuilder<Expense, ExpenseStep, ExpenseActions>();

			//var b0 = new StatechartBuilder<object, Enum, object>();
			//var b1 = new StatechartBuilder<Expense, Enum, object>();
			//var b2 = new StatechartBuilder<Expense, ExpenseStep, object>();
			//var b3 = new StatechartBuilder<Expense, ExpenseStep, ExpenseActions>();

			var exp = TokenFactory.Create("statecharts", "expenses");
			var tf = TokenFactory.Create(exp, "main");
			var chart = new StatechartBuilder<Expense>(exp.Token("main"));
			chart
				.OnEnter((e, s) => e.SetSupper((ExpenseSupper)(int)s))
				.OnExit((e, s) => e.ExitStep())
				.State(tf.Token(ExpenseSupper.Submitted))
					.Begin(tf.Token(ExpenseSupper.Submitted))
						.State(tf.Token(ExpenseStep.Dtaft), out var state1)
							.When(tf.Token(ExpenseActions.SubmitForPcaReview)).GoTo(0)
							.When(tf.Token(ExpenseActions.SubmitForReview)).GoTo(ExpenseStep.Review)
						.State(ExpenseStep.Review)
							.When(ExpenseActions.Ask).GoTo(ExpenseStep.ReviewQuestion)
							.When(ExpenseActions.Approve).GoTo(0)
						.State(ExpenseStep.ReviewQuestion)
							.When(ExpenseActions.Answer).GoTo(ExpenseStep.Review)
							.When(o => (++o.Value) % 3 == 0).GoTo(0)
						.Source
					.End<Expense, ExpenseSupper, ExpenseActions>()
				.State(ExpenseSupper.AdminReview)
					.Begin<Expense, ExpenseStep, ExpenseActions>()
						.State(ExpenseStep.PcaReview)
							.When(ExpenseActions.RequestLegalReview).GoTo(ExpenseStep.LegalReview)
							.When(ExpenseActions.Question).GoTo(ExpenseStep.NeedMoreInfo)
							.When(ExpenseActions.SubmitForPayment).GoTo(ExpenseStep.FinOpsReview)
							.When(ExpenseActions.Back).GoTo(0 /* state1 */)
						.State(ExpenseStep.LegalReview)
						.State(ExpenseStep.Questions)
						.State(ExpenseStep.FinOpsReview)
						.State(ExpenseStep.AdminDraft)
						.State(ExpenseStep.NeedMoreInfo)
					.End<Expense, ExpenseSupper, ExpenseActions>()
				.State(ExpenseSupper.Done)
				.State(ExpenseSupper.Cancelled)
				.State(ExpenseSupper.Removed)
			.End();

			var chart2 = new StatechartBuilder<Expense>();
			chart2
				.Begin("expenses")
					.OnEnter((e, s) => e.SetSupper(s))
					.OnExit((e, s) => e.ExitStep())
					.State(ExpenseSupper.Submitted)
						.Begin<Expense, ExpenseStep, ExpenseActions>()
							.State(ExpenseStep.Dtaft, out var state2)
								.When(ExpenseActions.SubmitForPcaReview).GoTo(0)
								.When(ExpenseActions.SubmitForReview).GoTo(ExpenseStep.Review)
							.State(ExpenseStep.Review)
								.When(ExpenseActions.Ask).GoTo(ExpenseStep.ReviewQuestion)
								.When(ExpenseActions.Approve).GoTo(0)
							.State(ExpenseStep.ReviewQuestion)
								.When(ExpenseActions.Answer).GoTo(ExpenseStep.Review)
								.When(o => (++o.Value) % 3 == 0).GoTo(0)
							.Source
						.End<Expense, ExpenseSupper, ExpenseActions>()
					.State(ExpenseSupper.AdminReview)
						.Begin<Expense, ExpenseStep, ExpenseActions>()
							.State(ExpenseStep.PcaReview)
								.When(ExpenseActions.RequestLegalReview).GoTo(ExpenseStep.LegalReview)
								.When(ExpenseActions.Question).GoTo(ExpenseStep.NeedMoreInfo)
								.When(ExpenseActions.SubmitForPayment).GoTo(ExpenseStep.FinOpsReview)
								.When(ExpenseActions.Back).GoTo(0 /* state1 */)
							.State(ExpenseStep.LegalReview)
							.State(ExpenseStep.Questions)
							.State(ExpenseStep.FinOpsReview)
							.State(ExpenseStep.AdminDraft)
							.State(ExpenseStep.NeedMoreInfo)
						.End<Expense, ExpenseSupper, ExpenseActions>()
					.State(ExpenseSupper.Done)
					.State(ExpenseSupper.Cancelled)
					.State(ExpenseSupper.Removed)
				.End();

			var expenseChart0 = new StatechartBuilder<Expense>()
				.State(ExpenseStep.Dtaft)
					.When(ExpenseActions.Submit).GoTo(ExpenseStep.PcaReview)
				.State(ExpenseStep.Review)
					.When(ExpenseActions.Ask).GoTo(ExpenseStep.Questions)
					.When(ExpenseActions.Approve).GoTo(ExpenseStep.PcaReview)
				.State(ExpenseStep.Questions)
					.When(ExpenseActions.Answer).GoTo(ExpenseStep.Review)
				.State(ExpenseStep.PcaReview)
					.When(ExpenseActions.RequestLegalReview).GoTo(ExpenseStep.LegalReview)
					.When(ExpenseActions.Question).GoTo(ExpenseStep.NeedMoreInfo)
					.When(ExpenseActions.SubmitForPayment).GoTo(ExpenseStep.FinOpsReview)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)
				.State(ExpenseStep.LegalReview)
					.When(ExpenseActions.SendToPca).And(o => o.HasStep(ExpenseStep.FinOpsReview)).GoTo(ExpenseStep.FinOpsReview)
					.When(ExpenseActions.SendToPca).GoTo(ExpenseStep.PcaReview)
					.When(ExpenseActions.SubmitForPayment).GoTo(ExpenseStep.FinOpsReview)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)

				.State(ExpenseStep.NeedMoreInfo)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)
				.State(ExpenseStep.FinOpsReview)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)
				.State(ExpenseStep.RequestOfW9)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)
				.State(ExpenseStep.UserApproval)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)
				.State(ExpenseStep.AdminDraft)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)
				.State(ExpenseStep.UserComments)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)
				.State(ExpenseStep.Canceled)
					.Final()
				;

			var expenseChar = new StatechartBuilder<Expense>()
				.OnEnter((e, s) => e.SetStep(s))
				.OnExit((e, s) => e.ExitStep())
				.State(ExpenseStep.Dtaft)
					.When(ExpenseActions.Submit).GoTo(ExpenseStep.PcaReview)
				.State(ExpenseStep.Review)
					.When(ExpenseActions.Ask).GoTo(ExpenseStep.Questions)
					.When(ExpenseActions.Approve).GoTo(ExpenseStep.PcaReview)
				.State(ExpenseStep.Questions)
					.When(ExpenseActions.Answer).GoTo(ExpenseStep.Review)
				.State(ExpenseStep.PcaReview)
					.When(ExpenseActions.RequestLegalReview).GoTo(ExpenseStep.LegalReview)
					.When(ExpenseActions.Question).GoTo(ExpenseStep.NeedMoreInfo)
					.When(ExpenseActions.SubmitForPayment).GoTo(ExpenseStep.FinOpsReview)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)
				.State(ExpenseStep.LegalReview)
					.When(ExpenseActions.SendToPca).And(o => o.HasStep(ExpenseStep.FinOpsReview)).GoTo(ExpenseStep.FinOpsReview)
					.When(ExpenseActions.SendToPca).GoTo(ExpenseStep.PcaReview)
					.When(ExpenseActions.SubmitForPayment).GoTo(ExpenseStep.FinOpsReview)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)

				.State(ExpenseStep.NeedMoreInfo)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)
				.State(ExpenseStep.FinOpsReview)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)
				.State(ExpenseStep.RequestOfW9)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)
				.State(ExpenseStep.UserApproval)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)
				.State(ExpenseStep.AdminDraft)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)
				.State(ExpenseStep.UserComments)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)
				.State(ExpenseStep.Canceled)
					.Final()
				;

			var expenseCh = new StatechartBuilder<Expense>()
				.OnEnter((e, s) => e.SetStep((ExpenseStep)s))
				.OnExit((e, s) => e.ExitStep())
				.State(ExpenseStep.Dtaft)
					.When(ExpenseActions.Submit).GoTo(ExpenseStep.PcaReview)
				.State(ExpenseStep.Review)
					.When(ExpenseActions.Ask).GoTo(ExpenseStep.Questions)
					.When(ExpenseActions.Approve).GoTo(ExpenseStep.PcaReview)
				.State(ExpenseStep.Questions)
					.When(ExpenseActions.Answer).GoTo(ExpenseStep.Review)
				.State(ExpenseStep.PcaReview)
					.When(ExpenseActions.RequestLegalReview).GoTo(ExpenseStep.LegalReview)
					.When(ExpenseActions.Question).GoTo(ExpenseStep.NeedMoreInfo)
					.When(ExpenseActions.SubmitForPayment).GoTo(ExpenseStep.FinOpsReview)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)
				.State(ExpenseStep.LegalReview)
					.When(ExpenseActions.SendToPca).And(o => o.HasStep(ExpenseStep.FinOpsReview)).GoTo(ExpenseStep.FinOpsReview)
					.When(ExpenseActions.SendToPca).GoTo(ExpenseStep.PcaReview)
					.When(ExpenseActions.SubmitForPayment).GoTo(ExpenseStep.FinOpsReview)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)

				.State(ExpenseStep.NeedMoreInfo)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)
				.State(ExpenseStep.FinOpsReview)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)
				.State(ExpenseStep.RequestOfW9)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)
				.State(ExpenseStep.UserApproval)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)
				.State(ExpenseStep.AdminDraft)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)
				.State(ExpenseStep.UserComments)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)
				.State(ExpenseStep.Canceled)
					.Final()
				;

			var expCh = new StatechartBuilder0()
				.OnEnter((e, s) => e.SetStep(s))
				.OnExit((e, s) => e.ExitStep())
				.State(ExpenseStep.Dtaft)
					.When(ExpenseActions.Submit).GoTo(ExpenseStep.PcaReview)
				.State(ExpenseStep.Review)
					.When(ExpenseActions.Ask).GoTo(ExpenseStep.Questions)
					.When(ExpenseActions.Approve).GoTo(ExpenseStep.PcaReview)
				.State(ExpenseStep.Questions)
					.When(ExpenseActions.Answer).GoTo(ExpenseStep.Review)
				.State(ExpenseStep.PcaReview)
					.When(ExpenseActions.RequestLegalReview).GoTo(ExpenseStep.LegalReview)
					.When(ExpenseActions.Question).GoTo(ExpenseStep.NeedMoreInfo)
					.When(ExpenseActions.SubmitForPayment).GoTo(ExpenseStep.FinOpsReview)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)
				.State(ExpenseStep.LegalReview)
					.When(ExpenseActions.SendToPca).And(o => o.HasStep(ExpenseStep.FinOpsReview)).GoTo(ExpenseStep.FinOpsReview)
					.When(ExpenseActions.SendToPca).GoTo(ExpenseStep.PcaReview)
					.When(ExpenseActions.SubmitForPayment).GoTo(ExpenseStep.FinOpsReview)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)

				.State(ExpenseStep.NeedMoreInfo)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)
				.State(ExpenseStep.FinOpsReview)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)
				.State(ExpenseStep.RequestOfW9)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)
				.State(ExpenseStep.UserApproval)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)
				.State(ExpenseStep.AdminDraft)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)
				.State(ExpenseStep.UserComments)
					.When(ExpenseActions.Cancel).GoTo(ExpenseStep.Canceled)
				.State(ExpenseStep.Canceled)
					.Final()
				;
		}

		class Expo
		{
			public void Open()
			{
				Console.WriteLine("Open");
			}

			public void Close()
			{
				Console.WriteLine("Close");
			}
		}

		enum ExpoState
		{
			NotPlanned,
			Planned,
			Openned,
			Closed,
		}

		enum ExpoCommand
		{

		}
	}
}
