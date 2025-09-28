using MiddleManClient.ServerContracts;
using System.Reflection;

namespace MiddleManClient.MethodProcessing.MethodParsing
{
  public class MethodParser : IMethodParser
  {
    public WebSocketClientMethod Parse(MethodInfo methodInfo)
    {
      return new WebSocketClientMethod
      {
        Name = methodInfo.Name,
        Arguments = [.. methodInfo.GetParameters().Select(x => DescribeType(x.Name,x.ParameterType))],
        Returns = GetReturnType(methodInfo)
      };
    }

    private static WebSocketClientMethodArgument? GetReturnType(MethodInfo methodInfo)
    {
      if (methodInfo.ReturnType == typeof(void))
      {
        return null;
      }

      return DescribeType(null, methodInfo.ReturnType);
    }

    private static WebSocketClientMethodArgument DescribeType(string? name, Type type)
    {
      if (type.IsEnum)
      {
        throw new NotSupportedException("Enum types are not supported in method arguments or return types.");
      }

      if (type == typeof(object))
      {
        throw new NotSupportedException("Object is not supported in method arguments or return types.");
      }

      // Arrays
      if (type.IsArray)
      {
        var elementType = type.GetElementType()!;
        var elementDescription = DescribeType(name, elementType);
        elementDescription.IsArray = true;

        return elementDescription;
      }

      // Lists
      if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
      {
        var elementType = type.GetGenericArguments()[0];
        var elementDescription = DescribeType(name, elementType);
        elementDescription.IsArray = true;

        return elementDescription;
      }
      
      var argument = new WebSocketClientMethodArgument
      {
        Name = name,
        Type = type.FullName,
        IsPrimitive = type.IsPrimitive || type == typeof(string) || type == typeof(decimal),
        IsArray = type.IsArray,
        IsNullable = Nullable.GetUnderlyingType(type) != null,
        IsNumeric = IsNumericType(type),
        IsBoolean = IsBooleanType(type),
      };
      
      // Primitives
      if (argument.IsPrimitive)
      {
        return argument;
      }

      // Objects
      foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
      {
        argument.Components.Add(DescribeType(prop.Name, prop.PropertyType));
      }

      return argument;
    }

    public static bool IsNumericType(Type type)
    {
      return type == typeof(byte) || type == typeof(sbyte) ||
             type == typeof(short) || type == typeof(ushort) ||
             type == typeof(int) || type == typeof(uint) ||
             type == typeof(long) || type == typeof(ulong) ||
             type == typeof(float) || type == typeof(double) ||
             type == typeof(decimal);
    }

    public static bool IsBooleanType(Type type)
    {
      return type == typeof(bool);
    }
  }
}
