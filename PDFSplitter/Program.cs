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
		public static void Main(string[] args)
		{
			Console.WriteLine("PDF Splitter v1.0.0");
			var filenameWithPath = "/Users/khill/OneDrive/Documents/RPT4039.002.pdf";
			var filePath = System.IO.Path.GetDirectoryName(filenameWithPath);
			var outputPath = System.IO.Path.Combine(filePath, System.IO.Path.GetFileNameWithoutExtension(filenameWithPath));
			Directory.CreateDirectory(outputPath);
			//var textFileName = "/Users/khill/OneDrive/Documents/RPT4039.002-2.txt";
			ProcessPDF(filenameWithPath, outputPath);
			var fileResults = System.IO.Directory.GetFiles(outputPath, "*.pdf", SearchOption.TopDirectoryOnly);
			var fileGroups = fileResults.GroupBy(f => f.Split('-')[0]);
			var multiResults = fileGroups.Where(g => g.Count() > 1);
			Console.WriteLine($"{multiResults.Count()} employees had Multiple results");
			foreach (var fileGroup in multiResults)
			{
				var filesToMerge = fileGroup.Select(System.IO.Path.GetFileNameWithoutExtension)
				                            .OrderByDescending(f => f.Split('-')[1])
				                            .Select(f => System.IO.Path.Combine(outputPath,$"{f}.pdf"));
				var fileName = $"{fileGroup.Key}-MERGE.pdf";
				MergePDF(filesToMerge, fileName);
				System.IO.File.Move($"{fileGroup.Key}-1.pdf", $"{fileGroup.Key}_1.pdf");
				System.IO.File.Move(fileName, $"{fileGroup.Key}-1.pdf");
			}
			var renameFileResults = System.IO.Directory.GetFiles(outputPath, "*-1.pdf", SearchOption.TopDirectoryOnly);
			foreach (var fileRename in renameFileResults)
			{
				var newName = fileRename.Replace("-1", "");
				System.IO.File.Move(fileRename, newName);
			}
			var renameFileResults2 = System.IO.Directory.GetFiles(outputPath, "*_1.pdf", SearchOption.TopDirectoryOnly);
			foreach (var fileRename in renameFileResults2)
			{
				var newName = fileRename.Replace("_1", "-1");
				System.IO.File.Move(fileRename, newName);
			}
			//File.WriteAllText(textFileName, text);
		}

		public static void ProcessPDF(string pdfFile, string outputPath)
		{
			PdfReader reader = new PdfReader(pdfFile);

			//StringWriter output = new StringWriter();

			var regex = new Regex("(Matricule|Employee #) ([0-9]{6,})");
			var pageCount = 0;
			for (int i = 1; i <= reader.NumberOfPages; i++)
			{
				var pageText = PdfTextExtractor.GetTextFromPage(reader, i, new SimpleTextExtractionStrategy());

				var match = regex.Match(pageText);
				if (match.Success) 
				{
					var employeeNumber = match.Groups[2].Value;
					var j = 1;
					var outputFilename = System.IO.Path.Combine(outputPath, $"{employeeNumber}-{j}.pdf");
					while (File.Exists(outputFilename)) 
					{
						j++;
						outputFilename = System.IO.Path.Combine(outputPath, $"{employeeNumber}-{j}.pdf");
					}
					Console.WriteLine($"Found Employee #{employeeNumber}");
					SavePageToNewFile(reader, i, outputFilename);
					//output.WriteLine(pageText);
					pageCount++;
				}
			}
			Console.WriteLine($"Extracted {pageCount} Pages");
			//return output.ToString();
		}

		public static void SavePageToNewFile(PdfReader reader, int pageNumber, string fileName)
		{
			Document document = new Document();
			PdfCopy copy = new PdfCopy(document, new FileStream(fileName, FileMode.Create));

			document.Open();

			copy.AddPage(copy.GetImportedPage(reader, pageNumber));

			document.Close();
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
