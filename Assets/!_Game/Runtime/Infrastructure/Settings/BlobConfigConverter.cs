using System;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;

namespace BarkingBird.Runtime.Infrastructure.Settings
{
    [AttributeUsage(AttributeTargets.Class)]
    public class BlobConfigAttribute : Attribute { }
    
    public static class BlobConfigConverter
    {
        public static BlobAssetReference<T> CreateBlob<T>(object source) where T : unmanaged
        {
            using (var builder = new BlobBuilder(Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<T>();
            
                var sourceType = source.GetType();
                var targetType = typeof(T);
            
                foreach (var sourceField in sourceType.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    var targetField = targetType.GetField(sourceField.Name);
                    if (targetField != null && targetField.FieldType == sourceField.FieldType)
                    {
                        var value = sourceField.GetValue(source);
                        targetField.SetValueDirect(__makeref(root), value);
                    }
                }
            
                return builder.CreateBlobAssetReference<T>(Allocator.Persistent);
            }
        }
    }
}