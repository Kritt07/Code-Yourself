namespace CodeYourself.Parsing
{
    public sealed class ParseError
    {
        public int LineIndex { get; }
        public string Message { get; }

        public ParseError(int lineIndex, string message)
        {
            LineIndex = lineIndex;
            Message = message ?? "Unknown parse error";
        }
    }
}

