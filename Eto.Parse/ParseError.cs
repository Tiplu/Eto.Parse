using System;

namespace Eto.Parse
{
	public class ParseError
	{
		public Parser Parser { get; set; }
		public bool IsOptional { get; set; }
		public int Position { get; set; }

		public ParseError(Parser parser, bool isOptional, int position)
		{
			Parser = parser;
			IsOptional = isOptional;
			Position = position;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Parser, IsOptional, Position);
		}

		public override bool Equals(object obj)
		{
			if(obj is ParseError error)
			{
				return Position == error.Position
					&& IsOptional == error.IsOptional
					&& Parser.Equals(error.Parser);
			}
			return false;
		}
	}
}
