# GT Utilities
These are C# utilities and PowerShell scripts I created and maintained between August 2016 and December 2019 when I was in [Georgia Tech's OMSCS program](https://www.omscs.gatech.edu/). They're used to convert [Udacity](https://www.udacity.com/)'s .srt subtitle files into English transcriptions as more human-readable HTML files. They can also produce a summary list of video counts and times.

Udacity's video and subtitle .zips usually:
* Lump everything into a single folder
* Have some duplicate files with slightly different names
* Have some missing files
* Contain some non-English translations
* Use characters that aren't valid in Windows file names (e.g., * when talking about A* search, :, ?)

So, every semester I had to do a lot of re-organization and clean-up of the .srt files to generate the transcriptions. It was a manual process to prepare everything for my Transcriber utility, but the PowerShell scripts helped some. It took me at least 30 minutes of work every semester to get things nice and ready for my Transcriber code, but the end result was usually searchable, readable transcription files.

If you run Transcriber.exe with no arguments it outputs this: 

```
You must specify an input path.
Transcriber InputPath [OutputFormat] [OutputPath] [SingleOutputPerFolder] [BreakBetweenSentences]
  OutputFormat = Text, Markdown, or Html.  Default is Text.
  OutputPath defaults to the current directory.
  SingleOutputPerFolder and BreakBetweenSentences must be True or False.  Both default to False.
 ```

To generate HTML transcriptions I typically used a command line like this (where my Subtitles folder already had the .srt files extracted into subfolders):

```
Transcriber.exe "C:\Projects\GT\MLT\Subtitles" Html "C:\Projects\GT\MLT\Transcriptions" True True
```

The code's not a model of how to develop long-term-maintainable software, but it has the [Works On My Machine](https://blog.codinghorror.com/the-works-on-my-machine-certification-program/) certification. It was good enough for my 10 classes, and [I got out](https://en.wikipedia.org/wiki/Traditions_of_the_Georgia_Institute_of_Technology#Getting_Out).
