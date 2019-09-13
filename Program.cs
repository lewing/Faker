using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;


public class Faker {
    class Section {
        public string type { get; set; }
        public string name { get; set; }
        internal string body { get; set; }
        public int size { get; set; }
        public List<Section> sections { get; set; }
        public override string ToString () => $"{name} = {size}";
    }

    public static void Main (string[] args) {
        //var regex = new Regex(@"(^.assembly .*?)\n+\{\n(.*?)^\}\n*", RegexOptions.Multiline | RegexOptions.Singleline);
        var classRegex = new Regex (@"^[.](class) (.*?)\n+\{\n(.*?)^\}\n*", RegexOptions.Multiline | RegexOptions.Singleline);
        var methodRegex = new Regex (@"^  [.]method ([^\{]*)\{[^\/]*// Code size\s*(\d+)",RegexOptions.Multiline | RegexOptions.Singleline);
        ///var regex = new Regex(@"^[.]([\w]+) (.*?)\n+\{\n(.*?)^\}\n*", RegexOptions.Multiline | RegexOptions.Singleline);
        var methodNameRegex = new Regex (@"^(\w* )*(.*) cil managed");
        var assemblyRegex = new Regex(@"^[.](assembly) (.*?)\n+\{\n(.*?)^\}\n*", RegexOptions.Multiline | RegexOptions.Singleline);
        
        var assemblies = args.Select (arg => {
            var data = File.ReadAllText (arg);
            var name = assemblyRegex.Matches(data).Select ((Match m) => m.Groups[2].ToString()).First (n => !n.Contains ("extern"));
            
            var sections = new List<Section>();
            var matches = classRegex.Matches (data);
            foreach (Match match in matches) {
                var c = new Section { 
                    type = match.Groups [1].ToString (),
                    name = Regex.Replace (match.Groups [2].ToString (), @"\s+", " "),
                    body = match.Groups [3].ToString ()
                };
                var methods = methodRegex.Matches (c.body);
                c.sections = methods.Select ((Match method) => {
                    return new Section {
                        type = "method",
                        name = methodNameRegex.Matches (Regex.Replace (method.Groups [1].ToString (), @"\s+", " ")).Select ((Match n) => $"{n.Groups[1]}{n.Groups[2]}").First(),
                        size = Int32.Parse (method.Groups [2].ToString ())
                    };
                }).ToList ();
                c.size = c.sections.Any() ? c.sections.Select (s => s.size).Aggregate ((total, item) => total + item) : 0;
                sections.Add (c);
            }

            return new Section {
                type = "assembly",
                name = name,
                sections = sections,
                size = sections.Select (s => s.size).Aggregate ((total, item) => total + item)
            };
        }).ToList ();
           
        var options = new JsonSerializerOptions {
                WriteIndented = true
        };
        var text = JsonSerializer.Serialize(assemblies, options);
        Console.WriteLine (text);
    }
}