﻿using Moth.AST.Node;
using Moth.LLVM;
using Moth.LLVM.Data;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Type = Moth.LLVM.Data.Type;

namespace Moth;

public static class Utils
{
    public static string ExpandOpName(string op)
    {
        return $"{Reserved.Operator}{{{op}}}";
    }

    public static string OpTypeToString(OperationType opType)
    {
        return opType switch
        {
            OperationType.Addition => "+",
            OperationType.Subtraction => "-",
            OperationType.Multiplication => "*",
            OperationType.Division => "/",
            OperationType.Exponential => "^",
            OperationType.Modulus => "%",
            OperationType.LesserThan => "<",
            OperationType.GreaterThan => ">",
            OperationType.LesserThanOrEqual => "<=",
            OperationType.GreaterThanOrEqual => ">=",
            OperationType.Equal => "==",
            //OperationType.Range => "..",
            
            _ => throw new NotImplementedException($"Unsupported operation type: \"{opType}\"")
        };
    }
    
    public static LLVMCallConv StringToCallConv(string str)
    {
        return str switch
        {
            "cdecl" => LLVMCallConv.LLVMCCallConv,
            _ => throw new Exception($"Invalid calling convention: \"{str}\".")
        };
    }
    
    public static OS StringToOS(string str)
    {
        return str switch
        {
            Reserved.Windows => OS.Windows,
            Reserved.Linux => OS.Linux,
            Reserved.MacOS => OS.MacOS,
            _ => throw new Exception($"Invalid OS: \"{str}\".")
        };
    }

    public static OS GetOS()
    {
        if (OperatingSystem.IsWindows()) return OS.Windows;
        if (OperatingSystem.IsLinux()) return OS.Linux;
        if (OperatingSystem.IsMacOS()) return OS.MacOS;
        throw new PlatformNotSupportedException();
    }

    public static bool IsOS(OS os)
    {
        return os switch
        {
            OS.Windows => OperatingSystem.IsWindows(),
            OS.Linux => OperatingSystem.IsLinux(),
            OS.MacOS => OperatingSystem.IsMacOS(),
            _ => throw new Exception($"Cannot verify that the current OS is \"{os}\".")
        };
    }
    
    public static byte[] Unescape(string original)
    {
        if (original.Length == 0)
        {
            return new byte[0];
        }

        uint index = 0;
        var bytes = new List<byte>();

        while (index < original.Length)
        {
            var ch = original[(int)index];

            if (ch == '\\')
            {
                index++;
                var hex1 = original[(int)index];

                if (hex1 == '\\')
                {
                    bytes.Add((byte) hex1);
                    index++;
                }
                else
                {
                    index++;
                    var hex2 = original[(int)index];

                    var byte1 = hex1 switch
                    {
                        >= '0' and <= '9' => (byte) hex1 - (byte) '0',
                        >= 'a' and <= 'f' => (byte) hex1 - (byte) 'a' + 10,
                        >= 'A' and <= 'F' => (byte) hex1 - (byte) 'A' + 10,
                        _ => throw new ArgumentOutOfRangeException(nameof(hex1)),
                    };
            
                    var byte2 = hex2 switch
                    {
                        >= '0' and <= '9' => (byte) hex2 - (byte) '0',
                        >= 'a' and <= 'f' => (byte) hex2 - (byte) 'a' + 10,
                        >= 'A' and <= 'F' => (byte) hex2 - (byte) 'A' + 10,
                        _ => throw new ArgumentOutOfRangeException(nameof(hex2)),
                    };

                    bytes.Add((byte) ((byte1 << 4) | byte2));
                    index++;
                }
            }
            else
            {
                bytes.Add((byte) ch);
                index++;
            }
        }

        return bytes.ToArray();
    }
}

public static class ListExtensions
{
    public static ReadOnlySpan<T> AsReadonlySpan<T>(this List<T> list)
    {
        Span<T> span = CollectionsMarshal.AsSpan(list);
        return span[..list.Count];
    }

    public static List<LLVMTypeRef> AsLLVMTypes(this List<InternalType> types)
    {
        var result = new List<LLVMTypeRef>();

        foreach (InternalType type in types)
        {
            result.Add(type.LLVMType);
        }

        return result;
    }
}

public static class ArrayExtensions
{
    public static RESULT[] ExecuteOverAll<ORIGINAL, RESULT>(this ORIGINAL[] original, Func<ORIGINAL, RESULT> func)
    {
        var result = new List<RESULT>();

        foreach (var val in original)
        {
            result.Add(func(val));
        }

        return result.ToArray();
    }

