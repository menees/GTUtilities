using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Console;

namespace Transcriber
{
	class Program
	{
		// For .srt files we could use ^\d\d:\d\d:\d\d,\d\d\d --> \d\d:\d\d:\d\d,\d\d\d$
		// But for WEBVTT files, we'll be more generic.
		private static readonly Regex TimeRange = new Regex(@"^(?ni)[\d:,.]+ --> (?<end>[\d:,.]+)$");

		private static string inputPath;
		private static OutputFormat outputFormat;
		private static string outputPath;
		private static bool singleOutputPerFolder;
		private static bool breakBetweenSentences;

		private enum OutputFormat
		{
			Text,
			Markdown,
			Html
		}

		static void Main(string[] args)
		{
			try
			{
				if (ParseArgs(args))
				{
					var filesPerFolder = Directory.EnumerateFiles(inputPath, "*.srt", SearchOption.AllDirectories)
						.GroupBy(file => Path.GetDirectoryName(file));

					TimeSpan totalTime = TimeSpan.Zero;
					int totalCount = 0;
					List<string> folderSummaries = new List<string>();
					foreach (var group in filesPerFolder)
					{
						TimeSpan folderTime = ProcessFolder(
							group.Key,
							group.OrderBy(file => GetSortableFileName(Path.GetFileName(file)), StringComparer.CurrentCulture));

						int count = group.Count();
						folderSummaries.Add($"{Path.GetFileName(group.Key)}\t{count}\t{folderTime}");
						totalTime += folderTime;
						totalCount += count;
					}

					WriteLine();
					WriteLine("Lesson\tCount\tTime");
					foreach (string summary in folderSummaries.OrderBy(item => item))
					{
						WriteLine(summary);
					}

					WriteLine();
					WriteLine($"Total\t{totalCount}\t{totalTime}");
				}
			}
			catch (Exception ex)
			{
				WriteLine(ex.ToString());
			}

			if (Debugger.IsAttached)
			{
				WriteLine("Press any key to end...");
				ReadKey(true);
			}
		}

		private static bool ParseArgs(IList<string> args)
		{
			bool result = false;
			outputPath = Directory.GetCurrentDirectory();

			const string Usage = "Transcriber InputPath [OutputFormat] [OutputPath] [SingleOutputPerFolder] [BreakBetweenSentences]\r\n"
				+ "  OutputFormat = Text, Markdown, or Html.  Default is Text.\r\n"
				+ "  OutputPath defaults to the current directory.\r\n"
				+ "  SingleOutputPerFolder and BreakBetweenSentences must be True or False.  Both default to False.";

			if (args.Count < 1)
			{
				WriteLine("You must specify an input path.");
				WriteLine(Usage);
			}
			else
			{
				inputPath = args[0];
				if (!Directory.Exists(inputPath))
				{
					WriteLine("The specified input path does not exist.");
					WriteLine(Usage);
				}
				else
				{
					result = true;
					if (args.Count >= 2)
					{
						if (!Enum.TryParse(args[1], true, out outputFormat))
						{
							result = false;
							WriteLine("Invalid OutputFormat: " + args[1]);
							WriteLine(Usage);
						}

						if (args.Count >= 3)
						{
							outputPath = args[2];

							if (args.Count >= 4)
							{
								if (!bool.TryParse(args[3], out singleOutputPerFolder))
								{
									result = false;
									WriteLine("Invalid True/False value for SingleOutputPerFolder: " + args[3]);
									WriteLine(Usage);
								}
								else if (args.Count >= 5)
								{
									if (!bool.TryParse(args[4], out breakBetweenSentences))
									{
										result = false;
										WriteLine("Invalid True/False value for BreakBetweenSentences: " + args[4]);
										WriteLine(Usage);
									}
								}
							}
						}
					}
				}
			}

			return result;
		}

