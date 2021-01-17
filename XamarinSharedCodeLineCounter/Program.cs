using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace XamarinSharedCodeLineCounter {

    internal class App {

        private static void Main(string[] args) {

            if(args == null || args.Length < 1) {
                System.Diagnostics.Debug.WriteLine("\nIn App.Main() - args is null or empty\n");
                return;
            }

            string path = args[0];//Environment.CurrentDirectory;

            //for(int i = 0; i < 3; i++) { 
            //    path = Path.Combine(Path.GetDirectoryName(path), string.Empty);
            //}

            string mainFolderName = path.Split('\\').Last();
            
            List<Solution> projects = new List<Solution> {
			
				new Solution {
					Name         = "Android",
					ProjectFiles = new List<string> {
						Path.Combine(path, mainFolderName + ".Droid/" + mainFolderName + ".Droid.csproj"),
						Path.Combine(path, mainFolderName + "/"       + mainFolderName + ".csproj")
					}
				},

				new Solution {
					Name         = "iOS",
					ProjectFiles = new List<string> {
						Path.Combine(path, mainFolderName + ".iOS/" + mainFolderName + ".iOS.csproj"),
						Path.Combine(path, mainFolderName + "/"     + mainFolderName + ".csproj")
					}
				}//,

                //new Solution {
                //    Name         = "WinPhone",
                //    ProjectFiles = new List<string> {
                //        Path.Combine(path, "APA.MAP.WinPhone/APA.MAP.WinPhone.csproj"),
                //        Path.Combine(path, "APA.MAP/APA.MAP.csproj")
                //    }
                //}
			};

            new App().Run(projects);
        }

        private class Solution {

            public string         Name         = "";
            public List<string>   ProjectFiles = new List<string>();
            public List<FileInfo> CodeFiles    = new List<FileInfo>();

            public override string ToString() { return Name; }

            public int UniqueLinesOfCode {
                get { return (from f in CodeFiles where f.Solutions.Count == 1 select f.LinesOfCode).Sum(); }
            }

            public int SharedLinesOfCode {
                get { return (from f in CodeFiles where f.Solutions.Count > 1 select f.LinesOfCode).Sum(); }
            }

            public int TotalLinesOfCode {
                get { return (from f in CodeFiles select f.LinesOfCode).Sum(); }
            }
        }

        private class FileInfo {
            public string Path = "";
            
            public List<Solution> Solutions = new List<Solution>();
            
            public int LinesOfCode;

            public override string ToString() { return Path; }
        }

        private Dictionary<string, FileInfo> _files = new Dictionary<string, FileInfo>();

        private void AddRef(string path, Solution sln) {

            if(_files.ContainsKey(path)) {
                _files[path].Solutions.Add(sln);
                sln.CodeFiles.Add(_files[path]);
            } else {
                FileInfo info = new FileInfo { Path = path };
                info.Solutions.Add(sln);
                _files[path] = info;
                sln.CodeFiles.Add(info);
            }
        }

        private void Run(List<Solution> solutions) {
            //
            // Find all the files
            //
            foreach(Solution sln in solutions) {
                foreach(string projectFile in sln.ProjectFiles) {

                    string    dir         = Path.GetDirectoryName(projectFile);
                    //string    projectName = Path.GetFileNameWithoutExtension(projectFile);
                    XDocument doc         = XDocument.Load(projectFile);

                    IEnumerable<string> q = 
                            from   x in doc.Descendants()
                            let    e = x// as XElement
                            where  e != null
                            where  e.Name.LocalName == "Compile"
                            where  e.Attributes().Any(a => a.Name.LocalName == "Include")
                            select e.Attribute("Include").Value;

                    foreach(string inc in q) {
                        //skip over some things that are added automatically
                        if(inc.Contains("Resource.designer.cs") ||
                           inc.Contains("Resource.Designer.cs") ||
                           inc.Contains("DebugTrace.cs") ||
                           inc.Contains("LinkerPleaseInclude.cs") ||
                           inc.Contains("AssemblyInfo.cs") ||
                           inc.Contains("Bootstrap.cs") ||
                           inc.Contains(".designer.cs") ||
                           inc.Contains("App.xaml.cs") ||
                           inc.EndsWith(".xaml") ||
                           inc.EndsWith(".xml") ||
                           inc.EndsWith(".axml")) { continue; }

                        string inc2 = inc.Replace("\\", Path.DirectorySeparatorChar.ToString());
                        AddRef(Path.GetFullPath(Path.Combine(dir, inc2)), sln);
                    }
                }
            }

            //
            // Get the lines of code
            //
            foreach(FileInfo f in _files.Values) {
                try {
                    List<string> lines = File.ReadAllLines(f.Path).ToList();

                    f.LinesOfCode = lines.Count;
                } catch(Exception ex) { System.Diagnostics.Debug.WriteLine("\nIn App.Run() - Exception: " + ex + "\n"); }
            }

            //
            // Output
            //
            System.Diagnostics.Debug.WriteLine("App\t\tTotal Lines\t\tUnique Lines\t\tShared Lines\t\tUnique %\t\tShared %");
            foreach(Solution sln in solutions) {

                System.Diagnostics.Debug.WriteLine("{0}\t{1}\t\t\t{2}\t\t\t\t{3}\t\t\t\t{4:p}\t\t\t{5:p}",
                    sln.Name == "iOS" ? sln.Name + "\t" : sln.Name,
                    sln.TotalLinesOfCode,
                    sln.UniqueLinesOfCode,
                    sln.SharedLinesOfCode,
                    sln.UniqueLinesOfCode / (double)sln.TotalLinesOfCode,
                    sln.SharedLinesOfCode / (double)sln.TotalLinesOfCode);
            }

            System.Diagnostics.Debug.WriteLine("DONE");
        }
    }
}