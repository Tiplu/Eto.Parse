using Eto.Parse.Scanners;
using System;

namespace Eto.Parse
{
	public abstract class Scanner
	{
		public int Position { get; set; }

		public abstract string Value { get; }

		public abstract int Advance(int length);

		public abstract bool IsEof { get; }

		public abstract bool ReadString(string matchString, bool caseSensitive);

		public abstract int ReadChar();

		public abstract int Peek();

		public abstract string Substring(int index, int length);

		public abstract bool FindInTree(TreeScanContainer identifier, out string matchedValue, out string[] tokenNames);

		public abstract int LineAtIndex(int index);
	}
}
