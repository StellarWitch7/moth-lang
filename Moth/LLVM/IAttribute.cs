using System.Reflection;

namespace Moth.LLVM;

public interface IAttribute
{
    public static Func<string, IReadOnlyList<object>, IAttribute> MakeCreationFunction(IEnumerable<SystemType> types)
    {
        var attributes = new Dictionary<string, Func<IReadOnlyList<object>, IAttribute>>();
        
        foreach (var type in types)
        {
            if(!type.IsAssignableTo(typeof(IAttributeImpl)) || type.IsAbstract)
                continue;

            var map = type.GetInterfaceMap(typeof(IAttributeImpl));
            MethodInfo? GetMethod(string name)
            {
                return map.TargetMethods.FirstOrDefault(m => m.Name == name);
            }

            if(GetMethod("get_Identifier") is not {} identifierMethod) continue;
            if(identifierMethod.Invoke(null, null) is not string identifier) continue;
            
            if(attributes.ContainsKey(identifier))
                continue;
            
            if(GetMethod("Create") is not {} createMethod) continue;
            var create = createMethod.CreateDelegate<Func<IReadOnlyList<object>, IAttribute>>();
            
            attributes.Add(identifier, create);
        }

        return (name, parameters) =>
        {
            var create = attributes[name];
            return create(parameters);
        };
    }
}

public interface IAttributeImpl: IAttribute
{
    public static abstract string Identifier { get; }
    public static abstract IAttribute Create(IReadOnlyList<object> parameters);
}