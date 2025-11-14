using Reflex.Core;
using Reflex.Enums;

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
}