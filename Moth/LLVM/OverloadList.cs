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

    public Function Get(IReadOnlyList<Data.Type> paramTypes)
    {
        Function? sufficient = null;
        bool hasMultipleCandidates = false;

        foreach (var func in _functions)
        {
            MatchResult result = CompareParams(func.ParameterTypes, paramTypes, func.IsVariadic);

            if (result == MatchResult.Exact)
            {
                return func;
            }
            else if (result == MatchResult.Sufficient)
            {
                if (sufficient != null)
                {
                    hasMultipleCandidates = true;
                }

                sufficient = func;
            }
        }

        if (sufficient == null)
        {
            throw new Exception($"No candidate definition for call to \"{Name}\".");
        }

        if (hasMultipleCandidates)
        {
            throw new Exception($"Cannot infer overload for call to \"{Name}\".");
        }

        return sufficient;
    }

    public bool TryGet(IReadOnlyList<Data.Type> paramTypes, out Function func)
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

    private MatchResult CompareParams(
        IReadOnlyList<Data.Type> definition,
        IReadOnlyList<Data.Type> call,
        bool isVariadic
    )
    {
        if (isVariadic)
        {
            if (definition.Count > call.Count)
            {
                return MatchResult.Insufficient;
            }
        }
        else
        {
            if (definition.Count != call.Count)
            {
                return MatchResult.Insufficient;
            }
        }

        if (ParamsAreEqual(definition, call))
        {
            return MatchResult.Exact;
        }
        else if (ParamsAreSuitable(definition, call))
        {
            return MatchResult.Sufficient;
        }

        return MatchResult.Insufficient;
    }

    private bool ParamsAreEqual(IReadOnlyList<Data.Type> definition, IReadOnlyList<Data.Type> call)
    {
        int index = 0;

        foreach (var type in definition)
        {
            if (!type.Equals(call[index]))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    private bool ParamsAreSuitable(
        IReadOnlyList<Data.Type> definition,
        IReadOnlyList<Data.Type> call
    )
    {
        int index = 0;

        foreach (var type in definition)
        {
            if (!call[index].CanConvertTo(type))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    enum MatchResult
    {
        Exact,
        Sufficient,
        Insufficient
    }
}