		private static TimeSpan ProcessFolder(string inputFolder, IEnumerable<string> inputFiles)
		{
			string relativeFolder = inputFolder.Substring(inputPath.Length);
			if (relativeFolder.StartsWith(@"\"))
			{
				relativeFolder = relativeFolder.Substring(1);
			}

			string outputFolder = Path.Combine(outputPath, relativeFolder);

			TimeSpan totalTime = TimeSpan.Zero;
			if (singleOutputPerFolder)
			{
				string outputFileName = Path.GetFileName(outputFolder);
				if (!string.IsNullOrEmpty(relativeFolder))
				{
					outputFolder = Path.GetDirectoryName(outputFolder);
				}

				Directory.CreateDirectory(outputFolder);
				using (TextWriter writer = CreateOutputFile(outputFolder, outputFileName))
				{
					foreach (string file in inputFiles)
					{
						totalTime += ProcessFile(writer, file);
					}

					WriteTime(writer, "Total Time", totalTime);
					FinishOutputFile(writer);
				}
			}
			else
			{
				Directory.CreateDirectory(outputFolder);
				foreach (string file in inputFiles)
				{
					using (TextWriter writer = CreateOutputFile(outputFolder, Path.GetFileNameWithoutExtension(file)))
					{
						totalTime += ProcessFile(writer, file);
						FinishOutputFile(writer);
					}
				}
			}

			return totalTime;
		}

		private static TextWriter CreateOutputFile(string outputFolder, string outputFileNameNoExt)
		{
			string fullOutputName = Path.Combine(outputFolder, outputFileNameNoExt);
			switch (outputFormat)
			{
				case OutputFormat.Text:
					fullOutputName += ".txt";
					break;

				case OutputFormat.Markdown:
					fullOutputName += ".md";
					break;

				case OutputFormat.Html:
					fullOutputName += ".html";
					break;
			}

			// Use UTF8 with byte-order marks because some .srt files use non-ASCII chars (e.g., smart quotes).
			// Without the byte-order marks a single smart quote will be decoded as three funky ASCII chars.
			StreamWriter result = new StreamWriter(fullOutputName, false, Encoding.UTF8);

			if (outputFormat == OutputFormat.Html)
			{
				result.WriteLine("<!doctype html>");
				result.WriteLine("<!-- saved from url=(0014)about:internet -->");
				result.WriteLine("<html>");
				result.WriteLine("<head>");
				result.WriteLine("<style>");
				result.WriteLine("* {font-family: Calibri; }");
				result.WriteLine("</style>");
				result.WriteLine("<meta http-equiv='Content-Type' content='text/html; charset=windows-1252'>");
				result.WriteLine("</head>");
				result.WriteLine("<body>");
			}

			return result;
		}

		private static TimeSpan ProcessFile(TextWriter writer, string inputFile)
		{
			WriteLine("Processing " + inputFile);

			if (singleOutputPerFolder)
			{
				string headerText = Path.GetFileNameWithoutExtension(inputFile);
				switch (outputFormat)
				{
					case OutputFormat.Text:
						headerText += Environment.NewLine + new string('-', headerText.Length);
						break;

					case OutputFormat.Markdown:
						headerText = "## " + headerText;
						break;

					case OutputFormat.Html:
						headerText = "<h2>" + headerText + "</h2>";
						break;
				}

				writer.WriteLine(headerText);
			}

			if (outputFormat == OutputFormat.Html)
			{
				writer.WriteLine("<p>");
			}

			bool firstLine = true;
			string[] lines = File.ReadAllLines(inputFile);
			string previousLine = null;
			TimeSpan endTime = TimeSpan.Zero;
			foreach (string rawLine in lines)
			{
				string line = rawLine;

				// https://en.wikipedia.org/wiki/Timed_text#Competing_formats
				Match match = TimeRange.Match(line);
				if (match.Success)
				{
					string timeText = match.Groups[1].Value.Replace(',', '.');
					if (TimeSpan.TryParse(timeText, out TimeSpan time) && time > endTime)
					{
						// In the AI4R/RAIT lectures "09:59:59,000" is used 234 times as the final time in the video.
						// Since none of the videos are actually 10 hours long, I'm going to ignore that time and
						// treat it as just a few extra seconds on the last line.
						if (time == TimeSpan.FromHours(10).Subtract(TimeSpan.FromSeconds(1)))
						{
							endTime += TimeSpan.FromSeconds(5);
						}
						else
						{
							endTime = time;
						}
					}
				}
				else if (!int.TryParse(line, out int subtitleCounter) && line != "WEBVTT")
				{
					line = line.Replace("&gt;", ">").Replace("<i>", "").Replace("</i>", "");

					// Seen in CPSS .srt files.
					const string NarratorPrefix = "(narrator) ";
					if (line.StartsWith(NarratorPrefix))
					{
						line = line.Substring(NarratorPrefix.Length);
					}

					if (string.IsNullOrWhiteSpace(line))
					{
						WriteBreakBetweenSentences(writer, previousLine, false);
					}
					else
					{
						const string SpeakerChangePrefix = ">> ";
						if (line.StartsWith(SpeakerChangePrefix))
						{
							if (!firstLine)
							{
								WriteBreak(writer, false);
							}

							writer.WriteLine(line.Substring(SpeakerChangePrefix.Length));
						}
						else
						{
							writer.WriteLine(line);
						}

						firstLine = false;
					}
				}

				previousLine = line;
			}

			WriteBreakBetweenSentences(writer, previousLine, true);

			if (endTime > TimeSpan.Zero)
			{
				endTime = TimeSpan.FromSeconds(Math.Ceiling(endTime.TotalSeconds));
				WriteTime(writer, "Time", endTime);
			}

			if (singleOutputPerFolder || outputFormat == OutputFormat.Html)
			{
				WriteBreak(writer, true);
			}

			return endTime;
		}

		private static void WriteBreakBetweenSentences(TextWriter writer, string previousLine, bool endOfFile)
		{
			if (breakBetweenSentences)
			{
				char previousEnd = string.IsNullOrEmpty(previousLine) ? '\0' : previousLine[previousLine.Length - 1];
				if (endOfFile || previousEnd == '.' || previousEnd == '?' || previousEnd == '!')
				{
					WriteBreak(writer, false);
				}
			}
		}

		private static void WriteTime(TextWriter writer, string label, TimeSpan time)
		{
			string output = $"{label}: {time}";
			switch (outputFormat)
			{
				case OutputFormat.Html:
					writer.Write("<i>");
					writer.Write(output);
					writer.WriteLine("</i>");
					break;

				case OutputFormat.Markdown:
					writer.Write("*");
					writer.Write(output);
					writer.WriteLine("*");
					break;

				default:
					writer.WriteLine(output);
					break;
			}
		}

		private static void WriteBreak(TextWriter writer, bool isFinal)
		{
			switch (outputFormat)
			{
				case OutputFormat.Text:
				case OutputFormat.Markdown:
					writer.WriteLine();
					break;

				case OutputFormat.Html:
					if (isFinal)
					{
						writer.WriteLine("</p>");
					}
					else
					{
						writer.WriteLine("</p><p>");
					}

					break;
			}
		}

		private static void FinishOutputFile(TextWriter writer)
		{
			if (outputFormat == OutputFormat.Html)
			{
				writer.WriteLine("</body>");
				writer.WriteLine("</html>");
			}
		}

		private static string GetSortableFileName(string file)
		{
			string result = file;

			int spaceIndex = file.IndexOfAny(new[] { ' ', '-' });
			if (spaceIndex > 0 && int.TryParse(file.Substring(0, spaceIndex), out int fileNumber))
			{
				result = $"{fileNumber:0000}{file.Substring(spaceIndex)}";
			}

			return result;
		}
	}
}
