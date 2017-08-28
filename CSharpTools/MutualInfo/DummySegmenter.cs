using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


class DummySegmenter : IBaseSegmenter
{
    public DummySegmenter()
    { }

    public IEnumerable<string> Split(string text)
    {
        yield return text;
    }
}
