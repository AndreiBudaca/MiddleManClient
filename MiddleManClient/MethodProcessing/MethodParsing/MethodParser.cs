using MiddleManClient.MethodProcessing.Models;
using System.Reflection;

namespace MiddleManClient.MethodProcessing.MethodParsing
{
  public class MethodParser : IMethodParser
  {
    public WebSocketClientMethod Parse(MethodInfo methodInfo)
    {
      var methodData = new WebSocketClientMethod
      {
        Name = methodInfo.Name,
        Arguments = methodInfo.GetParameters().Select(x => DescribeType(x.Name, x.ParameterType)).ToList(),
        Returns = GetReturnType(methodInfo)
      };

      if (methodData.Arguments.Any(x => x.IsBinary) && methodData.Arguments.Count > 1)
      {
        throw new NotSupportedException("Methods that accept binary data can declare only one parameter");
      }

      return methodData;
    }

    private static WebSocketClientMethodArgument? GetReturnType(MethodInfo methodInfo)
    {
      if (methodInfo.ReturnType == typeof(void))
      {
        return null;
      }

      // Tasks
      if (methodInfo.ReturnType == typeof(Task))
      {
        return null;
      }

      if (methodInfo.ReturnType.IsGenericType && methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
      {
        return DescribeType(null, methodInfo.ReturnType.GetGenericArguments()[0]);
      }

      return DescribeType(null, methodInfo.ReturnType);
    }

    private static WebSocketClientMethodArgument DescribeType(string? name, Type type, int depth = 0)
    {
      // Check for binary methods
      if (depth == 0 && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
      {
        var elementType = type.GetGenericArguments()[0];
        if (elementType.IsArray && elementType.GetElementType() == typeof(byte))
        {
          return new WebSocketClientMethodArgument
          {
            Name = name,
            Type = type.FullName,
            IsBinary = true,
          };
        }
      }

      if (depth > 32) 
      {
        throw new NotSupportedException("Type depth exceeds maximum allowed depth of 32.");
      }

      if (type.IsEnum || type.IsAbstract || type.IsInterface)
      {
        throw new NotSupportedException($"Type {type.Name} is not supported in method arguments or return types.");
      }

      if (type == typeof(object))
      {
        throw new NotSupportedException("Object is not supported in method arguments or return types.");
      }

      // Arrays
      if (type.IsArray)
      {
        var elementType = type.GetElementType()!;
        var elementDescription = DescribeType(name, elementType, depth + 1);
        elementDescription.IsArray = true;

        return elementDescription;
      }

      // Lists
      if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
      {
        var elementType = type.GetGenericArguments()[0];
        var elementDescription = DescribeType(name, elementType, depth + 1);
        elementDescription.IsArray = true;

        return elementDescription;
      }

      var argument = new WebSocketClientMethodArgument
      {
        Name = name,
        Type = type.FullName,
        IsArray = type.IsArray,
        IsNullable = Nullable.GetUnderlyingType(type) != null,
        IsNumeric = IsNumericType(type),
        IsBoolean = IsBooleanType(type),
      };
      
      // Primitives
      if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
      {
        return argument;
      }

      // Objects
      foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
      {
        argument.Components.Add(DescribeType(prop.Name, prop.PropertyType, depth + 1));
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
