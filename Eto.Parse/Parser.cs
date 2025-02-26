using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Eto.Parse
{
	/// <summary>
	/// Base parser class to define a parsing rule
	/// </summary>
	/// <remarks>
	/// All parsers should derive from this class to define the various ways to parse text.
	/// There are other base parsers that define base functionality such as a <see cref="ListParser"/>
	/// for parsers that contain a list of children parsers, or <see cref="UnaryParser"/> for parsers
	/// that contain a single child.
	/// </remarks>
	public abstract partial class Parser : ICloneable
	{
		bool hasNamedChildren;
		ParseMode mode;
		string name;
		bool addError;
		bool addErrorSet;
		bool addMatch;
		bool addMatchSet;

		enum ParseMode
		{
			Simple,
			NameOrError,
			NamedChildren
		}

		#region Properties

		/// <summary>
		/// Gets or sets the name of the match added to the match result tree
		/// </summary>
		/// <remarks>
		/// When you set this property, it affects the match result tree returned from the <see cref="Grammar.Match(string)"/>
		/// method. Each parser that is named will get a node entry in the match tree if it has succesfully matched
		/// on the input string.  This allows you to
		/// 
		/// If this is set to <c>null</c>, this parser will not add a node to the match tree, but any named
		/// children will still add to the match tree (if any).
		/// 
		/// If you set the name, the parser will automatically set <see cref="Parser.AddError"/> to <c>true</c>
		/// to give back information when this parser does not match, unless AddError has already been set
		/// to something else explicitly.
		/// </remarks>
		/// <value>The name to give the match in the match result tree</value>
		public string Name
		{
			get { return name; }
			set
			{
				name = value;
				if (!addErrorSet && name != null)
					addError = true;
				if (!addMatchSet && name != null)
					addMatch = true;
			}
		}

		/// <summary>
		/// Gets or sets the default separator to use for parsers that support a separator
		/// </summary>
		/// <value>The default separator.</value>
		public static Parser DefaultSeparator { get; set; }

		/// <summary>
		/// Gets or sets a value indicating that this parser should add to the errors list when not matched
		/// </summary>
		/// <value><c>true</c> to add errors; otherwise, <c>false</c>.</value>
		public bool AddError
		{
			get { return addError; }
			set
			{
				addError = value;
				addErrorSet = true;
			}
		}

		public bool AddMatch
		{
			get { return addMatch; }
			set
			{
				addMatch = value;
				addMatchSet = true;
			}
		}

		internal bool Reusable { get; set; }

		/// <summary>
		/// Gets a name of the parser used to describe its intent, used for the error message or display tree
		/// </summary>
		/// <value>The descriptive name</value>
		public virtual string DescriptiveName
		{
			get
			{
				if (this.name != null)
					return this.name;
				var type = GetType();
				var name = type.Name;
				if (type.GetTypeInfo().Assembly == typeof(Parser).GetTypeInfo().Assembly && name.EndsWith("Parser", StringComparison.Ordinal))
					name = name.Substring(0, name.LastIndexOf("Parser", StringComparison.Ordinal));
				return name;
			}
		}

		/// <summary>
		/// Gets a value indicating that this parser has named children
		/// </summary>
		/// <remarks>
		/// This is useful to know when to use <see cref="ParseArgs.Push"/> before parsing children, and
		/// <see cref="ParseArgs.PopFailed"/> / <see cref="ParseArgs.PopSuccess"/> after.
		/// 
		/// Using Push/Pop methods allow you to keep track of and discard (or keep) child matches based
		/// on the success or failure.  
		/// 
		/// This is set during initialization of the grammar before the first parse is performed.
		/// </remarks>
		protected bool HasNamedChildren { get { return hasNamedChildren; } }

		#endregion

		#region Events

		/// <summary>
		/// Event to handle when this parser is matched
		/// </summary>
		/// <remarks>
		/// This event is fired only for matches that have a <see cref="Parser.Name"/> defined.
		/// </remarks>
		public event Action<Match> Matched;

		/// <summary>
		/// Raises the <see cref="Matched"/> event
		/// </summary>
		/// <param name="match">Match</param>
		protected virtual void OnMatched(Match match)
		{
			if (Matched != null)
				Matched(match);
		}

		internal void TriggerMatch(Match match)
		{
			OnMatched(match);
		}

		/// <summary>
		/// Event to handle before this parser is matched
		/// </summary>
		public event Action<Match> PreMatch;

		/// <summary>
		/// Raises the <see cref="PreMatch"/> event
		/// </summary>
		/// <param name="match">Match</param>
		protected virtual void OnPreMatch(Match match)
		{
			if (PreMatch != null)
				PreMatch(match);
		}

		internal void TriggerPreMatch(Match match)
		{
			OnPreMatch(match);
		}

		#endregion

		/// <summary>
		/// Initializes a new instance of the <see cref="Eto.Parse.Parser"/> class.
		/// </summary>
		protected Parser()
		{
		}

		/// <summary>
		/// Initializes a new copy of the <see cref="Eto.Parse.Parser"/> class
		/// </summary>
		/// <param name="other">Parser to copy</param>
		/// <param name="args">Arguments for the copy</param>
		protected Parser(Parser other, ParserCloneArgs args)
		{
			Name = other.Name;
			addMatch = other.addMatch;
			addMatchSet = other.addMatchSet;
			addError = other.addError;
			addErrorSet = other.addErrorSet;

			args.Add(other, this);
		}

		/// <summary>
		/// Gets an enumeration of all child parsers of this instance
		/// </summary>
		public IEnumerable<Parser> Children
		{
			get
			{
				return Scan(null);
			}
		}

		internal IEnumerable<Parser> Scan(Action<Parser> action = null, Func<Parser, bool> filter = null)
		{
			var visited = new HashSet<Parser>();
			var stack = new Stack<Parser>();
			stack.Push(this);
			while (stack.Count > 0)
			{
				var current = stack.Pop();
				action?.Invoke(current);
				visited.Add(current);
				foreach (var item in current.GetChildren())
				{
					if (!visited.Contains(item) && (filter == null || filter(item)))
						stack.Push(item);
				}
			}
			return visited;
		}


		protected virtual IEnumerable<Parser> GetChildren()
		{
			yield break;
		}

		/// <summary>
		/// Gets the error message to display for this parser
		/// </summary>
		/// <remarks>
		/// By default, this will use the DescriptiveName
		/// </remarks>
		/// <returns>The error message to display when not matched</returns>
		public virtual string GetErrorMessage(ParserErrorArgs args)
		{
			return DescriptiveName;
		}

		public string GetErrorMessage(bool detailed = false)
		{
			return GetErrorMessage(new ParserErrorArgs(detailed));
		}

		/// <summary>
		/// Parses the input at the current position
		/// </summary>
		/// <remarks>
		/// Implementors of a Parser should implement <see cref="InnerParse"/> to perform the logic of their parser.
		/// </remarks>
		/// <param name="args">Parsing arguments</param>
		/// <returns>The length of the successfully matched value (can be zero), or -1 if not matched</returns>
		public int Parse(ParseArgs args)
		{
			if (mode == ParseMode.Simple)
			{
				var match = InnerParse(args);
				if (match >= 0)
					return match;

				args.SetChildError();
				return match;
			}
			else if (mode == ParseMode.NamedChildren)
			{
				args.Push();
				var pos = args.Scanner.Position;
				var match = InnerParse(args);
				if (match < 0)
				{
					args.PopFailed();
					if (AddError)
					{
						args.AddError(this);
						return -1;
					}
					args.SetChildError();
					return -1;
				}
				if (AddMatch)
				{
					args.PopMatch(this, pos, match);
					return match;
				}
				args.PopSuccess();
				return match;
			}
			else // if (mode == ParseMode.NameOrError)
			{
				var pos = args.Scanner.Position;
				var match = InnerParse(args);
				if (match < 0)
				{
					if (!AddError)
					{
						args.SetChildError();
					}
				}
				else if (AddMatch)
				{
					args.AddMatch(this, pos, match);
				}
				return match;
			}
		}

		/// <summary>
		/// Override to implement the main parsing logic for this parser
		/// </summary>
		/// <remarks>
		/// Never call this method directly, always call <see cref="Parse"/> when calling parse routines.
		/// </remarks>
		/// <returns>The length of the successfully matched value (can be zero), or -1 if not matched</returns>
		/// <param name="args">Parsing arguments</param>
		protected abstract int InnerParse(ParseArgs args);

		bool initialized;
		/// <summary>
		/// Called to initialize the parser when used in a grammar
		/// </summary>
		/// <remarks>
		/// This is used to perform certain tasks like caching information for performance, or to deal with
		/// things like left recursion in the grammar.
		/// </remarks>
		/// <param name="args">Initialization arguments</param>
		public void Initialize(ParserInitializeArgs args)
		{
			if (!initialized && args.Push(this))
			{
				initialized = true;
				var parent = args.Parent;
				args.Parent = this;
				InnerInitialize(args);
				args.Parent = parent;
				hasNamedChildren = (Children.Any(r => r.AddMatch || r.hasNamedChildren));
				var parentNamed = false;
				if (parent != null && hasNamedChildren)
				{
					var parentHasMatches = parent.Scan(filter: p => p != this).Any(p => p.AddMatch);
					//parentNamed = parent.Scan(filter: p => p != this).Any(p => p.AddMatch);
					parentNamed = parent.AddMatch;
				}

				mode = (hasNamedChildren && (AddMatch/* || parentNamed*/)) ? ParseMode.NamedChildren : AddMatch || AddError ? ParseMode.NameOrError : ParseMode.Simple;

				args.Pop();
			}
		}

		protected virtual void InnerInitialize(ParserInitializeArgs args)
		{
		}

		/// <summary>
		/// Determines whether this instance is left recursive with the specified parser
		/// </summary>
		/// <returns><c>true</c> if this instance is left recursive the specified parser; otherwise, <c>false</c>.</returns>
		/// <param name="parser">Parser.</param>
		public bool IsLeftRecursive(Parser parser)
		{
			return IsLeftRecursive(new ParserContainsArgs(parser));
		}

		/// <summary>
		/// Determines whether this instance is left recursive with the specified parser
		/// </summary>
		/// <remarks>
		/// This variant can be overridden by implementors to determine left recursion. Use the <paramref name="args"/>
		/// to ensure infinite recursion does not occur using Push/Pop.
		/// </remarks>
		/// <returns><c>true</c> if this instance is left recursive the specified parser; otherwise, <c>false</c>.</returns>
		/// <param name="args">Arguments for finding the left recursion</param>
		public virtual bool IsLeftRecursive(ParserContainsArgs args)
		{
			return object.ReferenceEquals(args.Parser, this);
		}

		public IEnumerable<Parser> Find(string parserId)
		{
			return Find(new ParserFindArgs(parserId));
		}

		public virtual IEnumerable<Parser> Find(ParserFindArgs args)
		{
			if (string.Equals(Name, args.ParserId, StringComparison.Ordinal))
				yield return this;
		}

		public Parser this[string parserId]
		{
			get { return Find(parserId).FirstOrDefault(); }
		}

		public Parser Clone()
		{
			return Clone(new ParserCloneArgs());
		}

		public abstract Parser Clone(ParserCloneArgs args);

		object ICloneable.Clone()
		{
			return Clone();
		}


		public void Replace(ParserReplaceArgs args)
		{
			if (args.Push(this))
			{
				InnerReplace(args);
				//args.Pop();
			}
		}

		protected virtual void InnerReplace(ParserReplaceArgs args)
		{
		}

		/// <summary>
		/// Sets the <see cref="AddError"/> flag on all children of this parser
		/// </summary>
		/// <param name="addError">Value to set the AddError flag to</param>
		/// <param name="name">Name of the parser(s) to match, or null to set all children</param>
		public void SetError(bool addError, string name = null)
		{
			var children = Children;
			if (name != null)
				children = children.Where(r => r.Name == name);
			foreach (var item in children)
				item.AddError = addError;
		}

		/// <summary>
		/// Sets the <see cref="AddError"/> flag on all children of this parser
		/// </summary>
		/// <param name="addError">Value to set the AddError flag to</param>
		/// <param name="name">Name of the parser(s) to match, or null to set all children</param>
		/// <typeparam name="T">The type of parser to update</typeparam>
		public void SetError<T>(bool addError, string name = null)
			where T : Parser
		{
			var children = Children.OfType<T>();
			if (name != null)
				children = children.Where(r => r.Name == name);
			foreach (var item in children)
				item.AddError = addError;
		}

		/// <summary>
		/// Gets the object value of the parser for the specified match
		/// </summary>
		/// <remarks>
		/// Specialized parsers such as <see cref="Parsers.NumberParser"/>, <see cref="Parsers.StringParser"/>, etc
		/// can return a type-specific value from its string representation using this method.
		/// 
		/// For example, the NumberParser can return an int, decimal, double, etc. and the StringParser
		/// can process escape sequences or double quoted values.
		/// 
		/// To get the value from a specified text fragment, use <see cref="GetValue(string)"/>.
		/// 
		/// Implementors of parsers can override this (or preferrably <see cref="GetValue(string)"/>) to
		/// provide special logic to get the translated object value.
		/// </remarks>
		/// <returns>The translated object value from the range specified in the <paramref name="match"/></returns>
		/// <param name="match">Match to get the object value for</param>
		public virtual object GetValue(Match match)
		{
			return GetValue(match.Text);
		}

		/// <summary>
		/// Gets the object value of the parser for the specified text representation
		/// </summary>
		/// <remarks>
		/// Specialized parsers such as <see cref="Parsers.NumberParser"/>, <see cref="Parsers.StringParser"/>, etc
		/// can return a type-specific value from its string representation using this method.
		/// 
		/// For example, the NumberParser can return an int, decimal, double, etc. and the StringParser
		/// can process escape sequences or double quoted values.
		/// 
		/// To get the value from a specified <see cref="Match"/>, use <see cref="GetValue(Match)"/> instead.
		/// 
		/// Implementors of parsers can override this to provide special logic to get the translated object value.
		/// </remarks>
		/// <returns>The translated object value from the specified <paramref name="text"/></returns>
		/// <param name="text">Text representation to translate to an object value</param>
		public virtual object GetValue(string text)
		{
			return text;
		}
	}
}
