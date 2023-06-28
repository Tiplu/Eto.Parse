using System;
using System.Collections.Generic;

namespace Eto.Parse
{
	public class ParserInitializeArgs : ParserChain
	{
		List<Parser> recursionFixes;

		public Grammar Grammar { get; private set; }

		public Parser Parent { get; internal set;}

		public string AnyTypeAtom { get; }

		public List<Parser> RecursionFixes
		{
			get { return recursionFixes ?? (recursionFixes = new List<Parser>()); }
		}

		public ParserInitializeArgs(Grammar grammar, string anytypeAtom)
		{
			this.Grammar = grammar;
			this.AnyTypeAtom = anytypeAtom;
		}
	}
}

