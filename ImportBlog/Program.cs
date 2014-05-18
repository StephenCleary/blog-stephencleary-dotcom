using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace ImportBlog
{
    class Program
    {
        private const string Entry = "{http://www.w3.org/2005/Atom}entry";
        private const string Category = "{http://www.w3.org/2005/Atom}category";
        private const string Title = "{http://www.w3.org/2005/Atom}title";
        private const string Content = "{http://www.w3.org/2005/Atom}content";
        private const string Link = "{http://www.w3.org/2005/Atom}link";
        private const string Published = "{http://www.w3.org/2005/Atom}published";
        private const string Control = "{http://purl.org/atom/app#}control";
        private const string Draft = "{http://purl.org/atom/app#}draft";

        static void Main(string[] args)
        {
            try
            {
                Directory.CreateDirectory("_posts");
                var blog = XDocument.Load("blog.xml");
                foreach (var element in blog.Root.Elements(Entry))
                {
                    DateTimeOffset published = DateTimeOffset.MinValue;
                    string title = null;
                    string content = null;
                    try
                    {
                        var category = element.Elements(Category).Single(x => x.Attribute("scheme").Value == "http://schemas.google.com/g/2005#kind");
                        if (category.Attribute("term").Value != "http://schemas.google.com/blogger/2008/kind#post")
                            continue;
                        var control = element.Element(Control);
                        if (control != null)
                        {
                            var draft = control.Element(Draft);
                            if (draft != null && draft.Value == "yes")
                                continue; // TODO: import drafts
                        }

                        published = DateTimeOffset.Parse(element.Element(Published).Value);
                        var tags = element.Elements(Category).Where(x => x.Attribute("scheme").Value == "http://www.blogger.com/atom/ns#").Select(x => x.Attribute("term").Value).ToArray();
                        title = element.Element(Title).Value;
                        content = "<div>" + PreprocessHtml(element.Element(Content).Value) + "</div>";
                        var url = element.Elements(Link).Single(x => x.Attribute("rel").Value == "alternate").Attribute("href").Value;

                        var filename = "_posts/" + published.Year.ToString("D4") + "-" + published.Month.ToString("D2") + "-" + published.Day.ToString("D2") + "-" + Path.ChangeExtension(url, "md").Substring(url.LastIndexOf('/') + 1);
                        var sb = new StringBuilder();
                        sb.AppendLine("---");
                        sb.AppendLine("layout: post");
                        sb.AppendLine("title: " + YamlString(title));
                        sb.AppendLine("tags: [" + string.Join(", ", tags.Select(YamlString)) + "]");
                        sb.AppendLine("---");
                        sb.Append(HtmlToMarkdown(XDocument.Parse(content, LoadOptions.PreserveWhitespace).Root, published, title));
                        File.WriteAllText(filename, sb.ToString());
                    }
                    catch (Exception ex)
                    {
                        if (content != null)
                        {
                            Console.WriteLine(title);
                            Console.WriteLine("Published " + published);
                        }
                        Console.WriteLine(ex.Message);
                    }
                }
                Console.WriteLine("Done.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            Console.ReadKey();
        }

        private static string YamlString(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        static readonly Regex UrlWithAmpersand = new Regex(@"http[^""< ]+&[a-zA-Z_]+=[^""< ]*[""< ]");
        private static string PreprocessHtml(string html)
        {
            // Minimal preprocessing to get the HTML to parse as XML.

            // URL references such as "/blah&ASIN=0735665877" break the XML parser with message "'=' is an unexpected token. The expected token is ';'"
            // It's trying to interpret the "&" as an entity.
            // So, convert URL references.
            html = UrlWithAmpersand.Replace(html, match => match.Value.Replace("&", "&amp;"));
            return html;
        }

        private static string HtmlToMarkdown(XElement root, DateTimeOffset published, string title)
        {
            var translator = new HtmlToMarkdownTranslator(published, title);
            translator.ImproperHeaders = root.Descendants("h3").Any();
            return translator.Parse(root);
        }

        private sealed class HtmlToMarkdownTranslator
        {
            private readonly string _title;
            private readonly DateTimeOffset _published;
            private static readonly HashSet<string> _unknownElementTypes = new HashSet<string>();
            private bool _inPre;
            private Stack<bool> _lists = new Stack<bool>();
            private bool _inTableData;
            private bool _inTable;

            public HtmlToMarkdownTranslator(DateTimeOffset published, string title)
            {
                _published = published;
                _title = title;
            }

            public bool ImproperHeaders { get; set; }

            public string Parse(XElement element)
            {
                var sb = new StringBuilder();
                foreach (var node in element.Nodes())
                {
                    if (node.NodeType == XmlNodeType.Element)
                    {
                        var child = (XElement)node;
                        if (child.Name == "p")
                        {
                            sb.Append("\r\n\r\n" + Parse(child) + "\r\n\r\n");
                        }
                        else if (child.Name == "br")
                        {
                            sb.Append("  ");
                        }
                        else if (child.Name == "h3")
                        {
                            sb.Append("## " + Parse(child));
                        }
                        else if (child.Name == "h4")
                        {
                            sb.Append(ImproperHeaders ? "### " : "## ");
                            sb.Append(Parse(child));
                        }
                        else if (child.Name == "h5")
                        {
                            sb.Append(ImproperHeaders ? "#### " : "### ");
                            sb.Append(Parse(child));
                        }
                        else if (child.Name == "em" || child.Name == "i")
                        {
                            sb.Append("_" + Parse(child) + "_");
                        }
                        else if (child.Name == "b")
                        {
                            sb.Append("**" + Parse(child) + "**");
                        }
                        else if (child.Name == "pre")
                        {
                            _inPre = true;
                            sb.Append(Parse(child));
                            _inPre = false;
                        }
                        else if (child.Name == "code")
                        {
                            if (_inPre)
                            {
                                var attribute = child.Attribute("class");
                                var type = attribute == null ? null : attribute.Value;
                                if (type == "csharp")
                                    sb.Append("{% highlight csharp %}" + Parse(child) + "{% endhighlight %}");
                                else if (type == "xml")
                                    sb.Append("{% highlight xml %}" + Parse(child) + "{% endhighlight %}");
                                else
                                {
                                    var text = Parse(child).Trim().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                                    foreach (var line in text)
                                        sb.Append("    " + line + "\r\n");
                                }
                            }
                            else
                            {
                                sb.Append("`" + Parse(child) + "`");
                            }
                        }
                        else if (child.Name == "span")
                        {
                            var classAttribute = child.Attribute("class").Value;
                            if (classAttribute == "keyword" || classAttribute == "comment" || classAttribute == "string" || classAttribute == "highlight" || classAttribute == "type" ||
                                classAttribute == "Element" || classAttribute == "AttrName" || classAttribute == "AttrValue" || classAttribute == "Comment")
                            {
                                sb.Append(Parse(child));
                            }
                            else
                            {
                                throw new Exception("Unexpected span class attribute " + classAttribute);
                            }
                        }
                        else if (child.Name == "a")
                        {
                            var href = child.Attribute("href").Value;
                            sb.Append("[" + Parse(child) + "](" + href + ")");
                        }
                        else if (child.Name == "blockquote")
                        {
                            var text = Parse(child).Trim().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                            foreach (var line in text)
                                sb.Append("> " + line + "\r\n");
                        }
                        else if (child.Name == "strike")
                        {
                            sb.Append("<del>" + Parse(child) + "</del>");
                        }
                        else if (child.Name == "script")
                        {
                            sb.Append(child);
                        }
                        else if (child.Name == "iframe")
                        {
                            sb.Append(child);
                        }
                        else if (child.Name == "ol")
                        {
                            _lists.Push(true);
                            sb.Append(Parse(child));
                        }
                        else if (child.Name == "ul")
                        {
                            _lists.Push(false);
                            sb.Append(Parse(child));
                        }
                        else if (child.Name == "li")
                        {
                            if (_lists.Peek())
                            {
                                sb.Append(new string(' ', _lists.Count - 1) + "1. " + Parse(child));
                            }
                            else
                            {
                                sb.Append(new string(' ', _lists.Count - 1) + "- " + Parse(child));
                            }
                        }
                        else if (child.Name == "img")
                        {
                            sb.Append("![");
                            var alt = child.Attribute("alt");
                            if (alt != null)
                                sb.Append(alt.Value);
                            sb.Append("](" + child.Attribute("src").Value + ")");
                        }
                        else if (child.Name == "div")
                        {
                            Console.WriteLine("div: " + _published + ": " + _title);
                            if (!child.Attributes().Any())
                                sb.Append(Parse(child));
                            else
                            {
                                if (child.Attribute("class") != null && child.Attribute("class").Value == "separator")
                                {
                                    sb.Append("{:.center}\r\n");
                                    sb.Append(Parse(child));
                                }
                                else
                                {
                                    sb.Append(child);
                                }
                            }
                        }
                        else if (child.Name == "table")
                        {
                            Console.WriteLine("table: " + _published + ": " + _title);
                            _inTable = true;
                            _inTableData = false;
                            sb.Append(Parse(child));
                            _inTable = false;
                        }
                        else if (child.Name == "tr")
                        {
                            sb.Append("|" + Parse(child) + "\r\n");
                            if (!_inTableData)
                            {
                                sb.Append("|-\r\n");
                                _inTableData = true;
                            }
                        }
                        else if (child.Name == "th" || child.Name == "td")
                        {
                            sb.Append(Parse(child) + "|");
                        }
                        else if (child.Name == "caption")
                        {
                            sb.Append("{:.center.caption}\r\n");
                            sb.Append(Parse(child) + "\r\n\r\n");
                        }
                        else
                        {
                            if (!_unknownElementTypes.Contains(child.Name.ToString()))
                                Console.WriteLine("Warning: unknown element type " + child.Name);
                            _unknownElementTypes.Add(child.Name.ToString());
                            Parse(child);
                            sb.Append(child);
                        }
                    }
                    else if (node.NodeType == XmlNodeType.Text)
                    {
                        sb.Append(Escape(((XText)node).Value));
                    }
                    else if (node.NodeType == XmlNodeType.Comment)
                    {
                        sb.Append("<!--" + Escape(((XComment)node).Value) + "-->");
                    }
                    else
                    {
                        Console.WriteLine("Warning: unknown node type " + node.NodeType);
                        sb.Append(node);
                    }
                }
                return sb.ToString();
            }

            private string Escape(string value)
            {
                var result = value.Replace("\u00A0", "&nbsp;");
                if (_inTable)
                    result = result.Trim();
                return result;
                //.Replace("{", "\\{").Replace("}", "\\}").Replace("[", "\\[").Replace("]", "\\]").Replace("(", "\\(").Replace(")", "\\)")
                //.Replace("+", "\\+").Replace("-", "\\-").Replace(".", "\\.").Replace("!", "\\!"));
            }
        }
    }
}
