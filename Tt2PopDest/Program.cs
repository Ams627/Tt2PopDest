using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Tt2PopDest
{
    internal class DictUtils
    {
        public static void AddEntryToList<T, U>(Dictionary<T, Dictionary<U, int>> d, T outerKey, U innerKey)
        {
            if (!d.TryGetValue(outerKey, out var innerDict))
            {
                // there was no inner dictionary for the outer key:
                innerDict = new Dictionary<U, int>
                {
                    { innerKey, 1 }
                };
                d.Add(outerKey, innerDict);
            }
            else
            {
                // the outer key existed, but the inner dictionary doesn't contain this inner key:
                if (!innerDict.ContainsKey(innerKey))
                {
                    innerDict.Add(innerKey, 1);
                }
                else
                {
                    innerDict[innerKey]++;
                }
            }
        }

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
            var freqDict = new Dictionary<string, Dictionary<string, int>>();
            //DictUtils.AddEntryToList(freqDict, "POO", "PKS");
            //DictUtils.AddEntryToList(freqDict, "POO", "PKS");
            //DictUtils.AddEntryToList(freqDict, "POO", "PKS");
            //DictUtils.AddEntryToList(freqDict, "POO", "BSM");
            //Environment.Exit(1);
            try
            {
                var stationfilename = "w:\\StationsRefData.xml";
                var doc = XDocument.Load(stationfilename);
                var tiplocToCrs = doc.Descendants("Station")
                    .Select(x => new { Crs = x.Element("CRS")?.Value, Tiploc = x.Element("Tiploc")?.Value })
                    .Where(x => !string.IsNullOrEmpty(x.Crs) && !string.IsNullOrEmpty(x.Tiploc))
                    .ToDictionary(x => x.Tiploc, x => x.Crs);


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
                foreach (var line in File.ReadLines(filename))
                {
                    try
                    {
                        var recordType = line.Substring(0, 2);
                        if (line.Length > 2 && recordType == "TI")
                        {
                            //// this is a tiploc insert record, so store the mapping from Tiploc to CRS:
                            //var tiploc = line.Substring(2, 7);
                            //var crs = line.Substring(53, 3);
                            //if (crs.All(c => char.IsLetterOrDigit(c)))
                            //{
                            //    tiplocToCrs.Add(tiploc, crs);
                            //    if (tiploc == "WATRLMN")
                            //    {
                            //        System.Diagnostics.Debug.WriteLine($"");
                            //    }
                            //}
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
                                    var tiploc = line.Substring(2, 7).Trim();
                                    var crs = GetValueOrNull(tiplocToCrs, tiploc);
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
                    if (linenumber % 1000000 == 999999)
                    {
                        Console.WriteLine($"{linenumber + 1}");
                    }
                }

                var timetaken = DateTime.Now - startTime;
                Console.WriteLine($"count of train runs is {allRuns.Count()} - time was {timetaken.TotalSeconds} seconds");
                foreach (var run in allRuns)
                {
                    for (int origin = 0; origin < run.Count - 1; origin++)
                    {
                        for (int destination = origin + 1; destination < run.Count; destination++)
                        {
                            DictUtils.AddEntryToList(freqDict, run[origin], run[destination]);
                        }
                    }
                }
                Console.WriteLine($"Frequency dictionary size is {freqDict.Count}");
                foreach (var entry in freqDict)
                {
                    Console.WriteLine($"--- {entry.Key} ---");
                    var top5 = entry.Value.OrderByDescending(x => x.Value);
                    foreach (var top in top5)
                    {
                        Console.WriteLine($"    {top.Key} {top.Value}");
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
