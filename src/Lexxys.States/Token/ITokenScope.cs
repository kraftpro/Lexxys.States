namespace Lexxys;

public interface ITokenScope
{
	Token Domain { get; }
	ITokenScope WithDomain(Token domain);
	Token Token(int id, string? name = null, string? description = null, Token? domain = null);
	Token Token(string name, string? description = null, Token? domain = null);
	Token? Find(int id, Token? domain = null);
}
