using Eto.Parse;
using System;

namespace Eto.Parse.Parsers
{
	public class LiteralTerminal : Parser
	{
		bool caseSensitive;
		public bool? CaseSensitive { get; set; }

		public string Value { get; set; }
		public Guid? TreeIndex { get; set; }

		public override string DescriptiveName
		{
			get { return string.Format("Literal: '{0}'", Value); }
		}

		protected LiteralTerminal(LiteralTerminal other, ParserCloneArgs chain)
			: base(other, chain)
		{
			CaseSensitive = other.CaseSensitive;
			Value = other.Value;
		}

		public LiteralTerminal()
		{
		}

		public LiteralTerminal(string value)
		{
			value.ThrowIfNull("value", "Value must not be null");
			Value = value;
		}

		protected override void InnerInitialize(ParserInitializeArgs args)
		{
			base.InnerInitialize(args);
			caseSensitive = CaseSensitive ?? args.Grammar.CaseSensitive;
		}

		protected override int InnerParse(ParseArgs args)
		{
			if (TreeIndex.HasValue)
			{
				if (args.Scanner.FindInTree(TreeIndex.Value, out var matchedValue, out var tokenName))
				{
					return matchedValue.Length;
				}
			}

			if (args.Scanner.ReadString(Value, caseSensitive))
			{
				return Value.Length;
			}
			return -1;
		}

		public override Parser Clone(ParserCloneArgs args)
		{
			return new LiteralTerminal(this, args);
		}

		public override bool Equals(object obj)
		{
			if (obj is LiteralTerminal lt)
				return lt.Value == Value;
			return false;
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}

		public override string ToString()
		{
			return Value;
		}
	}
}
