﻿using LLVMSharp.Interop;
using Moth.AST;
using Moth.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM;

public static class LLVMCodeGenerator
{
    public static void ConvertScript(CompilerContext compiler, ScriptAST script)
    {
        foreach (var @class in script.ClassNodes)
        {
            compiler.Classes.Add(@class.Name, ConvertClass(compiler, @class));
        }
    }

    public static Class ConvertClass(CompilerContext compiler, ClassNode @class)
    {
        LLVMTypeRef newStruct = compiler.Context.CreateNamedStruct(@class.Name);

        List<LLVMTypeRef> types = new List<LLVMTypeRef>();

        foreach (FieldNode field in @class.Scope.Statements.OfType<FieldNode>())
        {
            types.Add(DefToLLVMType(field.Type));
        }

        newStruct.StructSetBody(types.ToArray(), false);
        var newClass = new Class(newStruct, @class.Privacy);
        

        foreach (MethodDefNode methodDef in @class.Scope.Statements.OfType<MethodDefNode>())
        {
            newClass.Functions.Add(methodDef.Name, ConvertMethod(compiler, newClass, methodDef));
        }
    }

    public static Function ConvertMethod(CompilerContext compiler, Class @class, MethodDefNode methodDef)
    {
        List<LLVMTypeRef> paramTypes = new List<LLVMTypeRef> { LLVMTypeRef.CreatePointer(@class.LLVMClass, 0) };

        foreach (ParameterNode param in methodDef.Params)
        {
            paramTypes.Add(DefToLLVMType(param.Type));
        }

        var funcType = LLVMTypeRef.CreateFunction(DefToLLVMType(methodDef.ReturnType), paramTypes.ToArray());
        LLVMValueRef func = compiler.Module.AddFunction(methodDef.Name, funcType);
        @class.Functions.Add(methodDef.Name, new Function(func, methodDef.Privacy));
    }

    public static Block ConvertBlock(CompilerContext compiler, Function func, ScopeNode scope)
    {
        foreach (StatementNode statement in scope.Statements)
        {
            if (statement is IfNode @if)
            {
                LLVMValueRef condition = ConvertExpression(compiler, @if.Condition); //I don't know what the hell I'm doing

                var then = compiler.Context.AppendBasicBlock(func.LLVMFunc, "then");
                var @else = compiler.Context.AppendBasicBlock(func.LLVMFunc, "else");
                var @continue = compiler.Context.AppendBasicBlock(func.LLVMFunc, "continue");

                compiler.Builder.BuildCondBr(condition, then, @else);

                //then
                {
                    compiler.Builder.PositionAtEnd(then);
                    ConvertBlock(compiler, func, @if.Then);
                    compiler.Builder.BuildBr(@continue);
                }

                //else
                {
                    compiler.Builder.PositionAtEnd(@else);

                    if (@if.Else != null)
                    {
                        ConvertBlock(compiler, func, @if.Else);
                    }

                    compiler.Builder.BuildBr(@continue);
                }

                //continue
                {
                    compiler.Builder.PositionAtEnd(@continue);
                }
            }
            else if (statement is BinaryOperationNode binaryOp)
            {
                if (binaryOp.Type != OperationType.Assignment)
                {
                    throw new Exception();
                }
            }
        }
    }

    public static LLVMTypeRef DefToLLVMType(DefinitionType definitionType)
    {
        switch (definitionType)
        {
            case DefinitionType.Void:
                return LLVMTypeRef.Void;
            case DefinitionType.Int32:
                return LLVMTypeRef.Int32;
            case DefinitionType.Float32:
                return LLVMTypeRef.Float;
            case DefinitionType.Bool:
                return LLVMTypeRef.Int1;
            case DefinitionType.String:
                return LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0); //compiler.Context.GetConstString() for literal strings
            default:
                throw new NotImplementedException(); //can't handle other types D:
        }
    }
}
