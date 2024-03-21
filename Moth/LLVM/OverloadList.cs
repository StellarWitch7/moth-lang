using Moth.LLVM.Data;

namespace Moth.LLVM;

public class OverloadList
{
    public string Name { get; }

    private List<Function> _functions = new List<Function>();

    public OverloadList(string name)
    {
        Name = name;
    }

    public void Add(Function func)
    {
        _functions.Add(func);
    }

    public Function Get(IReadOnlyList<Type> paramTypes)
    {
        Function? sufficient = null;
        
        foreach (var func in _functions)
        {
            MatchResult result = CompareParams(func.ParameterTypes, paramTypes);
            
            if (result == MatchResult.Exact)
            {
                return func;
            }
            else if (result == MatchResult.Sufficient)
            {
                if (sufficient != null)
                {
                    throw new Exception($"Cannot infer overload for call to \"{Name}\".");
                }
                
                sufficient = func;
            }
        }

        if (sufficient == null)
        {
            throw new Exception($"No candidate definition for call to \"{Name}\".");
        }

        return sufficient;
    }

    public bool TryGet(IReadOnlyList<Type> paramTypes, out Function func)
    {
        try
        {
            func = Get(paramTypes);

            if (func == null)
            {
                throw new Exception();
            }
            
            return true;
        }
        catch
        {
            func = null;
            return false;
        }
    }

    private MatchResult CompareParams(IReadOnlyList<Type> definition, IReadOnlyList<Type> call)
    {
        throw new NotImplementedException();
    }

    enum MatchResult
    {
        Exact,
        Sufficient,
        Insufficient
    }
}