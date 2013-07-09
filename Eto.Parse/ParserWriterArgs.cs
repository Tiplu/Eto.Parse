using System;
using Eto.Parse.Parsers;
using System.Collections.Generic;
using System.IO;

namespace Eto.Parse
{
	public class ParserWriterArgs
	{
		Dictionary<Type, int> names = new Dictionary<Type, int>();
		HashSet<string> namedParsers = new HashSet<string>();
		Dictionary<object, string> objectNames = new Dictionary<object, string>();

		public Stack<Parser> Parsers { get; private set; }

		public virtual int Level { get; set; }

		public IParserWriter Writer { get; internal set; }

		public ParserWriterArgs()
		{
			Parsers = new Stack<Parser>();
		}

		public void Push(Parser parser)
		{
			Parsers.Push(parser);
			Level += 1;
		}

		public void Pop()
		{
			Parsers.Pop();
			Level -= 1;
		}

		public string Write(Parser parser)
		{
			return Writer.WriteParser(this, parser);
		}

		public string Write(ICharTester tester)
		{
			return Writer.WriteTester(this, tester);
		}

		public string GenerateName(ICharTester tester)
		{
			string name;
			if (!objectNames.TryGetValue(tester, out name))
			{
				name = GenerateName(tester.GetType());
				objectNames[tester] = name;
			}
			return name;
		}

		public string GenerateName(Parser parser)
		{
			string name;
			if (!objectNames.TryGetValue(parser, out name))
			{
				name = GenerateName(parser.GetType());
				objectNames[parser] = name;
			}
			return name;
		}

		public string GenerateName(Type type)
		{
			int val;
			if (!names.TryGetValue(type, out val))
				val = 0;
			val++;

			names[type] = val;
			return type.Name.ToLowerInvariant() + val;
		}

		public bool IsDefined(string name)
		{
			if (!namedParsers.Contains(name))
			{
				namedParsers.Add(name);
				return false;
			}
			return true;
		}
	}
}
