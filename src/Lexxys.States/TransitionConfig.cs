﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Lexxys.States;

#pragma warning disable CA1819

public class TransitionConfig
{
	public const string TokenDomain = "^";

	public int? Id { get; init; }
	public string? Name { get; init; }
	public string? Description { get; init; }
	public string? Source { get; set; }
	public string Destination { get; init; }
	public string? Guard { get; init; }
	public string? Action { get; init; }
	public bool Continues { get; init; }
	public string[]? Role { get; init; }

	public TransitionConfig()
	{
		Destination = String.Empty;
	}

	public TransitionConfig(string destination)
	{
		Destination = destination;
	}

	public TransitionConfig(string destination, int? id = null, string? name = null, string? description = null, string? source = null, string? guard = null, string? action = null, bool continues = false, IReadOnlyCollection<string>? roles = null)
	{
		if (destination is null || (destination = destination.Trim()).Length == 0)
			throw new ArgumentNullException(nameof(destination));
		(Id, Name) = name is null ? (id, name): FixName(id, name);
		Description = description;
		Source = source;
		Destination = destination;
		Guard = guard;
		Action = action;
		Continues = continues;
		Role = roles?.ToArray();
	}

	internal Transition<T> Create<T>(ITokenScope scope, IReadOnlyCollection<State<T>> states, Func<string, IStateAction<T>?> actionBuilder, Func<string, IStateCondition<T>?> conditionBuilder)
	{
		if (scope is null)
			throw new ArgumentNullException(nameof(scope));
		if (states is null)
			throw new ArgumentNullException(nameof(states));
		if (actionBuilder is null)
			throw new ArgumentNullException(nameof(actionBuilder));
		if (conditionBuilder is null)
			throw new ArgumentNullException(nameof(conditionBuilder));

		var transition = new Transition<T>(
			source: IsEmptyReference(Source) ? null: FindState(Source!, states),
			destination: FindState(Destination, states),
			@event: CreateToken(scope.Scope(scope.Token(TokenDomain)), Id, Name, Description),
			continues: Continues,
			action: String.IsNullOrEmpty(Action) ? null: actionBuilder(Action!),
			guard: String.IsNullOrEmpty(Guard) ? null: conditionBuilder(Guard!),
			roles: Role);
		return transition;
	}

	internal static (int? Id, string Name) FixName(int? id, string name) => StateConfig.FixName(id, name);

	internal static bool IsEmptyReference(string? reference) => String.IsNullOrEmpty(reference) || reference == ".";

	internal static State<T> FindState<T>(string reference, IReadOnlyCollection<State<T>> states)
	{
		int? intReference = int.TryParse(reference, out var id) ? id: null;
		var state = states.FirstOrDefault(o => String.Equals(o.Name, reference, StringComparison.OrdinalIgnoreCase) || o.Token.Id == intReference)
			?? throw new InvalidOperationException($"Cannot find state \"{reference}\".");
		return state;
	}

	internal void GenerateCode(TextWriter writer, string objName, string varName, Dictionary<StateConfig, string> stateNames, string indent)
	{
		var source = FindState(Source, objName, stateNames);
		var destination = FindState(Destination, objName, stateNames);

		writer.WriteLine(TrimEnd(
			$"{indent}var {varName} = new Transition<{objName}>({source}, {destination}, {T("t", Id, Name, Description)}, {(Continues ? "true" : "false")}, {A(Action, objName)}, {P(Guard, objName)}, {RR(Role)}",
			", null, false, null, null, null") + ");");
	}

	private static string FindState(string? reference, string objName, Dictionary<StateConfig, string> states)
	{
		if (IsEmptyReference(reference))
			return $"State<{objName}>.Empty";
		int? intReference = int.TryParse(reference, out var id) ? id : null;
		var state = states.FirstOrDefault(o => String.Equals(o.Key.Name, reference, StringComparison.OrdinalIgnoreCase) || (intReference is not null && o.Key.Id == intReference));
		return state.Value ?? throw new InvalidOperationException($"\"Cannot find state variable for '{reference}'.\"");
	}

	static Token CreateToken(ITokenScope scope, int? id, string? name, string? description)
	{
		if (id is not null)
			return scope.Token(id.GetValueOrDefault(), name, description);
		if (name is not null)
			return scope.Token(name, description);
		return Token.Empty;
	}

	private static string TrimEnd(string value, string defaults) => StateConfig.TrimEnd(value, defaults);
	private static string T(string scope, int? id, string? name, string? description) => StateConfig.T(scope, id, name, description);
	private static string P(string? value, string objName) => StateConfig.P(value, objName);
	private static string A(string? value, string objName) => StateConfig.A(value, objName);
	private static string RR(IReadOnlyCollection<string>? roles) => StateConfig.RR(roles);
}
