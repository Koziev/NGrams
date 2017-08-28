using System;
using System.Collections.Generic;


interface IBaseSegmenter
{
    IEnumerable<string> Split(string text);
}

