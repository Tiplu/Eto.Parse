using Eto.Parse.Parsers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Eto.Parse
{
	/// <summary>
	/// Arguments used for each parse operation
	/// </summary>
	/// <remarks>
	/// This is used during the parsing process to track the current match tree, errors, scanner, etc.
	/// </remarks>
	public class ParseArgs
	{
		public class ParentParser
		{
			public Parser Parser;
			public string Name => Parser.Name;
			public int ErrorIndex;

			public ParentParser(Parser parser, int errorIndex)
			{
				Parser = parser;
				ErrorIndex = errorIndex;
			}

			public override int GetHashCode()
			{
				return $"{Name}{ErrorIndex}".GetHashCode();
			}

			public override bool Equals(object obj)
			{
				if (obj is ParentParser parent)
					return ReferenceEquals(parent.Parser, Parser) && ErrorIndex == parent.ErrorIndex;
				return false;
			}
		}

		public class StackPusher : IDisposable
		{
			public class StackEntry
			{
				public Parser Parser;
				public bool Failed;

				public StackEntry(Parser parser, bool failed)
				{
					Parser = parser;
					Failed = failed;
				}
			}

			private ParseArgs Args;
			public StackPusher(ParseArgs args, Parser thisParser)
			{
				Args = args;
				args.CurrentParserStack.Push(new StackEntry(thisParser, false));
			}

			public void MarkFailed()
			{
				Args.CurrentParserStack.Peek().Failed = true;
			}

			public void Dispose()
			{
				Args.CurrentParserStack.Pop();
			}
		}

		readonly SlimStack<MatchCollection> nodes = new SlimStack<MatchCollection>(50);
		private Dictionary<LiteralTerminal, HashSet<ParentParser>> errors = new Dictionary<LiteralTerminal, HashSet<ParentParser>>();
		int childErrorIndex = -1;
		int errorIndex = -1;
		readonly Dictionary<object, object> properties = new Dictionary<object, object>();

		public Dictionary<object, object> Properties { get { return properties; } }

		public List<MatchCollection> TriedMatches = new List<MatchCollection>();

		public Stack<StackPusher.StackEntry> CurrentParserStack = new Stack<StackPusher.StackEntry>();

		/// <summary>
		/// Gets the root match when the grammar is matched
		/// </summary>
		public GrammarMatch Root { get; internal set; }

		/// <summary>
		/// Gets the current scanner used to parse the text
		/// </summary>
		/// <value>The scanner</value>
		public Scanner Scanner { get; private set; }

		/// <summary>
		/// Gets the current grammar being parsed
		/// </summary>
		/// <value>The grammar.</value>
		public Grammar Grammar { get; private set; }

		/// <summary>
		/// Gets the index of the last parser error (if any), or -1 if the error has not been set
		/// </summary>
		/// <remarks>
		/// Use the <see cref="Errors"/> collection to get the list of parsers that had an invalid match
		/// at this position.
		/// 
		/// Only parsers with the <see cref="Parser.AddError"/> flag turned on will cause the error
		/// index to be updated to the position of where that parser started from.
		/// 
		/// To get where the actual error occurred, see <see cref="ChildErrorIndex"/>, which gives
		/// you the exact position where the failure occurred.
		/// 
		/// Alternatively, for debugging purposes you can turn on AddError for all parsers by using
		/// <see cref="Parser.SetError"/> 
		/// </remarks>
		/// <value>The index of the error.</value>
		public int ErrorIndex { get { return errorIndex; } }

		/// <summary>
		/// Gets the index of where the error action
		/// </summary>
		/// <value>The index of the error context.</value>
		public int ChildErrorIndex { get { return childErrorIndex; } }

		/// <summary>
		/// Gets the list of parsers that failed a match at the specicified <see cref="ErrorIndex"/>
		/// </summary>
		/// <remarks>
		/// This is only added to when the <see cref="Parser.AddError"/> boolean value is true, and failed to match.
		/// 
		/// For example, if you have a SequenceParser with <see cref="AddError"/> set to true, but none of its children,
		/// then even when some of the children match (but not all otherwise there wouldn't be an error), then
		/// the child won't be added to this list, only the parent.
		/// 
		/// The <see cref="ErrorIndex"/> will also indicate the position that the *parent* failed to match, not the
		/// child.  To get the child index, use <see cref="ChildErrorIndex"/>
		/// </remarks>
		/// <value>The list of parsers that have errors</value>
		public IReadOnlyDictionary<LiteralTerminal, HashSet<ParentParser>> Errors => errors;

		internal ParseArgs(Grammar grammar, Scanner scanner)
		{
			Grammar = grammar;
			Scanner = scanner;
		}

		internal bool IsRoot
		{
			get { return nodes.Count <= 1; }
		}

		public void ClearErrors()
		{
			errors = KeepErrors().ToDictionary(x => x.Key, x => x.Value);
		}

		private IEnumerable<KeyValuePair<LiteralTerminal, HashSet<ParentParser>>> KeepErrors()
		{
			string scannerValue = Scanner.Value;
			foreach (KeyValuePair<LiteralTerminal, HashSet<ParentParser>> kvp in errors)
			{
				string ltValue = kvp.Key.Value.ToLowerInvariant();

				HashSet<ParentParser> possibleErrors = new HashSet<ParentParser>();
				foreach (ParentParser parent in kvp.Value)
				{
					string fromErrorUntrimmed = scannerValue.Substring(parent.ErrorIndex).ToLowerInvariant();
					string fromErrorTrimmed = fromErrorUntrimmed.TrimStart();

					if (ltValue.Contains(fromErrorTrimmed))
						possibleErrors.Add(new ParentParser(parent.Parser, parent.ErrorIndex + fromErrorUntrimmed.Length - fromErrorTrimmed.Length));
				}

				if (possibleErrors.Any())
					yield return new KeyValuePair<LiteralTerminal, HashSet<ParentParser>>(kvp.Key, possibleErrors);
			}
		}

		public void AddLiteralError(LiteralTerminal parser)
		{
			if (!errors.TryGetValue(parser, out HashSet<ParentParser> list))
			{
				list = new HashSet<ParentParser>();
				errors.Add(parser, list);
			}
			Parser p = CurrentParserStack.FirstOrDefault(x => !string.IsNullOrEmpty(x.Parser.Name))?.Parser;
			if (p != null)
			{
				list.Add(new ParentParser(p, Scanner.Position));
			}
		}

		/// <summary>
		/// Sets the child error index for parsers that have <see cref="Parser.AddError"/> set to false
		/// </summary>
		public void SetChildError()
		{
			var pos = Scanner.Position;
			if (pos > childErrorIndex)
				childErrorIndex = pos;
		}

		/// <summary>
		/// Pushes a new match tree node
		/// </summary>
		/// <remarks>
		/// Use this when there is a possibility that a child parser will not match, such as the <see cref="Parsers.OptionalParser"/>,
		/// items in a <see cref="Parsers.AlternativeParser"/>
		/// </remarks>
		public void Push()
		{
			nodes.PushDefault();
		}

		/// <summary>
		/// Pops the last match tree node, and returns its value
		/// </summary>
		/// <remarks>
		/// Use <see cref="PopSuccess"/> or <see cref="PopFailed"/> when implementing parsers.
		/// This does not perform any logic like merging the match tree with the parent node when succesful,
		/// nor does it allow for re-use of the match collection for added performance.
		/// </remarks>
		public MatchCollection Pop()
		{
			return nodes.Pop();
		}

		/// <summary>
		/// When an optional match is successful, this pops the current match tree node and merges it with the
		/// parent match tree node.
		/// </summary>
		/// <remarks>
		/// This call must be proceeded with a call to <see cref="Push"/> to push a match tree node.
		/// </remarks>
		public void PopSuccess()
		{
			var last = nodes.PopKeep();
			if (last != null)
			{
				var node = nodes.Last;
				if (node != null)
				{
					node.AddRange(last);
					last.Clear();
				}
				else
				{
					nodes.Last = last;
					nodes[nodes.Count] = null;
				}
			}
		}

		public void StoreMatches()
		{
			if (nodes.count > 0)
			{
				var last = nodes.Last;
				if (last != null && last.Any())
				{
					TriedMatches.Add(new MatchCollection(last));
				}
			}
		}

		/// <summary>
		/// Clears the matches of the current match tree node
		/// </summary>
		/// <remarks>
		/// Used instead of doing a PopFailed() then another Push(), like an Alternative parser
		/// </remarks>
		public void ClearMatches()
		{
			var last = nodes.Last;
			if (last != null)
			{
				last.Clear();
			}
		}

		/// <summary>
		/// When an optional match did not succeed, this pops the current match tree node and prepares it for re-use.
		/// </summary>
		/// <remarks>
		/// This call must be proceeded with a call to <see cref="Push"/> to push a match tree node.
		/// </remarks>
		public void PopFailed()
		{
			var last = nodes.PopKeep();
			if (last != null)
			{
				last.Clear();
			}
		}

		/// <summary>
		/// Pops a succesful named match node, and adds it to the parent match node
		/// </summary>
		/// <param name="parser">Parser with the name to add to the match tree</param>
		/// <param name="index">Index of the start of the match</param>
		/// <param name="length">Length of the match</param>
		/// <param name="name">Name to give the match</param>
		public void PopMatch(Parser parser, int index, int length, string name)
		{
			// always successful here, assume at least two or more nodes
			var last = nodes.Pop();

			if (nodes.Count > 0)
			{
				var node = nodes.Last;
				if (node == null)
				{
					node = new MatchCollection();
					nodes.Last = node;
				}
				node.Add(new Match(name, parser, Scanner, index, length, last));
			}
		}

		/// <summary>
		/// Pops a succesful named match node, and adds it to the parent match node
		/// </summary>
		/// <param name="parser">Parser with the name to add to the match tree</param>
		/// <param name="index">Index of the start of the match</param>
		/// <param name="length">Length of the match</param>
		public void PopMatch(Parser parser, int index, int length)
		{
			// always successful here, assume at least two or more nodes
			var last = nodes.Pop();

			if (nodes.Count > 0)
			{
				var node = nodes.Last;
				if (node == null)
				{
					node = new MatchCollection();
					nodes.Last = node;
				}
				node.Add(new Match(parser, Scanner, index, length, last));
			}
		}

		/// <summary>
		/// Adds a match to the current result match node with the specified name
		/// </summary>
		/// <remarks>
		/// This is used to add a parse match to the result match tree
		/// </remarks>
		/// <param name="parser">Parser for the match</param>
		/// <param name="index">Index of the start of the match</param>
		/// <param name="length">Length of the match</param>
		/// <param name="name">Name of this match (usually the Parser.Match value)</param>
		public void AddMatch(Parser parser, int index, int length, string name)
		{
			if (nodes.Count > 0)
			{
				var node = nodes.Last;
				if (node == null)
				{
					node = new MatchCollection();
					nodes.Last = node;
				}
				node.Add(new Match(name, parser, Scanner, index, length, null));
			}
		}

		/// <summary>
		/// Adds a match to the current result match node
		/// </summary>
		/// <remarks>
		/// This is used to add a parse match to the result match tree
		/// </remarks>
		/// <param name="parser">Parser for the match</param>
		/// <param name="index">Index of the start of the match</param>
		/// <param name="length">Length of the match</param>
		public void AddMatch(Parser parser, int index, int length)
		{
			if (nodes.Count > 0)
			{
				var node = nodes.Last;
				if (node == null)
				{
					node = new MatchCollection();
					nodes.Last = node;
				}
				node.Add(new Match(parser, Scanner, index, length));
			}
		}
	}
}
