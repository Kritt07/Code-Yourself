using System;
using System.Collections.Generic;
using System.Linq;
using CodeYourself.Commands;
using CodeYourself.Commands.Base;
using CodeYourself.Models;

namespace CodeYourself.Parsing
{
    public class CommandParser
    {
        private abstract class Node
        {
            public int LineIndex { get; }

            protected Node(int lineIndex)
            {
                LineIndex = lineIndex;
            }

            public abstract void Emit(List<GameCommand> output);
        }

        private class WaitNode : Node
        {
            private readonly int _count;
            public WaitNode(int lineIndex, int count) : base(lineIndex) => _count = count;
            public override void Emit(List<GameCommand> output)
            {
                for (int k = 0; k < _count; k++)
                    output.Add(new WaitCommand(LineIndex));
            }
        }

        private class MoveNode : Node
        {
            private readonly MoveDirection _direction;
            private readonly int _count;

            public MoveNode(int lineIndex, MoveDirection direction, int count) : base(lineIndex)
            {
                _direction = direction;
                _count = count;
            }

            public override void Emit(List<GameCommand> output)
            {
                for (int k = 0; k < _count; k++)
                    output.Add(new MoveCommand(LineIndex, _direction));
            }
        }

        private class JumpNode : Node
        {
            private readonly MoveDirection _direction;
            private readonly int _count;

            public JumpNode(int lineIndex, MoveDirection direction, int count) : base(lineIndex)
            {
                _direction = direction;
                _count = count;
            }

            public override void Emit(List<GameCommand> output)
            {
                for (int stepIndex = 0; stepIndex < _count; stepIndex++)
                    output.Add(new JumpCommand(LineIndex, _direction, stepIndex, _count));
            }
        }

        private class RepeatNode : Node
        {
            private readonly int _count;
            private readonly List<Node> _body;

            public RepeatNode(int lineIndex, int count, List<Node> body) : base(lineIndex)
            {
                _count = count;
                _body = body ?? new List<Node>();
            }

            public override void Emit(List<GameCommand> output)
            {
                for (int i = 0; i < _count; i++)
                {
                    foreach (var node in _body)
                        node.Emit(output);
                }
            }
        }

        public class ParseResult
        {
            public IEnumerable<GameCommand> Commands { get; }
            public IEnumerable<ParseError> Errors { get; }

            public bool IsSuccess => !Errors.Any();

            public ParseResult(IEnumerable<GameCommand> commands, IEnumerable<ParseError> errors)
            {
                Commands = commands ?? Enumerable.Empty<GameCommand>();
                Errors = errors ?? Enumerable.Empty<ParseError>();
            }
        }

        public ParseResult Parse(string text)
        {
            var errors = new List<ParseError>();

            if (text == null) text = string.Empty;

            var lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

            int index = 0;
            var nodes = ParseBlock(lines, ref index, errors, stopOnEnd: false);
            if (nodes == null)
                nodes = new List<Node>();

            var commands = new List<GameCommand>();
            foreach (var node in nodes)
                node.Emit(commands);

            return new ParseResult(commands, errors);
        }

