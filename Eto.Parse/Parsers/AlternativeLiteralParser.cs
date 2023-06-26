using Eto.Parse;
using Eto.Parse.Scanners;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Eto.Parse.Parsers
{
	public class AlternativeLiteralParser : Parser
	{
		public bool? CaseSensitive { get; set; }

		public Parser Separator { get; set; }

		public string TokenId { get; }
		public TreeScanContainer TreeScanContainer { get; }

		public Dictionary<string, Parser> ParserLookup { get; }

		public override string DescriptiveName
		{
			get { return string.Format("Literal: '{0}'", TokenId); }
		}

		protected AlternativeLiteralParser(AlternativeLiteralParser other, ParserCloneArgs chain)
			: base(other, chain)
		{
			CaseSensitive = other.CaseSensitive;
			TokenId = other.TokenId;
			TreeScanContainer = other.TreeScanContainer;
			Separator = other.Separator;
			ParserLookup = other.ParserLookup;
		}

		public AlternativeLiteralParser()
		{
			Separator = DefaultSeparator;
		}

		public AlternativeLiteralParser(string tokenId, TreeScanContainer treeScanContainer, Dictionary<string, Parser> parserLookup)
		{
			name = tokenId;
			TokenId = tokenId;
			TreeScanContainer = treeScanContainer;
			Separator = DefaultSeparator;
			ParserLookup = parserLookup;
		}

		protected override void InnerInitialize(ParserInitializeArgs args)
		{
			base.InnerInitialize(args);
			CaseSensitive ??= args.Grammar.CaseSensitive;

			foreach(var continuation in TreeScanContainer.SortedEntries.SelectMany(x => x.NextTokens))
			{
				if(ParserLookup.TryGetValue(continuation, out var parser))
				{
					parser.Initialize(args);
				}
			}
		}

		protected override int InnerParse(ParseArgs args)
		{
			args.Push();
			var pos = args.Scanner.Position;
			if (!args.Scanner.FindInTree(TreeScanContainer, out var matchedValue, out var nextTokenName))
			{
				args.PopFailed();
				if (AddError)
				{
					args.AddError(this);
					return -1;
				}
				args.SetChildError();
				return -1;
			}

			args.PopMatch(this, pos, matchedValue.Length);

			if (!nextTokenName.Any())
			{
				// No continuation
				return matchedValue.Length;
			}

			// progress through the seperators
			var sepMatch = Separator.Parse(args);

			// parse the continuation
			foreach (var nextParser in nextTokenName.Select(x => ParserLookup[x]))
			{
				int continuationMatch = ParseContinuation(nextParser, args);
				if (continuationMatch == -1)
				{
					// failed
					args.Scanner.Position = pos;
					return -1;
				}

				var continuationIndex = matchedValue.Length + sepMatch;
				return continuationIndex + continuationMatch;
			}

			return matchedValue.Length;
		}

		// Copy-Paste from AlternativeParser
		private static int ParseContinuation(Parser parser, ParseArgs args)
		{
			if (parser != null)
			{
				var match = parser.Parse(args);
				if (match < 0)
				{
					return -1;
				}
				else
				{
					return match;
				}
			}
			else
			{
				return 0;
			}
		}

		public override Parser Clone(ParserCloneArgs args)
		{
			return new AlternativeLiteralParser(this, args);
		}

		public override bool Equals(object obj)
		{
			if (obj is AlternativeLiteralParser lt)
				return lt.TokenId == TokenId;
			return false;
		}

		public override int GetHashCode()
		{
			return TokenId.GetHashCode();
		}

		public override string ToString()
		{
			return $"Alternative: {TokenId}";
		}
	}
}
