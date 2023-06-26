﻿namespace LanguageParser.AST;

internal class BinaryOperationNode : ExpressionNode
{
	public ExpressionNode Left { get; set; }
	public ExpressionNode Right { get; set; }
    public OperationType Type { get; }

	public BinaryOperationNode(ExpressionNode left, ExpressionNode right, OperationType type)
	{
		Left = left;
		Right = right;
        Type = type;
	}
}

public enum OperationType
{
    Addition,
    Subtraction,
    Multiplication,
    Division,
    Exponential,
    LessThan,
    LessThanOrEqual,
    LargerThan,
    LargerThanOrEqual,
    Equal,
    NotEqual,
    And,
    Or,
    NotAnd
}