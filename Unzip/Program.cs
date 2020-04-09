using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using static System.Console;

namespace Unzip
{
	class Program
	{
		static string inputFile;
		static string outputFolder;
		static bool listFiles;

		static void Main(string[] args)
		{
			try
			{
				if (!ParseArgs(args))
				{
					WriteLine("Unzip.exe InputFile OutputDir [-l]");
				}
				else
				{
					using (var rawInput = File.OpenRead(inputFile))
					using (var zip = new ZipFile(rawInput))
					{
						foreach (ZipEntry entry in zip)
						{
							string entryName = entry.Name;
							string scrubbedName = Scrub(entryName);

							if (listFiles)
							{
								WriteLine(entryName);
								if (entryName != scrubbedName && entryName != scrubbedName.Replace('\\', '/'))
								{
									WriteLine("\tScrubbed to: " + scrubbedName);
								}
							}
							else
							{
								using (Stream zipStream = zip.GetInputStream(entry))
								{
									string outputFileName = Path.Combine(outputFolder, scrubbedName);
									Directory.CreateDirectory(Path.GetDirectoryName(outputFileName));

									using (Stream output = File.Create(outputFileName))
									{
										zipStream.CopyTo(output);
									}
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				WriteLine(ex);
				if (Debugger.IsAttached)
				{
					WriteLine("Press any key to end the process...");
					ReadKey(true);
				}
			}
		}

		static bool ParseArgs(string[] args)
		{
			bool result = false;

			if (args.Length >= 2)
			{
				inputFile = args[0];
				outputFolder = args[1];

				result = File.Exists(inputFile);

				if (args.Length == 3)
				{
					listFiles = string.Equals(args[2], "-l");
				}
			}

			return result;
		}

		static string Scrub(string name)
		{
			string result = name;

			char[] invalid = Path.GetInvalidFileNameChars();
			if (name.IndexOfAny(invalid) >= 0)
			{
				StringBuilder sb = new StringBuilder(name.Length);
				foreach (char ch in name)
				{
					switch (ch)
					{
						case '/':
						case '\\':
							sb.Append('\\');
							break;

						case '"':
							sb.Append('\'');
							break;

						case ':':
							sb.Append('.');
							break;

						case '_':
							sb.Append(' ');
							break;

						default:
							if (invalid.Contains(ch))
							{
								// TODO: Do better substitution.
								sb.Append('~');
							}
							else
							{
								sb.Append(ch);
							}

							break;
					}
				}

				result = sb.ToString();
			}

			return result;
		}
	}
}
