using System.Collections.Generic;
using System.Linq;

namespace Eto.Parse.Scanners
{
	public class TreeScanContainer
	{
		public class Entry
		{
			public string Ebnf { get; set; }
			public string[] ThisToken { get; set; }
			public string[] NextTokens { get; set; }

			public int Length => Ebnf.Length;
			public bool IsEmpty(int at) => Length <= at;
			public char At(int at) => Ebnf[at];
		}

		public Entry[] SortedEntries { get; }

		public TreeScanContainer(IEnumerable<Entry> sortedEntries)
		{
			SortedEntries = sortedEntries.ToArray();
		}

		private int FindLowerBorder(int lowerBorder, int upperBorder, char c, int charIndex)
		{
			if (lowerBorder >= SortedEntries.Length) { return -1; }

			if (lowerBorder == upperBorder)
			{
				if (SortedEntries[lowerBorder].Length <= charIndex) { return -1; }
				if (SortedEntries[lowerBorder].At(charIndex) == c) { return upperBorder; }
				return -1;
			}
			int pivot = (lowerBorder + upperBorder) / 2;

			if (SortedEntries[pivot].IsEmpty(charIndex)) { return FindLowerBorder(pivot + 1, upperBorder, c, charIndex); }
			else if (SortedEntries[pivot].At(charIndex) < c) { return FindLowerBorder(pivot + 1, upperBorder, c, charIndex); }
			else { return FindLowerBorder(lowerBorder, pivot, c, charIndex); }
		}

		private int FindUpperBorder(int lowerBorder, int upperBorder, char c, int charIndex)
		{
			int pivot = (lowerBorder + upperBorder) / 2;
			if (pivot == lowerBorder) { return upperBorder; }

			if (SortedEntries[pivot].IsEmpty(charIndex) || SortedEntries[pivot].At(charIndex) > c) { return pivot; }

			if (SortedEntries[pivot].At(charIndex) <= c) { return FindUpperBorder(pivot, upperBorder, c, charIndex); }
			else { return FindUpperBorder(lowerBorder, pivot + 1, c, charIndex); }
		}

		public int Find(string fullString, int startIndex, out string[] tokenName)
		{
			tokenName = null;
			int lowBorder = 0;
			int highBorder = SortedEntries.Length;

			for (int i = 0; startIndex + i < fullString.Length; ++i)
			{
				char c = fullString[startIndex + i];
				int newLowBorder = FindLowerBorder(lowBorder, highBorder, c, i);

				if (newLowBorder == -1) { break; }// Word doesn't exist

				highBorder = FindUpperBorder(newLowBorder, highBorder, c, i);

				if (highBorder == -1) { return -1; }// Word doesn't exist

				if (lowBorder == highBorder - 1) { break; }

				lowBorder = newLowBorder;
			}

			// Only possibility found, check if it is really the word
			var match = SortedEntries[lowBorder].Ebnf;

			// The remaining input is too short to match
			if (fullString.Length - startIndex < match.Length) { return -1; }

			// The remaining input doesn't match
			if (fullString[startIndex..(startIndex + match.Length)] != match) { return -1; }

			// Return the length of the parsed string
			tokenName = SortedEntries[lowBorder].NextTokens;
			return match.Length;
		}
	}
}