    public static Value[] CompileToValues(this ExpressionNode[] expressionNodes, LLVMCompiler compiler, Scope scope)
    {
        var result = new List<Value>();

        foreach (var expr in expressionNodes)
        {
            result.Add(compiler.CompileExpression(scope, expr));
        }

        return result.ToArray();
    }
    
    public static LLVMValueRef[] AsLLVMValues(this byte[] bytes)
    {
        var result = new LLVMValueRef[bytes.Length];
        uint index = 0;

        foreach (var @byte in bytes)
        {
            result[index] = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int8, @byte);
            index++;
        }

        return result;
    }

    public static Value[] ImplicitConvertAll(this Value[] values, LLVMCompiler compiler, InternalType target)
    {
        var result = new Value[values.Length];
        uint index = 0;

        foreach (Value value in values)
        {
            result[index] = value.ImplicitConvertTo(compiler, target);
            index++;
        }

        return result;
    }
    
    public static LLVMValueRef[] AsLLVMValues(this Value[] values)
    {
        var result = new LLVMValueRef[values.Length];
        uint index = 0;

        foreach (Value value in values)
        {
            result[index] = value.LLVMValue;
            index++;
        }

        return result;
    }

    public static LLVMTypeRef[] AsLLVMTypes(this Field[] fields)
    {
        var types = new List<InternalType>();

        foreach (var field in fields)
        {
            types.Add(field.InternalType);
        }

        return types.ToArray().AsLLVMTypes();
    }
    
    public static LLVMTypeRef[] AsLLVMTypes(this InternalType[] types)
    {
        var result = new LLVMTypeRef[types.Length];
        uint index = 0;

        foreach (InternalType type in types)
        {
            result[index] = type.LLVMType;
            index++;
        }

        return result;
    }

    public static int GetHashes(this InternalType[] types)
    {
        int hash = 3;

        foreach (InternalType type in types)
        {
            hash *= 31 + type.GetHashCode();
        }

        return hash;
    }
    
    public static ulong[] ToULong(this byte[] bytes)
    {
        var values = new ulong[bytes.Length / 8];
        for (var i = 0; i < values.Length; i++)
            values[i] = Unsafe.ReadUnaligned<ulong>(ref bytes[i * 8]);
        return values;
    }
    
    public static bool TryGetNamespace(this Namespace[] imports, string name, out Namespace nmspace)
    {
        nmspace = null;
        
        foreach (var import in imports)
        {
            if (import.Namespaces.TryGetValue(name, out nmspace))
            {
                break;
            }
            else if (import.Name == name)
            {
                nmspace = import;
                break;
            }
        }

        if (nmspace != null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public static bool TryGetFunction(this Namespace[] imports, string name, IReadOnlyList<InternalType> paramTypes, out Function func)
    {
        func = null;
        
        foreach (var import in imports)
        {
            if (import.Functions.TryGetValue(name, out OverloadList overloads)
                && overloads.TryGet(paramTypes, out func))
            {
                if (func is DefinedFunction defFunc && defFunc.Privacy == PrivacyType.Priv)
                {
                    func = null;
                }
                else
                {
                    break;
                }
            }
        }

        if (func != null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public static bool TryGetType(this Namespace[] imports, string name, out Type type)
    {
        type = null;
        
        foreach (var import in imports)
        {
            if (import.Types.TryGetValue(name, out type))
            {
                if (type.Privacy == PrivacyType.Priv)
                {
                    type = null;
                }
                else
                {
                    break;
                }
            }
        }

        if (type != null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    
    public static bool TryGetTrait(this Namespace[] imports, string name, out Trait trait)
    {
        trait = null;
        
        foreach (var import in imports)
        {
            if (import.Traits.TryGetValue(name, out trait))
            {
                if (trait.Privacy == PrivacyType.Priv)
                {
                    trait = null;
                }
                else
                {
                    break;
                }
            }
        }

        if (trait != null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    
    public static bool TryGetTemplate(this Namespace[] imports, string name, out Template template)
    {
        template = null;
        
        foreach (var import in imports)
        {
            if (import.Templates.TryGetValue(name, out template))
            {
                if (template.Privacy == PrivacyType.Priv)
                {
                    template = null;
                }
                else
                {
                    break;
                }
            }
        }

        if (template != null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}