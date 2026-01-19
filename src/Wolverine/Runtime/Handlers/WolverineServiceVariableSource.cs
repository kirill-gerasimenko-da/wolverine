using System.Reflection;
using JasperFx;
using JasperFx.CodeGeneration;
using JasperFx.CodeGeneration.Frames;
using JasperFx.CodeGeneration.Model;
using JasperFx.CodeGeneration.Services;
using JasperFx.Core.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Wolverine.Runtime.Handlers;

/// <summary>
/// Wraps ServiceCollectionServerVariableSource to handle cases where a DI-registered service
/// has constructor dependencies on types created by middleware.
/// When normal DI resolution fails, this source checks if the service can be constructed
/// using middleware-created variables from MiddlewareContext.
/// </summary>
public class WolverineServiceVariableSource : IServiceVariableSource
{
    private readonly ServiceCollectionServerVariableSource _inner;
    private readonly IServiceCollection _services;

    public WolverineServiceVariableSource(IServiceContainer container, IServiceCollection services)
    {
        _inner = new ServiceCollectionServerVariableSource((ServiceContainer)container);
        _services = services;
    }

    public bool Matches(Type type)
    {
        return _inner.Matches(type);
    }

    public bool TryFindKeyedService(Type type, string key, out Variable? variable)
    {
        return _inner.TryFindKeyedService(type, key, out variable);
    }

    public Variable Create(Type type)
    {
        try
        {
            return _inner.Create(type);
        }
        catch (NotSupportedException)
        {
            // Check if we can construct this service using middleware variables
            var variable = TryCreateWithMiddlewareVariables(type);
            if (variable != null)
            {
                return variable;
            }

            throw;
        }
    }

    private Variable? TryCreateWithMiddlewareVariables(Type serviceType)
    {
        var middlewareVariables = MiddlewareContext.CurrentVariables;
        if (middlewareVariables.Count == 0)
        {
            return null;
        }

        // Find the implementation type for this service type
        var implementationType = FindImplementationType(serviceType);
        if (implementationType == null)
        {
            return null;
        }

        // Get the constructor
        var constructors = implementationType.GetConstructors();
        if (constructors.Length == 0)
        {
            return null;
        }

        // Try to find a constructor that can be satisfied
        foreach (var constructor in constructors.OrderByDescending(c => c.GetParameters().Length))
        {
            if (CanSatisfyConstructor(constructor, middlewareVariables))
            {
                return CreateConstructorVariable(implementationType, constructor, middlewareVariables);
            }
        }

        return null;
    }

    private Type? FindImplementationType(Type serviceType)
    {
        // Look through service descriptors to find the implementation type
        foreach (var descriptor in _services)
        {
            if (descriptor.ServiceType == serviceType)
            {
                if (descriptor.ImplementationType != null)
                {
                    return descriptor.ImplementationType;
                }

                // For factory-based registrations, we can't easily determine the implementation type
                return null;
            }
        }

        // If the service type is a concrete type, use it directly
        if (!serviceType.IsAbstract && !serviceType.IsInterface)
        {
            return serviceType;
        }

        return null;
    }

    private bool CanSatisfyConstructor(ConstructorInfo constructor, IReadOnlyList<Variable> middlewareVariables)
    {
        foreach (var parameter in constructor.GetParameters())
        {
            var paramType = parameter.ParameterType;

            // Check if middleware provides this type
            if (middlewareVariables.Any(v => v.VariableType == paramType || paramType.IsAssignableFrom(v.VariableType)))
            {
                continue;
            }

            // Check if DI can provide this type
            if (IsServiceAvailable(paramType))
            {
                continue;
            }

            // This parameter can't be satisfied
            return false;
        }

        return true;
    }

    private bool IsServiceAvailable(Type serviceType)
    {
        foreach (var descriptor in _services)
        {
            if (descriptor.ServiceType == serviceType)
            {
                return true;
            }
        }

        return false;
    }

    private Variable CreateConstructorVariable(Type implementationType, ConstructorInfo constructor, IReadOnlyList<Variable> middlewareVariables)
    {
        var constructorFrame = new ConstructorFrame(implementationType, constructor);

        // Set up the constructor arguments
        foreach (var parameter in constructor.GetParameters())
        {
            var paramType = parameter.ParameterType;

            // First check middleware variables
            var middlewareVar = middlewareVariables.FirstOrDefault(v =>
                v.VariableType == paramType || paramType.IsAssignableFrom(v.VariableType));

            if (middlewareVar != null)
            {
                constructorFrame.Parameters[parameter.Position] = middlewareVar;
            }
            // DI variables will be resolved automatically by the frame
        }

        return constructorFrame.Variable;
    }

    // Delegate other IServiceVariableSource methods to inner source
    public void ReplaceVariables(IMethodVariables variables)
    {
        _inner.ReplaceVariables(variables);
    }

    public void StartNewType()
    {
        _inner.StartNewType();
    }

    public void StartNewMethod()
    {
        _inner.StartNewMethod();
    }
}
