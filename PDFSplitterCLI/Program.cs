using System;
using System.IO;
using PDFSplitter.Core;
using Path = System.IO.Path;

namespace PDFSplitterCLI
{
    public class Program
	{
		public static Options Options = new Options();

		public static void Main(string[] args)
		{
			if (!CommandLine.Parser.Default.ParseArguments(args, Options))
				return;
            
			// Values are available here
			Console.WriteLine("PDF Splitter v1.0.0");
            if (Options.Verbose) Console.WriteLine("Filename: {0}", Options.InputFile);
            if (!File.Exists(Options.InputFile))
			{
				Console.WriteLine($"Input file not found: '{Options.InputFile}'");
				return;
			}
			var inputFile = new FileInfo(Options.InputFile);
			var filenameWithPath = inputFile.FullName;
			var outputPath = GetOutputFolder();
			if (Options.Overwrite && Directory.Exists(outputPath))
			{
				Directory.Delete(outputPath, true);
			}
			Directory.CreateDirectory(outputPath);

            var utils = new Utils(m =>
            {
                Console.WriteLine($"{m.Message}  [{m.Progress}%]");
            })
            {
                Settings = new Settings
                {
                    FilePatternGroup = Options.FilePatternGroup,
                    SplitPattern = Options.SplitPattern
                }
            };
            utils.ProcessAndSplit(filenameWithPath, outputPath);
		}
        
	    public static string GetOutputFolder()
		{
			if (Options.OutputSpecified) return Options.Output;

			var inputFile = new FileInfo(Options.InputFile);
			var filenameWithPath = inputFile.FullName;
			var filePath = Path.GetDirectoryName(filenameWithPath);
			var outputPath = Path.Combine(filePath ?? "", Path.GetFileNameWithoutExtension(filenameWithPath));
			return outputPath;
		}
	}


}
