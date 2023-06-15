using System.Collections.Generic;
using System.Linq;

namespace Eto.Parse.Scanners
{
	public class TreeScanContainer
	{
		public string[] SortedAlternatives { get; }
		public string[] SortedTokenNames { get; }

		public TreeScanContainer(IEnumerable<string> sortedAlternatives, IEnumerable<string> sortedTokenNames)
		{
			SortedAlternatives = sortedAlternatives.ToArray();
			SortedTokenNames = sortedTokenNames.ToArray();
		}

		private int FindLowerBorder(int lowerBorder, int upperBorder, char c, int charIndex)
		{
			if (lowerBorder >= SortedAlternatives.Length) { return -1; }

			if (lowerBorder == upperBorder)
			{
				if (SortedAlternatives[lowerBorder].Length <= charIndex) { return -1; }
				if (SortedAlternatives[lowerBorder][charIndex] == c) { return upperBorder; }
				return -1;
			}
			int pivot = (lowerBorder + upperBorder) / 2;

			if (SortedAlternatives[pivot].Length <= charIndex) { return FindLowerBorder(pivot + 1, upperBorder, c, charIndex); }
			else if (SortedAlternatives[pivot][charIndex] < c) { return FindLowerBorder(pivot + 1, upperBorder, c, charIndex); }
			else { return FindLowerBorder(lowerBorder, pivot, c, charIndex); }
		}

		private int FindUpperBorder(int lowerBorder, int upperBorder, char c, int charIndex)
		{
			int pivot = (lowerBorder + upperBorder) / 2;
			if (pivot == lowerBorder) { return upperBorder; }

			if (SortedAlternatives[pivot].Length <= charIndex || SortedAlternatives[pivot][charIndex] > c) { return pivot; }

			if (SortedAlternatives[pivot][charIndex] <= c) { return FindUpperBorder(pivot, upperBorder, c, charIndex); }
			else { return FindUpperBorder(lowerBorder, pivot + 1, c, charIndex); }
		}

		public int Find(string fullString, int startIndex, out string tokenName)
		{
			tokenName = null;
			int lowBorder = 0;
			int highBorder = SortedAlternatives.Length;

			for (int i = 0; startIndex + i < fullString.Length; ++i)
			{
				char c = fullString[startIndex + i];
				lowBorder = FindLowerBorder(lowBorder, highBorder, c, i);

				if (lowBorder == -1) { return -1; }// Word doesn't exist

				highBorder = FindUpperBorder(lowBorder, highBorder, c, i);

				if (highBorder == -1) { return -1; }// Word doesn't exist

				if (lowBorder == highBorder - 1) { break; }
			}

			// Only possibility found, check if it is really the word
			var match = SortedAlternatives[lowBorder];

			// The remaining input is too short to match
			if (fullString.Length - startIndex < match.Length) { return -1; }

			// The remaining input doesn't match
			if (fullString[startIndex..(startIndex + match.Length)] != match) { return -1; }

			// Return the length of the parsed string
			tokenName = SortedTokenNames[lowBorder];
			return match.Length;
		}
	}
}
