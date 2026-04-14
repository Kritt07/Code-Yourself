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
            var commands = new List<GameCommand>();
            var errors = new List<ParseError>();

            if (text == null) text = string.Empty;

            var lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var rawLine = lines[i];
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

                var keyword = tokens[0].ToUpper();

                if (keyword == "WAIT")
                {
                    if (!TryParseCount(tokens, i, errors, out var n))
                        continue;

                    for (int k = 0; k < n; k++)
                        commands.Add(new WaitCommand(i));

                    continue;
                }

                if (keyword == "MOVE")
                {
                    if (tokens.Length < 2)
                    {
                        errors.Add(new ParseError(i, "MOVE requires direction: LEFT or RIGHT"));
                        continue;
                    }

                    var dirToken = tokens[1].ToUpperInvariant();
                    MoveDirection dir;

                    if (dirToken == "LEFT")
                        dir = MoveDirection.Left;
                    else if (dirToken == "RIGHT")
                        dir = MoveDirection.Right;
                    else
                    {
                        errors.Add(new ParseError(i, $"Unknown MOVE direction: '{tokens[1]}'"));
                        continue;
                    }

                    if (!TryParseCount(tokens, i, errors, out var n))
                        continue;

                    for (int k = 0; k < n; k++)
                        commands.Add(new MoveCommand(i, dir));

                    continue;
                }

                errors.Add(new ParseError(i, $"Unknown command: '{tokens[0]}'"));
            }

            return new ParseResult(commands, errors);
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

