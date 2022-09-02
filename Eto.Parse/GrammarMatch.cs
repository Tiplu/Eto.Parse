using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using static Eto.Parse.ParseArgs;
using Eto.Parse.Parsers;

namespace Eto.Parse
{
	public class GrammarMatch : Match
	{
		readonly IEnumerable<(Parser P, bool IsOptional)> errors;
		readonly IEnumerable<(Parser P, bool IsOptional)> nextParser;

		public int ErrorIndex { get; private set; }
		public int NextMatchIndex { get; private set; }

		public int ChildErrorIndex { get; private set; }

		public int ErrorLine => ErrorIndex >= 0 ? Scanner.LineAtIndex(ErrorIndex) : -1;

		public int ChildErrorLine => ChildErrorIndex >= 0 ? Scanner.LineAtIndex(ChildErrorIndex) : -1;

		public IEnumerable<(Parser P, bool IsOptional)> Errors => errors ?? Enumerable.Empty<(Parser P, bool IsOptional)>();
		public IEnumerable<(Parser P, bool IsOptional)> NextParser => nextParser ?? Enumerable.Empty<(Parser P, bool IsOptional)>();

		public GrammarMatch(Grammar grammar, Scanner scanner, int index, int length, MatchCollection matches, int errorIndex, int nextMatchIndex, int childErrorIndex, IEnumerable<(Parser P, bool IsOptional)> errors, IEnumerable<(Parser P, bool IsOptional)> nextParser)
			: base(grammar.Name, grammar, scanner, index, length, matches)
		{
			this.errors = errors;
			this.nextParser = nextParser;
			this.ErrorIndex = errorIndex;
			this.NextMatchIndex = nextMatchIndex;
			this.ChildErrorIndex = childErrorIndex;
		}

		public string GetContext(int index, int count, string indicator = ">>>")
		{
			if (index < 0)
				throw new ArgumentOutOfRangeException("index", "Index must be greater or equal to zero");
			var before = Scanner.Substring(Math.Max(0, index - count), Math.Min(index, count));
			var after = Scanner.Substring(index, count);
			return before + indicator + after;
		}

		public string ErrorMessage
		{
			get { return GetErrorMessage(true); }
		}

		public string GetErrorMessage(bool detailed = false)
		{
			var sb = new StringBuilder();
			if (ErrorIndex >= 0)
				sb.AppendLine(string.Format("Index={0}, Line={1}, Context=\"{2}\"", ErrorIndex, Scanner.LineAtIndex(ErrorIndex), GetContext(ErrorIndex, 10)));
			if (ChildErrorIndex >= 0 && ChildErrorIndex != ErrorIndex)
				sb.AppendLine(string.Format("ChildIndex={0}, Line={1}, Context=\"{2}\"", ChildErrorIndex, Scanner.LineAtIndex(ChildErrorIndex), GetContext(ChildErrorIndex, 10)));
			var messages = string.Join("\n", Errors.Select(r => r.P.GetErrorMessage(detailed)));
			if (!string.IsNullOrEmpty(messages))
			{
				sb.AppendLine("Expected:");
				sb.AppendLine(messages);
			}
			return sb.ToString();
		}
	}
}

