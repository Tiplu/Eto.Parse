using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

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
			public override string ToString() => Ebnf;
		}

		public string TypeId { get; }
		public Entry[] SortedEntries { get; }

		public TreeScanContainer(string typeId, IEnumerable<Entry> sortedEntries)
		{
			TypeId = typeId;
			SortedEntries = sortedEntries.ToArray();
		}

		public bool IsOutOfBounds(int pivot) => pivot < 0 || pivot >= SortedEntries.Length;
		public bool IsSmallerThan(int pivot, int at, char c) => SortedEntries[pivot].Length <= at || SortedEntries[pivot].Ebnf[at] < c;
		public bool IsEqual(int pivot, int at, char c) => SortedEntries[pivot].Length > at && SortedEntries[pivot].Ebnf[at] == c;

		public bool IsGreaterThan(int pivot, int at, char c) => SortedEntries[pivot].Length > at && SortedEntries[pivot].Ebnf[at] > c;

		// 4 Cases: 1. entry exists, 2. entry is below everything, 3. entry is in the middle but doesn't exist, 4. entry is above everything
		private int FindLowerBorder(int lowerBorder, int upperBorder, char c, int charIndex)
		{
			int pivot = (lowerBorder + upperBorder) / 2;

			// Current Index is smaller than c, go up
			if (IsSmallerThan(pivot, charIndex, c))
			{
				if (pivot == upperBorder - 1) { return -1; } // case 4: entry is above everything
				return FindLowerBorder(pivot + 1, upperBorder, c, charIndex);
			}

			// If we reached the end, return this value
			if (pivot == lowerBorder || IsSmallerThan(pivot - 1, charIndex, c))
			{
				if (IsEqual(pivot, charIndex, c)) { return pivot; } // case 1: entry found
				return -1; // case 2 or case 3: entry should be here, but missing
			}

			// Else go down
			return FindLowerBorder(lowerBorder, pivot, c, charIndex);
		}

		private int FindUpperBorder(int lowerBorder, int upperBorder, char c, int charIndex)
		{
			// If we're not at the low end but lowerBorder and upperBorder meet, the word doesn't exist
			if (upperBorder < lowerBorder) { return -1; }

			int pivot = (lowerBorder + upperBorder) / 2;

			if (pivot == upperBorder || IsGreaterThan(pivot, charIndex, c))
			{
				// If we reached the end, return this value
				if (IsEqual(pivot - 1, charIndex, c)) { return pivot; }

				return FindUpperBorder(lowerBorder, pivot - 1, c, charIndex);
			}

			// Else go up
			return FindUpperBorder(pivot + 1, upperBorder, c, charIndex);
		}

		public int Find(string fullString, int startIndex, out string[] tokenName)
		{
			tokenName = null;
			int lowBorder = 0;
			int highBorder = SortedEntries.Length;

			int lastMatch = -1;
			for (int i = 0; startIndex + i < fullString.Length; ++i)
			{
				char c = fullString[startIndex + i];
				lowBorder = FindLowerBorder(lowBorder, highBorder, c, i);

				if (lowBorder == -1) { break; }// Word doesn't exist

				highBorder = FindUpperBorder(lowBorder, highBorder, c, i);

				if (highBorder == -1) { break; }// Word doesn't exist

				// If we have reached the length of the lowBorder word, consider it the best match
				if (i + 1 == SortedEntries[lowBorder].Length) { lastMatch = lowBorder; }

				if (highBorder - lowBorder == 1) { break; } // Only one word left, early out
			}

			// Get the best match (there may be an even better match than lastMatch now)
			bool newMatch = false;
			Entry match = null;
			if (lowBorder != -1)
			{
				match = SortedEntries[lowBorder];

				// If the new match fits, take that over the last match
				if (fullString.Length - startIndex >= match.Length &&
					fullString[startIndex..(startIndex + match.Length)] == match.Ebnf)
				{
					newMatch = true;
				}
			}

			if (!newMatch)
			{
				if (lastMatch == -1) { return -1; } // no match at all
				match = SortedEntries[lastMatch];
			}

			// Return the length of the parsed string
			tokenName = match.NextTokens;
			return match.Length;
		}
	}
}
