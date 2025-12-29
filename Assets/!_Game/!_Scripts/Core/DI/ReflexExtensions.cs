using System;
using Reflex.Core;

public static class ReflexExtensions
{
    public static void AddInterfacesAndSelf<T>(this ContainerBuilder builder)
    {
        var type = typeof(T);
        var interfaces = type.GetInterfaces();
        
        builder.AddSingleton(type, type);
        builder.AddSingleton(type, interfaces);
    }
    
    public static void AddInterfacesAndSelf(this ContainerBuilder builder, object instance)
    {
        var type = instance.GetType();
        var interfaces = type.GetInterfaces();
        
        builder.AddSingleton(instance, type);
        builder.AddSingleton(instance, interfaces);
    }
    
    public static ContainerBuilder NonLazy<T>(this ContainerBuilder builder)
    {
        builder.OnContainerBuilt += OnBuilderContainerBuilt; 
        return builder;

        void OnBuilderContainerBuilt(Container container)
        {
            builder.OnContainerBuilt -= OnBuilderContainerBuilt; 
            container.Single<T>();
        }
    }
}