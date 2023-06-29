using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Eto.Parse
{
    /// <summary>
    /// Represents a matched range of the input string
    /// </summary>
    public class Match
    {
        MatchCollection matches;
        string name;
        readonly int index;
        readonly int length;
        readonly Parser parser;
        readonly Scanner scanner;
        object value;
        internal static readonly Match EmptyMatch = new Match(null, null, null, -1, -1, new MatchCollection());

        public MatchCollection Matches
        {
            get { return matches ?? (matches = new MatchCollection()); }
        }

        public bool HasMatches
        {
            get { return matches != null && matches.Count > 0; }
        }

        public Scanner Scanner { get { return scanner; } }

        public object Value => Success ? (value ?? (value = parser.GetValue(this))) : null;

        public string StringValue
        {
            get
            {
                var val = Value;
                if (val is String str)
                    return str;
                return Convert.ToString(val);
            }
        }

        public string Text { get { return Success ? scanner.Substring(index, length) : null; } }

        public string Name { get { return name ?? (name = parser.Name); } set { name = value; } }

        public Parser Parser { get { return parser; } }

        public object Tag { get; set; }

        public int Index { get { return index; } }

        public int Length { get { return length; } }

        public bool Success { get { return length >= 0; } }

        public bool Empty { get { return length == 0; } }

        public int Line
        {
            get { return Scanner.LineAtIndex(index); }
        }

        internal Match(string name, Parser parser, Scanner scanner, int index, int length, MatchCollection matches)
        {
            this.name = name;
            this.parser = parser;
            this.scanner = scanner;
            this.index = index;
            this.matches = matches;

            if (length > -1)
            {
                var lastMatchWithLength = matches?.LastOrDefault(x => x.length > 0);
                var firstMatchWithLength = matches?.FirstOrDefault(x => x.length > 0);

                if (lastMatchWithLength != null) { this.length = Math.Max(length, lastMatchWithLength.Index + lastMatchWithLength.Length - firstMatchWithLength.Index); }
                else { this.length = length; }
            }
            else
            {
                this.length = length;
            }
        }

        internal Match(Parser parser, Scanner scanner, int index, int length, MatchCollection matches)
        {
            //this.name = parser.Name;
            this.parser = parser;
            this.scanner = scanner;
            this.index = index;
            this.matches = matches;

            if (length > -1)
            {
                var lastMatchWithLength = matches?.LastOrDefault(x => x.length > 0);
                var firstMatchWithLength = matches?.FirstOrDefault(x => x.length > 0);

                if (lastMatchWithLength != null) { this.length = Math.Max(length, lastMatchWithLength.Index + lastMatchWithLength.Length - firstMatchWithLength.Index); }
                else { this.length = length; }
            }
            else
            {
                this.length = length;
            }
        }

        internal Match(Parser parser, Scanner scanner, int index, int length)
        {
            //this.name = parser.Name;
            this.parser = parser;
            this.scanner = scanner;
            this.index = index;
            this.length = length;
        }

        public IEnumerable<Match> Find(string id, bool deep = false)
        {
            if (matches != null)
                return matches.Find(id, deep);
            else
                return Enumerable.Empty<Match>();
        }
        public IEnumerable<Match> Find(string id)
        {
            if (matches != null)
                return matches.Find(id);
            else
                return Enumerable.Empty<Match>();
        }

        public Match this[string id, bool deep = false]
        {
            get
            {
                if (matches != null)
                    return matches[id, deep];
                else
                    return Match.EmptyMatch;
            }
        }

        internal void TriggerPreMatch()
        {
            if (matches != null)
            {
                for (int i = 0; i < matches.Count; i++)
                {
                    Match match = matches[i];
                    match.TriggerPreMatch();
                }
            }
            Parser.TriggerPreMatch(this);
        }

        internal void TriggerMatch()
        {
            if (matches != null)
            {
                for (int i = 0; i < matches.Count; i++)
                {
                    Match match = matches[i];
                    match.TriggerMatch();
                }
            }
            Parser.TriggerMatch(this);
        }

        public override string ToString()
        {
            return Text ?? string.Empty;
        }

        public static bool operator true(Match match)
        {
            return match.Success;
        }

        public static bool operator false(Match match)
        {
            return !match.Success;
        }
    }

    public class MatchCollection : List<Match>
    {
        public MatchCollection()
            : base(4)
        {
        }

        public MatchCollection(IEnumerable<Match> collection)
            : base(collection)
        {
        }

        public IEnumerable<Match> Find(string id)
        {
            for (int i = 0; i < Count; i++)
            {
                var item = this[i];
                if (item.Name == id)
                {
                    yield return item;
                }
            }
        }


        public IEnumerable<Match> Find(string id, bool deep)
        {
            bool found = false;
            for (int i = 0; i < Count; i++)
            {
                var item = this[i];
                if (item.Name == id)
                {
                    yield return item;
                    found = true;
                }
            }
            if (deep && !found)
            {
                for (int i = 0; i < Count; i++)
                {
                    var item = this[i];
                    foreach (var child in item.Find(id, deep))
                    {
                        yield return child;
                    }
                }
            }
        }

        public Match this[string id, bool deep]
        {
            get
            {
                if (!deep)
                {
                    for (int i = 0; i < Count; i++)
                    {
                        var item = this[i];
                        if (item.Name == id)
                        {
                            return item;
                        }
                    }
                    return Match.EmptyMatch;
                }

                return Find(id, deep).FirstOrDefault() ?? Match.EmptyMatch;

            }
        }

        public Match this[string id]
        {
            get
            {
                for (int i = 0; i < Count; i++)
                {
                    var item = this[i];
                    if (item.Name == id)
                    {
                        return item;
                    }
                }
                return Match.EmptyMatch;
            }
        }
    }
}
