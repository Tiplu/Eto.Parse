using System;
using Eto.Parse;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Eto.Parse.Parsers
{
	public class AlternativeParser : ListParser
	{
		private bool _exhaustiveSearch = false;

		public void EnableExhaustiveSearch() => _exhaustiveSearch = true;

		public static Parser ExcludeNull(params Parser[] parsers)
		{
			return ExcludeNull((IEnumerable<Parser>)parsers);
		}

		public static Parser ExcludeNull(IEnumerable<Parser> parsers)
		{
			var p = parsers.Where(r => r != null).ToArray();
			return p.Length == 1 ? p[0] : new AlternativeParser(p);
		}

		protected AlternativeParser(AlternativeParser other, ParserCloneArgs chain)
			: base(other, chain)
		{
		}

		public AlternativeParser()
		{
		}

		public AlternativeParser(IEnumerable<Parser> sequence)
			: base(sequence)
		{
		}

		public AlternativeParser(params Parser[] sequence)
			: base(sequence)
		{
		}

		public override string GetErrorMessage(ParserErrorArgs args)
		{
			if (args.Detailed && args.Push(this))
			{
				var sb = new StringBuilder();
				for (int i = 0; i < Items.Count; i++)
				{
					Parser item = Items[i];
					if (sb.Length > 0)
						sb.Append(" | ");
					sb.Append(item != null ? item.GetErrorMessage(args) : "null");
				}
				sb.Insert(0, "(");
				sb.Append(")");
				return sb.ToString();
			}
			return DescriptiveName;
		}

		protected int ExhaustiveInnerParse(ParseArgs args)
		{
			int bestMatch = -1;
			int bestIndex = -1;
			int pos = args.Scanner.Position;
			for (int i = 0; i < Items.Count; i++)
			{
				args.Scanner.Position = pos;

				var match = Items[i]?.Parse(args) ?? 0;
				if (match > bestMatch)
				{
					bestMatch = match;
					bestIndex = i;
				}
				args.ClearMatches();
			}

			if (bestMatch == -1) { return -1; }

			if (!HasNamedChildren)
			{
				args.Scanner.Position = pos + bestMatch;
				return bestMatch;
			}

			args.Scanner.Position = pos;
			args.Push();
			var parser = Items[bestIndex];
			if (parser != null)
			{
				var match = Items[bestIndex].Parse(args);
				if (match < 0)
				{
					args.ClearMatches();
					args.PopFailed();
					return -1;
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

		protected override int InnerParse(ParseArgs args)
		{
			if (_exhaustiveSearch)
			{
				return ExhaustiveInnerParse(args);
			}

			var count = Items.Count;
			if (HasNamedChildren)
			{
				args.Push();
				for (int i = 0; i < count; i++)
				{
					var parser = Items[i];
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
			}
			else
			{
				for (int i = 0; i < count; i++)
				{
					var parser = Items[i];
					if (parser != null)
					{
						var match = parser.Parse(args);
						if (match >= 0)
							return match;
					}
					else
					{
						return 0;
					}
				}
			}
			return -1;
		}

		public override Parser Clone(ParserCloneArgs args)
		{
			return new AlternativeParser(this, args);
		}

		public override bool IsLeftRecursive(ParserContainsArgs args)
		{
			if (base.IsLeftRecursive(args))
				return true;
			if (args.Push(this))
			{
				for (int i = 0; i < Items.Count; i++)
				{
					Parser item = Items[i];
					if (item != null && item.IsLeftRecursive(args))
					{
						args.Pop();
						return true;
					}
				}
				args.Pop();
			}
			return false;
		}
	}
}
