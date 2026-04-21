using System.Linq;
using CodeYourself.Commands;
using CodeYourself.Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeYourself.Tests.Parsing
{
    [TestClass]
    public sealed class CommandParserTests
    {
        [TestMethod]
        public void MoveRight_3_ExpandsToThreeCommands_WithSameLineIndex()
        {
            var parser = new CommandParser();
            var result = parser.Parse("MOVE RIGHT 3");
            var commands = result.Commands.ToList();

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(3, commands.Count);
            Assert.IsTrue(commands.All(c => c is MoveCommand));
            Assert.IsTrue(commands.All(c => c.LineIndex == 0));
        }

        [TestMethod]
        public void Wait_2_ExpandsToTwoCommands()
        {
            var parser = new CommandParser();
            var result = parser.Parse("WAIT 2");
            var commands = result.Commands.ToList();

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(2, commands.Count);
            Assert.IsTrue(commands.All(c => c is WaitCommand));
        }

        [TestMethod]
        public void JumpRight_3_ExpandsToThreeCommands_WithSameLineIndex()
        {
            var parser = new CommandParser();
            var result = parser.Parse("JUMP RIGHT 3");
            var commands = result.Commands.ToList();

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(3, commands.Count);
            Assert.IsTrue(commands.All(c => c is JumpCommand));
            Assert.IsTrue(commands.All(c => c.LineIndex == 0));
        }

        [TestMethod]
        public void Repeat_2_RepeatsBodyTwice()
        {
            var parser = new CommandParser();
            var text = "REPEAT 2\r\nMOVE RIGHT 1\r\nWAIT 1\r\nEND";
            var result = parser.Parse(text);
            var commands = result.Commands.ToList();

            Assert.IsTrue(result.IsSuccess);
            // (MOVE RIGHT 1 + WAIT 1) * 2
            Assert.AreEqual(4, commands.Count);
            Assert.IsTrue(commands[0] is MoveCommand);
            Assert.IsTrue(commands[1] is WaitCommand);
            Assert.IsTrue(commands[2] is MoveCommand);
            Assert.IsTrue(commands[3] is WaitCommand);
        }

        [TestMethod]
        public void Repeat_AllowsNesting()
        {
            var parser = new CommandParser();
            var text = "REPEAT 2\r\nREPEAT 3\r\nWAIT 1\r\nEND\r\nEND";
            var result = parser.Parse(text);
            var commands = result.Commands.ToList();

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(6, commands.Count);
            Assert.IsTrue(commands.All(c => c is WaitCommand));
        }

        [TestMethod]
        public void EmptyLinesAndComments_AreIgnored()
        {
            var parser = new CommandParser();
            var text = "\r\n# comment\r\n// comment\r\nWAIT 1\r\n";
            var result = parser.Parse(text);
            var commands = result.Commands.ToList();

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(1, commands.Count);
            Assert.AreEqual(3, commands[0].LineIndex);
        }

        [TestMethod]
        public void EndWithoutRepeat_ReturnsError()
        {
            var parser = new CommandParser();
            var result = parser.Parse("END");
            var errors = result.Errors.ToList();

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, result.Commands.Count());
            Assert.AreEqual(1, errors.Count);
            Assert.AreEqual(0, errors[0].LineIndex);
            StringAssert.Contains(errors[0].Message, "END without REPEAT");
        }

        [TestMethod]
        public void Repeat_MissingEnd_ReturnsError()
        {
            var parser = new CommandParser();
            var result = parser.Parse("REPEAT 2\r\nWAIT 1");
            var errors = result.Errors.ToList();

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, result.Commands.Count());
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains(errors[0].Message, "Missing END");
        }

        [TestMethod]
        public void UnknownCommand_ReturnsError()
        {
            var parser = new CommandParser();
            var result = parser.Parse("FLY 1");
            var errors = result.Errors.ToList();

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, result.Commands.Count());
            Assert.AreEqual(1, errors.Count);
            Assert.AreEqual(0, errors[0].LineIndex);
            StringAssert.Contains(errors[0].Message, "Unknown command");
        }

        [TestMethod]
        public void InvalidCount_ReturnsError()
        {
            var parser = new CommandParser();
            var result = parser.Parse("WAIT 0");
            var errors = result.Errors.ToList();

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, result.Commands.Count());
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains(errors[0].Message, "Invalid count");
        }

        [TestMethod]
        public void NullInput_ReturnsSuccessWithNoCommands()
        {
            var parser = new CommandParser();
            var result = parser.Parse(null);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(0, result.Commands.Count());
            Assert.AreEqual(0, result.Errors.Count());
        }

        [TestMethod]
        public void Wait_WithoutCount_DefaultsToOne()
        {
            var parser = new CommandParser();
            var result = parser.Parse("WAIT");
            var commands = result.Commands.ToList();

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(1, commands.Count);
            Assert.IsTrue(commands[0] is WaitCommand);
            Assert.AreEqual(0, commands[0].LineIndex);
        }

        [TestMethod]
        public void Move_MissingDirection_ReturnsError()
        {
            var parser = new CommandParser();
            var result = parser.Parse("MOVE");
            var errors = result.Errors.ToList();

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, result.Commands.Count());
            Assert.AreEqual(1, errors.Count);
            Assert.AreEqual(0, errors[0].LineIndex);
            StringAssert.Contains(errors[0].Message, "MOVE requires direction");
        }

        [TestMethod]
        public void Move_UnknownDirection_ReturnsError()
        {
            var parser = new CommandParser();
            var result = parser.Parse("MOVE UP 1");
            var errors = result.Errors.ToList();

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, result.Commands.Count());
            Assert.AreEqual(1, errors.Count);
            Assert.AreEqual(0, errors[0].LineIndex);
            StringAssert.Contains(errors[0].Message, "Unknown MOVE direction");
        }

        [TestMethod]
        public void Move_InvalidCount_ReturnsError()
        {
            var parser = new CommandParser();
            var result = parser.Parse("MOVE LEFT -2");
            var errors = result.Errors.ToList();

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, result.Commands.Count());
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains(errors[0].Message, "Invalid count");
        }

        [TestMethod]
        public void Parser_IsCaseInsensitive_AndIgnoresExtraWhitespace()
        {
            var parser = new CommandParser();
            var result = parser.Parse("  move\tleft\t2  \r\n  wAiT\t1 ");
            var commands = result.Commands.ToList();

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(3, commands.Count);
            Assert.AreEqual(0, commands[0].LineIndex);
            Assert.AreEqual(0, commands[1].LineIndex);
            Assert.AreEqual(1, commands[2].LineIndex);
        }

        [TestMethod]
        public void MultipleErrors_AreCollected_WithCorrectLineIndices()
        {
            var parser = new CommandParser();
            var result = parser.Parse("WAIT 1\r\nMOVE UP 1\r\nFLY 1\r\nMOVE RIGHT");
            var errors = result.Errors.ToList();
            var commands = result.Commands.ToList();

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(2, commands.Count); // WAIT 1 + MOVE RIGHT (count defaults to 1)
            Assert.AreEqual(2, errors.Count);
            Assert.AreEqual(1, errors[0].LineIndex);
            Assert.AreEqual(2, errors[1].LineIndex);
        }
    }
}

