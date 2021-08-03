﻿using System;
using System.Collections.Generic;

namespace Lexxys.State.Tests
{

	public class Login
	{
		private readonly bool _test;

		public Login(bool test)
		{
			_test = test;
		}

		public LoginStates State { get; set; }

		public bool Success() => _test;
	}

	public enum LoginStates
	{
		Initialized = 1,
		NameEntered,
		PasswordEntered,
		NameAndPasswordEntered,
		Authenticated,
		NotAuthenticated // WrongNameOrPawword,
	}

	public class Entity
	{

		public void Command(ExpenseActions command)
		{
			Console.WriteLine($"cmd: %{command}");
		}

		private readonly List<ExpenseStep> _steps = new List<ExpenseStep>();

		public ExpenseStep Step { get; private set; }
		public int Value { get; internal set; }

		public void SetStep(ExpenseStep step)
		{
			Console.WriteLine($"{Step} -> {step}");
			_steps.Add(step);
			Step = step;
		}

		public void SetSupper(ExpenseSupper step)
		{
			Console.WriteLine($"{Step} !{step}");
		}

		public bool HasStep(ExpenseStep step)
		{
			return _steps.Contains(step);
		}

		public void ExitStep()
		{
			Console.WriteLine($"{Step} ->");
		}
	}



	public enum ExpenseStep
	{
		Canceled = -1,
		Dtaft = 0,
		AdminDraft,
		Questions,
		Review,
		PcaReview,
		LegalReview,
		NeedMoreInfo,
		FinOpsReview,
		RequestOfW9,
		UserApproval,
		UserComments,

		Submitted,
		ReviewQuestion,
	}

	public enum ExpenseSupper
	{
		Submitted,
		AdminReview,
		Done,
		Cancelled,
		Removed,
	}

	public enum ExpenseActions
	{
		Cancel,
		Submit,
		SubmitForReview,
		Answer,
		Ask,
		Approve,
		RequestLegalReview,
		Question,
		SubmitForPayment,
		SendToPca,
		SendToFinOps,
		ResponseToPca,
		Ignore,
		MoreInfo,
		RequestDocument,
		RequestDocumentOffline,
		SubmitForPcaReview,
		Modify,
		Back,
	}

	public struct StateStep
	{
		public static readonly StateStep Submitted = S(1, "Submitted", "The Expense has been submitted");

		public int Value { get; }
		public string Name => Map[Value].Name;
		public string Description => Map[Value].Description;

		public StateStep(int value)
		{
			Value = value;
		}

		private static StateStep S(int value, string name, string description)
		{
			Map.Add(value, (name, description));
			return new StateStep(value);
		}

		private static readonly Dictionary<int, (string Name, string Description)> Map = new Dictionary<int, (string Name, string Description)>();
	}
}
