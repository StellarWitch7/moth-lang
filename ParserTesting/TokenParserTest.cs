using LanguageParser;
using LanguageParser.AST;
using LanguageParser.Tokens;
using System.Numerics;

namespace ParserTesting;

[TestClass]
public class TokenParserTest
{
    [TestMethod]
    public void Assignment()
    {
        var expected = new BinaryOperationNode(new VariableRefNode("catHeight", new ClassRefNode(true)),
            new BinaryOperationNode(new VariableRefNode("dogHeight", new ClassRefNode(true)),
            new BinaryOperationNode(new VariableRefNode("catHeight", new ClassRefNode(true)),
            new ConstantNode(new BigInteger(3)),
            OperationType.Multiplication),
            OperationType.Addition),
            OperationType.Assignment);
        var actual = TokenParser.ProcessExpression(new ParseContext(Tokenizer
            .Tokenize("self.catHeight = self.dogHeight * (self.catHeight + 3);")), null);

        Console.WriteLine("Expected output:");
        Console.WriteLine(expected.GetDebugString("   "));
        Console.WriteLine("Actual output:");
        Console.WriteLine(actual.GetDebugString("   "));

        Assert.AreEqual(expected.GetDebugString(), actual.GetDebugString());
    }

    [TestMethod]
    public void IfStatement()
    {
        //var expected = new StatementListNode
        //    (
        //    new List<StatementNode>
        //    {

        //    }
        var actual = TokenParser.ProcessStatementList(new ParseContext(Tokenizer
            .Tokenize("whether 4 > 1 {\r\n\t  ring Toot.GetGayness(5).ToString();\r\n\t}}")));

        Console.WriteLine("Expected output:");
        //Console.WriteLine(expected.GetDebugString("   "));
        Console.WriteLine("Actual output:");
        Console.WriteLine(actual.GetDebugString("   "));

        //Assert.AreEqual(expected.GetDebugString(), actual.GetDebugString());
    }
}