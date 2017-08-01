using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Path = System.IO.Path;

namespace PDFSplitter.Core
{
    public class Settings
    {
        public string SplitPattern { get; set; } = "(Matricule|Employee #) ([0-9]{6,})";
        public int FilePatternGroup { get; set; } = 2;
    }

    public class ProcessMessage
    {
        public string Message { get; set; }
        public int Progress { get; set; }
    }

    public class Utils
    {
        private readonly Action<ProcessMessage> _logger;
        public Settings Settings = new Settings();

        public Utils(Action<ProcessMessage> logger)
        {
            _logger = logger;
        }

        public Utils()
        {
            _logger = s => { };
        }

        public async Task<bool> ProcessAndSplitAsync(string filenameWithPath, string outputPath)
        {
            var t = await Task<bool>.Factory.StartNew(() =>
            {
                ProcessAndSplit(filenameWithPath, outputPath);
                return true;
            });
            return t;
        }

        public void ProcessAndSplit(string filenameWithPath, string outputPath)
        {
            ProcessPDF(filenameWithPath, outputPath);
            var fileResults = Directory.GetFiles(outputPath, "*.pdf", SearchOption.TopDirectoryOnly);
            var fileGroups = fileResults.GroupBy(f => f.Split('-')[0]);
            var multiResults = fileGroups.Where(g => g.Count() > 1).ToList();
            var totalMulti = multiResults.Count;
            _logger.Invoke(new ProcessMessage { Message = $"{totalMulti} employee{(totalMulti > 1 ? "s" : "")} had Multiple results", Progress = 100});
            foreach (var fileGroup in multiResults)
            {
                var filesToMerge = fileGroup.Select(Path.GetFileNameWithoutExtension)
                    .OrderByDescending(f => f.Split('-')[1])
                    .Select(f => Path.Combine(outputPath, $"{f}.pdf"));
                var fileName = $"{fileGroup.Key}-MERGE.pdf";
                MergePDF(filesToMerge, fileName);
                File.Move($"{fileGroup.Key}-1.pdf", $"{fileGroup.Key}_1.pdf");
                File.Move(fileName, $"{fileGroup.Key}-1.pdf");
            }
            var renameFileResults = Directory.GetFiles(outputPath, "*-1.pdf", SearchOption.TopDirectoryOnly);
            foreach (var fileRename in renameFileResults)
            {
                var newName = fileRename.Replace("-1", "");
                File.Move(fileRename, newName);
            }
            var renameFileResults2 = Directory.GetFiles(outputPath, "*_1.pdf", SearchOption.TopDirectoryOnly);
            foreach (var fileRename in renameFileResults2)
            {
                var newName = fileRename.Replace("_1", "-1");
                File.Move(fileRename, newName);
            }
            var renameFileResults3 = Directory.GetFiles(outputPath, "*-*.pdf", SearchOption.TopDirectoryOnly);
            if (!renameFileResults3.Any()) return;

            var specialPath = Path.Combine(outputPath, "MergeOriginals");
            Directory.CreateDirectory(specialPath);
            foreach (var s in renameFileResults3)
            {
                var newFile = s.Replace(outputPath, specialPath);
                File.Move(s, newFile);
            }
        }

        public void ProcessPDF(string pdfFile, string outputPath)
        {
            var reader = new PdfReader(pdfFile);

            var regex = new Regex(Settings.SplitPattern);
            var pageCount = 0;
            for (var i = 1; i <= reader.NumberOfPages; i++)
            {
                var pageText = PdfTextExtractor.GetTextFromPage(reader, i, new SimpleTextExtractionStrategy());

                var match = regex.Match(pageText);
                if (!match.Success) continue;
                var employeeNumber = match.Groups[Settings.FilePatternGroup].Value;
                var j = 1;
                var outputFilename = Path.Combine(outputPath, $"{employeeNumber}-{j}.pdf");
                while (File.Exists(outputFilename))
                {
                    j++;
                    outputFilename = Path.Combine(outputPath, $"{employeeNumber}-{j}.pdf");
                }
                _logger.Invoke(new ProcessMessage { Message = $"Found Employee #{employeeNumber}", Progress = (int)(1.0 * i / reader.NumberOfPages * 100.0) });
                SavePageToNewFile(reader, i, outputFilename);
                pageCount++;
            }
            _logger.Invoke(new ProcessMessage { Message = $"Extracted {pageCount} Pages", Progress = 100 });
        }

        public void SavePageToNewFile(PdfReader reader, int pageNumber, string fileName)
        {
            using (var stream = new FileStream(fileName, FileMode.Create))
            {
                var document = new Document();
                try
                {
                    var copy = new PdfCopy(document, stream);
                    document.Open();
                    copy.AddPage(copy.GetImportedPage(reader, pageNumber));
                }
                finally
                {
                    document.Close();
                }
            }
        }
        
        public bool MergePDF(IEnumerable<string> fileNames, string targetPdf)
        {
            var merged = true;
            using (var stream = new FileStream(targetPdf, FileMode.Create))
            {
                var document = new Document();
                var pdf = new PdfCopy(document, stream);
                PdfReader reader = null;
                try
                {
                    document.Open();
                    foreach (var file in fileNames)
                    {
                        reader = new PdfReader(file);
                        pdf.AddDocument(reader);
                        reader.Close();
                    }
                }
                catch (Exception)
                {
                    merged = false;
                    reader?.Close();
                }
                finally
                {
                    document.Close();
                }
            }
            return merged;
        }
    }
}
