using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.XPath;

namespace Gibbed.Cryptic.Champions.Deparse
{
    internal partial class Program
    {
        private static void LoadParsers(Dictionary<string, Parser> parsers)
        {
            Dictionary<string, Dictionary<string, string>> names = new Dictionary<string, Dictionary<string, string>>();

            foreach (string path in Directory.GetFiles("parsers", "names_*.xml", SearchOption.AllDirectories))
            {
                Stream input = File.OpenRead(path);
                
                XPathDocument doc = new XPathDocument(input);
                XPathNavigator nav = doc.CreateNavigator();
                XPathNavigator node;
                XPathNodeIterator nodes;

                node = nav.SelectSingleNode("/values");
                string name = node.GetAttribute("name", "");

                if (name == string.Empty)
                {
                    throw new Exception("named values without a name");
                }

                Dictionary<string, string> values = new Dictionary<string, string>();
                
                nodes = node.Select("value");
                while (nodes.MoveNext())
                {
                    XPathNavigator current = nodes.Current;
                    if (values.ContainsKey(current.Value) == false)
                    {
                        values.Add(current.Value, current.GetAttribute("name", ""));
                    }
                }

                names.Add(name, values);
            }

            foreach (string path in Directory.GetFiles("parsers", "parser_*.xml", SearchOption.AllDirectories))
            {
                Stream input = File.OpenRead(path);
                
                XPathDocument doc = new XPathDocument(input);
                XPathNavigator nav = doc.CreateNavigator();
                XPathNavigator node, subnode;
                XPathNodeIterator nodes, subnodes;
                
                Parser parser = new Parser();

                node = nav.SelectSingleNode("/structure");
                parser.Name = node.GetAttribute("name", "");

                if (parser.Name == string.Empty)
                {
                    throw new Exception("parser without a name");
                }

                string id = node.GetAttribute("id", "");
                if (id != string.Empty)
                {
                    parser.Id = UInt32.Parse(id);
                }

                parser.Mode = (StructureMode)Enum.Parse(typeof(StructureMode), node.GetAttribute("mode", ""));

                nodes = node.Select("member");
                while (nodes.MoveNext())
                {
                    XPathNavigator current = nodes.Current;

                    Parser.Member member = new Parser.Member();
                    member.Name = current.GetAttribute("name", "");

                    string index = current.GetAttribute("index", "");
                    if (index == String.Empty)
                    {
                        member.Index = -1;
                    }
                    else
                    {
                        member.Index = int.Parse(index);
                    }

                    member.Type = (Parser.Member.DataType)Enum.Parse(typeof(Parser.Member.DataType), current.GetAttribute("type", ""));
                    member.Mode = (Parser.Member.DataMode)Enum.Parse(typeof(Parser.Member.DataMode), current.GetAttribute("mode", ""));

                    if (member.Mode == Parser.Member.DataMode.Static)
                    {
                        member.ExplicitCount = int.Parse(current.GetAttribute("count", ""));
                    }

                    if (member.Type == Parser.Member.DataType.Structure ||
                        member.Type == Parser.Member.DataType.Reference)
                    {
                        member.Reference = current.GetAttribute("reference", "");
                    }

                    string namedvalues = current.GetAttribute("names", "");
                    if (namedvalues != string.Empty)
                    {
                        member.PossibleNames = names[namedvalues];
                    }

                    parser.Members.Add(member);
                }

                parsers.Add(parser.Name, parser);
                input.Close();
            }
        }
    }
}
