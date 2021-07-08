using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace State
{
	public class Expense
	{
		private ExpenseStep _step;

		public int Value { get; set; }

		public Expense()
		{
			_step = ExpenseStep.Empty;
		}

		public ExpenseStep Step
		{
			get
			{
				return _step;
			}
			set
			{
				if (_step != value)
				{
					Console.WriteLine($"Step {_step} -> {value}");
					_step = value;
				}
			}
		}

		#region Actions

		public void CreateDraft()
		{
			Step = ExpenseStep.Draft;
			Console.WriteLine("Draft created");
		}

		public void CreateAdminDraft()
		{
			Step = ExpenseStep.AdminDraft;
			Console.WriteLine("Admin draft created");
		}

		public void AskQuestion()
		{
			Step = ExpenseStep.Questions;
			Console.WriteLine("Question asked");
		}

		public void SendToReview()
		{
			Step = ExpenseStep.Review;
			Console.WriteLine("Sent to review");
		}

		public void SendToPcaReview()
		{
			Step = ExpenseStep.PcaReview;
			Console.WriteLine("Sent to review by PCA");
		}

		public void SendToLegalReview()
		{
			Step = ExpenseStep.LegalReview;
			Console.WriteLine("Sent to review by legal department");
		}

		public void RequestMoreInfo()
		{
			Step = ExpenseStep.NeedMoreInfo;
			Console.WriteLine("More info requested");
		}

		public void SendToFinOpsReview()
		{
			Step = ExpenseStep.FinOpsReview;
			Console.WriteLine("Sent to review by financial ops.");
		}

		public void Cancel()
		{
			Step = ExpenseStep.Canceled;
			Console.WriteLine("Expense canceled");
		}

		public void Close()
		{
			Step = ExpenseStep.Closed;
			Console.WriteLine("Expense closed");
		}

		public void Delete()
		{
			Step = ExpenseStep.Empty;
			Console.WriteLine("Expense deleted");
		}

		#endregion
	}

	public enum ExpenseStep
	{
		Empty = -99,
		AdminDraft = 0,
		Draft = 1,
		Questions = 4,
		Review = 5,
		PcaReview = 10,
		LegalReview = 15,
		NeedMoreInfo = 20,
		FinOpsReview = 25,
		Closed = 99,
		Canceled = -1,
		Template = -10
	}
}
