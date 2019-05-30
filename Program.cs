using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FirmwareParserFinal
{
    class PageInfo
    {
        public int InfoStartAt { get; set; }
        public int DataStartAt { get; set; }
        public int DataEndAt { get; set; }
        public int PageNumber { get; set; }
        public byte[] Bytes { get; set; }
    }

    class Firmware
    {
        Regex info = new Regex(@"page\s+0x([0-9a-f]+):");
        Regex data = new Regex(@"\s*([0-9a-f]{2})\s*");

        public Firmware(string text)
        {
            Text = text;
        }

        public string Text { get; }

        public IEnumerable<PageInfo> GetNextPage()
        {
            int startAt = 0;

            Match infoMatch = info.Match(Text, startAt);

            while (infoMatch.Success)
            {
                var currentPageInfo = new PageInfo
                {
                    InfoStartAt = infoMatch.Index,
                    DataStartAt = infoMatch.Index + infoMatch.Length,
                    PageNumber = int.Parse(infoMatch.Groups[1].Value, NumberStyles.HexNumber),
                };

                startAt = currentPageInfo.DataStartAt;

                List<byte> bytes = new List<byte>();
                int bytePositionCount = currentPageInfo.DataStartAt;

                var byteMatch = data.Match(Text, bytePositionCount);

                while (byteMatch.Success)
                {
                    if (byteMatch.Index != bytePositionCount)
                        break;

                    bytes.Add(byte.Parse(byteMatch.Groups[1].Value, NumberStyles.HexNumber));
                    bytePositionCount = byteMatch.Groups[1].Index + byteMatch.Groups[1].Length;

                    byteMatch = data.Match(Text, bytePositionCount);
                }

                currentPageInfo.Bytes = bytes.ToArray();

                infoMatch = info.Match(Text, startAt);
                yield return currentPageInfo;
            }
        }
    }

    class Program
    {
        const int pageSize = 2048;
        static void Main(string[] args)
        {
            SortedDictionary<int, PageInfo> pages = new SortedDictionary<int, PageInfo>();

            // Remove CRLF => no hassles with RegEx
	    foreach(var file in args)
	    {
            	var firmware = new Firmware(File.ReadAllText(file).Replace("\r\n", " "));

	        foreach (var item in firmware.GetNextPage())
        	{
        		if (item.Bytes.Length == pageSize)
        	        {
        	            pages[item.PageNumber] = item;
        	        }
        	        else
        	        {
        	            Console.WriteLine($"Wrong number of bytes @ page: {item.PageNumber}");
        	        }
        	}
	    }
            List<int> missingPages = new List<int>();
            int? lastPage = null;

            using (var dump = File.OpenWrite("finaldump.bin"))
            {
                foreach (var v in pages)
                {
                    if (lastPage.HasValue)
                    {
                        if (v.Key != lastPage + 1)
                        {
			    Console.WriteLine($"Gap between {v.Key} and {lastPage}");
                            missingPages.AddRange(Enumerable.Range(lastPage.Value + 1, v.Key - lastPage.Value));
                        }
                    }
		    lastPage = v.Key;
                    dump.Write(v.Value.Bytes, 0, v.Value.Bytes.Length);
                }
            }
            Console.ReadKey();
        }
    }
}
