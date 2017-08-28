using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class Tokenizer : IBaseTokenizer
{
    private System.Text.RegularExpressions.Regex rx_splitter;

    private string Normalize(string word)
    {
        return word.ToLower();
    }


    public Tokenizer()
    {
        rx_splitter = new System.Text.RegularExpressions.Regex("\\W");
    }

    public IEnumerable<string> Tokenize(string sent)
    {
        // string[] words = line.ToLower().Split(" ,.?!-:\t".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

        foreach (string token in rx_splitter.Split(sent))
        {
            if (token.Length > 0)
            {
                yield return Normalize(token);
            }
        }
    }
}
