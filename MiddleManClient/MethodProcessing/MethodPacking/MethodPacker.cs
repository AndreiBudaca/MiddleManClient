using MiddleManClient.MethodProcessing.Models;
using System.Text;

namespace MiddleManClient.MethodProcessing.MethodPacking
{
  public class MethodPacker : IMethodPacker
  {
    private static readonly List<byte> varStringTerminator = [0x00];

    public byte[]? GetDiff(byte[]? serverData, List<WebSocketClientMethod> knownMethods)
    {
      var packedMethods = Pack(knownMethods);
      var signature = packedMethods.Skip(2).Take(32).ToArray();

      if (serverData != null && serverData.SequenceEqual(signature)) return null;
      return packedMethods;
    }

    private static byte[] Pack(List<WebSocketClientMethod> methods)
    {
      if (methods.Count > byte.MaxValue)
        throw new ArgumentOutOfRangeException(nameof(methods), "Too many methods to pack");

      var typeDictionary = new Dictionary<string, uint>();

      IEnumerable<byte> methodsBytes = [(byte)methods.Count];

      // Append rest of method data
      foreach (var method in methods)
      {                                                                             
        methodsBytes = methodsBytes.Concat(PackMethod(method, ref typeDictionary, (uint)methodsBytes.Count() + 2 + 32));
      }
      var signature = System.Security.Cryptography.SHA256.HashData(methodsBytes.ToArray());

      //                       VERSION (1B), SET (0x01)                 
      IEnumerable<byte> mainHeader = [0x00, 0x01];

      return [.. mainHeader.Concat(signature).Concat(methodsBytes)];
    }

    private static IEnumerable<byte> PackMethod(WebSocketClientMethod method, ref Dictionary<string, uint> typeDictionary, uint offset)
    {

      if (method.Arguments.Count > 0x7F)
        throw new ArgumentOutOfRangeException(nameof(method.Arguments), "Too many method arguments to pack");

      //                                        NAME (variable)                     NULL TERMINATOR (1B)
      IEnumerable<byte> methodHeader = Encoding.ASCII.GetBytes(method.Name).Concat(varStringTerminator);

      if (method.Returns is not null)
      {
        methodHeader = methodHeader.Concat([(byte)(0x80 | method.Arguments.Count)]);
      }
      else
      {
        methodHeader = methodHeader.Concat([(byte)(0x7F & method.Arguments.Count)]);
      }

      foreach (var argument in method.Arguments)
      {
        methodHeader = methodHeader.Concat(PackArgument(argument, ref typeDictionary, offset + (uint)methodHeader.Count()));
      }

      if (method.Returns is not null)
      {
        methodHeader = methodHeader.Concat(PackArgument(method.Returns, ref typeDictionary, offset + (uint)methodHeader.Count()));
      }

      return methodHeader;
    }

    private static IEnumerable<byte> PackArgument(WebSocketClientMethodArgument argument, ref Dictionary<string, uint> typeDictionary, uint offset)
    {
      if (argument.Name?.Length > 0x7F)
        throw new ArgumentOutOfRangeException(nameof(argument.Name), "Argument name too long to pack");

      if (argument.Components.Count > byte.MaxValue)
        throw new ArgumentOutOfRangeException(nameof(argument.Components), "Too many argument components to pack");

      //                                        NAME (variable)                               NULL TERMINATOR (1B)
      IEnumerable<byte> argumentHeader = Encoding.ASCII.GetBytes(argument.Name ?? string.Empty).Concat(varStringTerminator);

      var isKnownConplexType = argument.Type is not null && typeDictionary.ContainsKey(argument.Type) && argument.Components.Count > 0;

      // Flags
      byte flags = isKnownConplexType ? (byte)0x01 : (byte)0x00; 
      if (argument.IsArray) flags |= 0x02;
      if (argument.IsNullable) flags |= 0x04;
      if (argument.IsNumeric) flags |= 0x08;
      if (argument.IsBoolean) flags |= 0x10;

      argumentHeader = argumentHeader.Concat([flags]);

      // Complex type already exists, use existing index
      if (isKnownConplexType)
      {
        return argumentHeader
        //           TYPE OFFSET (4B)
        .Concat(BitConverter.GetBytes(typeDictionary[argument.Type!]));
      }

      // New complex type, save definition offset
      if (argument.Type is not null && argument.Components.Count > 0)
      {
        typeDictionary[argument.Type] = offset + (uint)argumentHeader.Count();
      }

      //                                            COMPONENTS COUNT (1B)
      argumentHeader = argumentHeader.Concat([(byte)argument.Components.Count]);

      foreach (var component in argument.Components)
      {
        argumentHeader = argumentHeader.Concat(PackArgument(component, ref typeDictionary, offset + (uint)argumentHeader.Count()));
      }

      return argumentHeader;
    }
  }
}
