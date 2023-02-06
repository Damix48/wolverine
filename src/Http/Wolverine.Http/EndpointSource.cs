using System.Reflection;
using JasperFx.CodeGeneration.Frames;
using JasperFx.Core;
using JasperFx.Core.Reflection;
using JasperFx.TypeDiscovery;
using Microsoft.AspNetCore.Mvc.Routing;
using Wolverine.Attributes;

namespace Wolverine.Http;

internal class EndpointSource
{
    private readonly IList<Assembly> _assemblies;

    public EndpointSource(IEnumerable<Assembly> assemblies)
    {
        _assemblies = assemblies.ToList();
        
        _typeFilters.Includes += type =>
            type.Name.EndsWith("Endpoint", StringComparison.OrdinalIgnoreCase) ||
            type.Name.EndsWith("Endpoints", StringComparison.OrdinalIgnoreCase);

    }
    
    private readonly ActionMethodFilter _methodFilters = new();
    private readonly CompositeFilter<Type> _typeFilters = new();

    internal async Task<MethodCall[]> FindActions()
    {
        var discovered =
            await TypeRepository.FindTypes(_assemblies, TypeClassification.Concretes | TypeClassification.Closed, _typeFilters.Matches);

        return discovered
            .Distinct()
            .SelectMany(actionsFromType).ToArray();
    }

    private IEnumerable<MethodCall> actionsFromType(Type type)
    {
        return type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static)
            .Where(m => _methodFilters.Matches(m))
            .Select(m => new MethodCall(type, m));
    }
}

internal class ActionMethodFilter : CompositeFilter<MethodInfo>
{
    public ActionMethodFilter()
    {
        Excludes += method => method.DeclaringType == typeof(object);
        Excludes += method => method.Name == ReflectionHelper.GetMethod<IDisposable>(x => x.Dispose()).Name;
        Excludes += method => method.ContainsGenericParameters;
        Excludes += method => method.IsSpecialName;
        Excludes += method => method.HasAttribute<WolverineIgnoreAttribute>();

        Includes += method => method.HasAttribute<HttpMethodAttribute>();
    }

    public void IgnoreMethodsDeclaredBy<T>()
    {
        Excludes += x => x.DeclaringType == typeof(T);
    }
}