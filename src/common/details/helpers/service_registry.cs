//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Diagnostics;

namespace Azure.AI.Details.Common.CLI;

public class ServiceRegistry: IServiceProvider, IDisposable
{
    private record Item;

    private record SingletonItem(object instance): Item;
    private record DisposableSingletonItem(IDisposable instance): Item;
    private record TransientItem(Type implementationType): Item;

    private readonly Dictionary<Type, Item> _services = new();

    public void RegisterService<Interface, Implementation>() where Implementation: Interface
    {
        _services.Add(typeof(Interface), new TransientItem(typeof(Implementation)));
    }

    public void RegisterServiceSingleton<Interface, Implementation>(Implementation instance) where Implementation: Interface
    {
        Debug.Assert(instance != null);
        if (instance is IDisposable disposable)
        {
            _services.Add(typeof(Interface), new DisposableSingletonItem(disposable));
        }
        else
        {
            _services.Add(typeof(Interface), new SingletonItem(instance));
        }
    }

    public object? GetService(Type serviceType)
    {
        if (_services.TryGetValue(serviceType, out var item))
        {
            switch (item)
            {
                case SingletonItem singletonItem:
                    return singletonItem.instance;
                case DisposableSingletonItem disposableSingletonItem:
                    return disposableSingletonItem.instance;
                case TransientItem transientItem:
                    return Activator.CreateInstance(transientItem.implementationType);
            }
        }
        return null;
    }

    public void Dispose()
    {
        foreach (var item in _services.Values)
        {
            if (item is DisposableSingletonItem disposableSingletonItem)
            {
                disposableSingletonItem.instance.Dispose();
            }
        }
    }
}