﻿namespace Moth.AST.Node;

public class AttributeNode : ASTNode
{
    public string Name { get; set; }
    public List<ExpressionNode> Arguments { get; set; }

    public AttributeNode(string name, List<ExpressionNode> arguments)
    {
        Name = name;
        Arguments = arguments;
    }
}