        private static List<Node> ParseBlock(string[] lines, ref int index, List<ParseError> errors, bool stopOnEnd)
        {
            var nodes = new List<Node>();

            for (; index < lines.Length; index++)
            {
                var rawLine = lines[index];
                var line = (rawLine ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(line)
                    || line.StartsWith("#") || line.StartsWith("//"))
                    continue;

                var tokens = line
                    .Split(new[] { ' ', '\t' })
                    .Select(t => t.Trim())
                    .Where(t => t.Length > 0)
                    .ToArray();

                if (tokens.Length == 0)
                    continue;

                var keyword = tokens[0].ToUpperInvariant();

                if (keyword == "END")
                {
                    if (!stopOnEnd)
                    {
                        errors.Add(new ParseError(index, "END without REPEAT"));
                        continue;
                    }

                    return nodes;
                }

                if (keyword == "REPEAT")
                {
                    var repeatLineIndex = index;
                    if (tokens.Length < 2)
                    {
                        errors.Add(new ParseError(index, "REPEAT requires count: REPEAT n"));
                        continue;
                    }

                    if (!int.TryParse(tokens[1], out var repeatCount) || repeatCount <= 0)
                    {
                        errors.Add(new ParseError(index, $"Invalid count: '{tokens[1]}'. Expected positive integer."));
                        continue;
                    }

                    index++; // move to first line inside the block
                    var body = ParseBlock(lines, ref index, errors, stopOnEnd: true);
                    if (body == null)
                        body = new List<Node>();

                    if (index >= lines.Length)
                    {
                        return nodes;
                    }

                    nodes.Add(new RepeatNode(repeatLineIndex, repeatCount, body));
                    continue;
                }

                if (keyword == "WAIT")
                {
                    if (!TryParseWaitCount(tokens, index, errors, out var n))
                        continue;

                    nodes.Add(new WaitNode(index, n));
                    continue;
                }

                if (keyword == "MOVE" || keyword == "JUMP")
                {
                    if (tokens.Length < 2)
                    {
                        errors.Add(new ParseError(index, $"{keyword} requires direction: LEFT or RIGHT"));
                        continue;
                    }

                    if (!TryParseDirection(tokens[1], index, errors, keyword, out var dir))
                        continue;

                    if (!TryParseOptionalCountAfterDirection(tokens, index, errors, out var n))
                        continue;

                    if (keyword == "MOVE")
                        nodes.Add(new MoveNode(index, dir, n));
                    else
                        nodes.Add(new JumpNode(index, dir, n));

                    continue;
                }

                errors.Add(new ParseError(index, $"Unknown command: '{tokens[0]}'"));
            }

            if (stopOnEnd)
                errors.Add(new ParseError(Math.Max(0, lines.Length - 1), "Missing END for REPEAT block"));

            return nodes;
        }

        private static bool TryParseDirection(string token, int lineIndex, List<ParseError> errors, string keyword, out MoveDirection dir)
        {
            dir = MoveDirection.Right;
            var dirToken = (token ?? string.Empty).ToUpperInvariant();

            if (dirToken == "LEFT")
            {
                dir = MoveDirection.Left;
                return true;
            }

            if (dirToken == "RIGHT")
            {
                dir = MoveDirection.Right;
                return true;
            }

            errors.Add(new ParseError(lineIndex, $"Unknown {keyword} direction: '{token}'"));
            return false;
        }

        private static bool TryParseWaitCount(string[] tokens, int lineIndex, List<ParseError> errors, out int n)
        {
            // WAIT [n] (по умолчанию 1)
            n = 1;
            if (tokens.Length == 1)
                return true;

            if (tokens.Length >= 2)
            {
                if (!int.TryParse(tokens[1], out n) || n <= 0)
                {
                    errors.Add(new ParseError(lineIndex, $"Invalid count: '{tokens[1]}'. Expected positive integer."));
                    return false;
                }
            }

            return true;
        }

        private static bool TryParseOptionalCountAfterDirection(string[] tokens, int lineIndex, List<ParseError> errors, out int n)
        {
            // MOVE/JUMP <DIR> [n] (по умолчанию 1)
            n = 1;
            if (tokens.Length < 3)
                return true;

            if (!int.TryParse(tokens[2], out n) || n <= 0)
            {
                errors.Add(new ParseError(lineIndex, $"Invalid count: '{tokens[2]}'. Expected positive integer."));
                return false;
            }

            return true;
        }

        private static bool TryParseCount(string[] tokens, int lineIndex, List<ParseError> errors, out int n)
        {
            n = 1;

            if (tokens.Length >= 3)
            {
                if (!int.TryParse(tokens[2], out n) || n <= 0)
                {
                    errors.Add(new ParseError(lineIndex, $"Invalid count: '{tokens[2]}'. Expected positive integer."));
                    return false;
                }
            }
            else if (tokens.Length == 2)
            {
                // Allow implicit count=1 for WAIT n? WAIT must be 'WAIT n' or 'WAIT'?
                // For MOVE, tokens[1] is direction. For WAIT, tokens[1] might be count.
                // We handle WAIT separately before calling this helper when tokens length is validated.
                if (tokens[0].Equals("WAIT", StringComparison.OrdinalIgnoreCase))
                {
                    if (!int.TryParse(tokens[1], out n) || n <= 0)
                    {
                        errors.Add(new ParseError(lineIndex, $"Invalid count: '{tokens[1]}'. Expected positive integer."));
                        return false;
                    }
                }
            }

            return true;
        }
    }
}

