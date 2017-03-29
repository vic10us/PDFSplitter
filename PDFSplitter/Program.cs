using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

namespace PDFSplitter
{
	class MainClass
	{
		public static Options options = new Options();

		public static void Main(string[] args)
		{
			if (!CommandLine.Parser.Default.ParseArguments(args, options))
				return;

			// Values are available here
			Console.WriteLine("PDF Splitter v1.0.0");
            if (options.Verbose) Console.WriteLine("Filename: {0}", options.InputFile);
            if (!File.Exists(options.InputFile))
			{
				Console.WriteLine($"Input file not found: '{options.InputFile}'");
				return;
			}
			var inputFile = new FileInfo(options.InputFile);
			var filenameWithPath = inputFile.FullName;
			var outputPath = GetOutputFolder();
			if (options.Overwrite && Directory.Exists(outputPath))
			{
				Directory.Delete(outputPath, true);
			}
			Directory.CreateDirectory(outputPath);
			//var textFileName = "/Users/khill/OneDrive/Documents/RPT4039.002-2.txt";
			ProcessPDF(filenameWithPath, outputPath);
			var fileResults = System.IO.Directory.GetFiles(outputPath, "*.pdf", SearchOption.TopDirectoryOnly);
			var fileGroups = fileResults.GroupBy(f => f.Split('-')[0]);
			var multiResults = fileGroups.Where(g => g.Count() > 1);
			var totalMulti = multiResults.Count();
			Console.WriteLine($"{totalMulti} employee{(totalMulti > 1 ? "s" : "")} had Multiple results");
			foreach (var fileGroup in multiResults)
			{
				var filesToMerge = fileGroup.Select(System.IO.Path.GetFileNameWithoutExtension)
				                            .OrderByDescending(f => f.Split('-')[1])
				                            .Select(f => System.IO.Path.Combine(outputPath,$"{f}.pdf"));
				var fileName = $"{fileGroup.Key}-MERGE.pdf";
				MergePDF(filesToMerge, fileName);
                File.Move($"{fileGroup.Key}-1.pdf", $"{fileGroup.Key}_1.pdf");
                File.Move(fileName, $"{fileGroup.Key}-1.pdf");
			}
			var renameFileResults = System.IO.Directory.GetFiles(outputPath, "*-1.pdf", SearchOption.TopDirectoryOnly);
			foreach (var fileRename in renameFileResults)
			{
				var newName = fileRename.Replace("-1", "");
                File.Move(fileRename, newName);
			}
			var renameFileResults2 = System.IO.Directory.GetFiles(outputPath, "*_1.pdf", SearchOption.TopDirectoryOnly);
			foreach (var fileRename in renameFileResults2)
			{
				var newName = fileRename.Replace("_1", "-1");
                File.Move(fileRename, newName);
			}
			//File.WriteAllText(textFileName, text);
		}

		public static string GetOutputFolder()
		{
			if (options.OutputSpecified) return options.Output;

			var inputFile = new FileInfo(options.InputFile);
			var filenameWithPath = inputFile.FullName;
			var filePath = System.IO.Path.GetDirectoryName(filenameWithPath);
			var outputPath = System.IO.Path.Combine(filePath, System.IO.Path.GetFileNameWithoutExtension(filenameWithPath));
			return outputPath;
		}

		public static void ProcessPDF(string pdfFile, string outputPath)
		{
			PdfReader reader = new PdfReader(pdfFile);

			var regex = new Regex(options.SplitPattern);
			var pageCount = 0;
			for (int i = 1; i <= reader.NumberOfPages; i++)
			{
				var pageText = PdfTextExtractor.GetTextFromPage(reader, i, new SimpleTextExtractionStrategy());

				var match = regex.Match(pageText);
				if (match.Success) 
				{
					var employeeNumber = match.Groups[options.FilePatternGroup].Value;
					var j = 1;
					var outputFilename = System.IO.Path.Combine(outputPath, $"{employeeNumber}-{j}.pdf");
					while (File.Exists(outputFilename)) 
					{
						j++;
						outputFilename = System.IO.Path.Combine(outputPath, $"{employeeNumber}-{j}.pdf");
					}
                    if (options.Verbose) Console.WriteLine($"Found Employee #{employeeNumber}");
					SavePageToNewFile(reader, i, outputFilename);
					pageCount++;
				}
			}
			Console.WriteLine($"Extracted {pageCount} Pages");
		}

		public static void SavePageToNewFile(PdfReader reader, int pageNumber, string fileName)
		{
            using (var stream = new FileStream(fileName, FileMode.Create))
            {
                Document document = new Document();
                try
                {
                    PdfCopy copy = new PdfCopy(document, stream);
                    document.Open();
                    copy.AddPage(copy.GetImportedPage(reader, pageNumber));
                }
                finally
                {
                    if (document != null) document.Close();
                }
            }
		}

		public static bool MergePDF(IEnumerable<string> fileNames, string targetPdf)
		{
			bool merged = true;
			using (FileStream stream = new FileStream(targetPdf, FileMode.Create))
			{
				Document document = new Document();
				PdfCopy pdf = new PdfCopy(document, stream);
				PdfReader reader = null;
				try
				{
					document.Open();
					foreach (string file in fileNames)
					{
						reader = new PdfReader(file);
						pdf.AddDocument(reader);
						reader.Close();
					}
				}
				catch (Exception)
				{
					merged = false;
					if (reader != null)
					{
						reader.Close();
					}
				}
				finally
				{
					if (document != null)
					{
						document.Close();
					}
				}
			}
			return merged;
		}
	}


}
