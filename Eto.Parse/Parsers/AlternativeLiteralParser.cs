using Eto.Parse;
using Eto.Parse.Scanners;
using System;
using System.Collections.Generic;
using System.Linq;

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
		}

		public AlternativeLiteralParser()
		{
			Separator = DefaultSeparator;
		}

		public AlternativeLiteralParser(string tokenId, TreeScanContainer treeScanContainer)
		{
			TokenId = tokenId;
			TreeScanContainer = treeScanContainer;
			Separator = DefaultSeparator;
		}

		protected override void InnerInitialize(ParserInitializeArgs args)
		{
			base.InnerInitialize(args);
			CaseSensitive ??= args.Grammar.CaseSensitive;
		}

		protected override int InnerParse(ParseArgs args)
		{
			var pos = args.Scanner.Position;
			if (!args.Scanner.FindInTree(TreeScanContainer, out var matchedValue, out var nextTokenName))
			{
				return -1;
			}

			if (!nextTokenName.Any())
			{
				// No continuation
				return matchedValue.Length;
			}

			var sepMatch = Separator.Parse(args);
			if (sepMatch < 0)
			{
				// failed
				args.Scanner.Position = pos;
				return -1;
			}

			int continuationMatch = ParseContinuation(nextTokenName.Select(x => ParserLookup[x]).ToArray(), args);
			if (continuationMatch == -1)
			{
				// failed
				args.Scanner.Position = pos;
				return -1;
			}
			return matchedValue.Length + sepMatch + continuationMatch;
		}

		// Copy-Paste from AlternativeParser
		private static int ParseContinuation(Parser[] items, ParseArgs args)
		{
			var count = items.Length;
			args.Push();
			for (int i = 0; i < count; i++)
			{
				var parser = items[i];
				if (parser != null)
				{
					var match = parser.Parse(args);
					if (match < 0)
					{
						args.ClearMatches();
					}
					else
					{
						args.PopSuccess();
						return match;
					}
				}
				else
				{
					args.PopFailed();
					return 0;
				}
			}
			args.PopFailed();

			return -1;
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
