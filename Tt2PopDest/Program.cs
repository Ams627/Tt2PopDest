using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tt2PopDest
{
    internal class DictUtils
    {

    }
    internal class Program
    {
        static V GetValueOrNull<K, V>(Dictionary<K, V> dict, K key)
        {
            V result = default;
            if (dict.TryGetValue(key, out var value))
            {
                result = value;
            }
            return result;
        }
        private static void Main(string[] args)
        {
            try
            {
                var tipLocToCrs = new Dictionary<string, string>();
                var startTime = DateTime.Now;
                var filename = "s:\\ttisf772.mca";
                var count = 0;
                var linenumber = 0;
                bool isRecordInDate = false;
                var allRuns = new List<List<string>>();
                var oneRun = new List<string>();
                var crsCompressor = new CrsCodec();
                var minDate = DateTime.Today;
                var maxDate = DateTime.Today;
                var freqDict = new Dictionary<string, Dictionary<string, int>>();
                foreach (var line in File.ReadLines(filename))
                {
                    try
                    {
                        var recordType = line.Substring(0, 2);
                        if (line.Length > 2 && recordType == "TI")
                        {
                            // this is a tiploc insert record, so store the mapping from Tiploc to CRS:
                            var tiploc = line.Substring(2, 7);
                            var crs = line.Substring(53, 3);
                            if (crs.All(c => char.IsLetterOrDigit(c)))
                            {
                                tipLocToCrs.Add(tiploc, crs);
                            }
                        }
                        else if (recordType == "BS")
                        {
                            // This is a basic schedule record:
                            var bs = new BSRecord(line);
                            if (bs.RunsTo > DateTime.Today)
                            {
                                // This record is valid for the current date:
                                isRecordInDate = true;
                                count++;
                            }
                        }
                        if (isRecordInDate)
                        {
                            // check for origin, intermediate or terminating record:
                            if (recordType == "LO" || recordType == "LI" || recordType == "LT")
                            {
                                if (!(recordType == "LI" && line.Substring(15, 4).All(char.IsWhiteSpace)))
                                {
                                    // the record is not a "station passing" record:
                                    var tiploc = line.Substring(2, 7);
                                    var crs = GetValueOrNull(tipLocToCrs, tiploc);
                                    if (crs != null)
                                    {
                                        oneRun.Add(crs);
                                    }
                                }
                            }
                            if (recordType == "LT")
                            {
                                isRecordInDate = false;
                                var newlist = new List<string>();
                                newlist.AddRange(oneRun);
                                allRuns.Add(newlist);
                                oneRun.Clear();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error at line {linenumber + 1} : {e.Message}");
                    }
                    linenumber++;
                    if (linenumber % 1000 == 999)
                    {
                        Console.WriteLine($"{linenumber + 1}");
                    }
                }

                var timetaken = DateTime.Now - startTime;
                Console.WriteLine($"count of train runs is {allRuns.Count()} - time was {timetaken.TotalSeconds} seconds");
                foreach (var run in allRuns)
                {
                    for (int i = 0; i < run.Count - 1; i++)
                    {
                        for (int j = i + 1; j < count; j++)
                        {

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var codeBase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
                var progname = Path.GetFileNameWithoutExtension(codeBase);
                Console.Error.WriteLine(progname + ": Error: " + ex.Message);
            }

        }
    }
}
