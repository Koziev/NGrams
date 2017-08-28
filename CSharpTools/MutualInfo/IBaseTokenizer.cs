using System;
using System.Collections.Generic;

interface IBaseTokenizer
{
    IEnumerable<string> Tokenize(string sent);
}

