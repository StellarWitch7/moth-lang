using Moth;

namespace Moth.AST;

public interface IASTNode
{
    int ColumnStart { get; init; }
    int LineStart { get; init; }
    int ColumnEnd { get; init; }
    int LineEnd { get; init; }

    string GetSource();
}
