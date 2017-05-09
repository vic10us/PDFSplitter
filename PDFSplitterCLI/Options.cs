using CommandLine;
using CommandLine.Text;

namespace PDFSplitterCLI
{
	public class Options
	{
		[Option('r', "read", Required = true,
		  HelpText = "Input file to be processed.")]
		public string InputFile { get; set; }

		[Option('v', "verbose", DefaultValue = true,
		  HelpText = "Prints all messages to standard output.")]
		public bool Verbose { get; set; }

		[Option('p', "pattern", DefaultValue = "(Matricule|Employee #) ([0-9]{6,})",
		  HelpText = "Page detection pattern.")]
		public string SplitPattern { get; set; }

		[Option('g', "group", DefaultValue = 2,
		  HelpText = "Pattern match group used for filename.")]
		public int FilePatternGroup { get; set; }

		[Option('f', "overwrite", DefaultValue = false,
		  HelpText = "Force overwrite of the output.")]
		public bool Overwrite { get; set; }

		[Option('o', "output", DefaultValue = null,
		  HelpText = "Output folder.")]
		public string Output { get; set; }

		public bool OutputSpecified => !string.IsNullOrEmpty(Output);

		[ParserState]
		public IParserState LastParserState { get; set; }

		[HelpOption]
		public string GetUsage()
		{
			return HelpText.AutoBuild(this,
			  (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
		}
	}
}
