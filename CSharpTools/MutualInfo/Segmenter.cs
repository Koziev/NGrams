using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


class Segmenter : IBaseSegmenter
{
    private System.Text.RegularExpressions.Regex rx_split = new System.Text.RegularExpressions.Regex("([.?!] )");
    public Segmenter()
    {}

    public IEnumerable<string> Split(string text)
    {
        string[] lines = rx_split.Split(text);
        foreach (string line in lines)
        {
            if (line.Length > 1 && !".?!".Contains(line[0]))
                yield return line;
        }
    }
}
