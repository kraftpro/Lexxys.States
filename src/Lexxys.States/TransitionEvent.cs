namespace Lexxys.States;

public record TransitionEvent<T>(Statechart<T> Chart, Transition<T> Transition)
{
}
