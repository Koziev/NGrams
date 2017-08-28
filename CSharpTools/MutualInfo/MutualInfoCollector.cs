using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;


class MutualInfoCollector
{
    static bool IsStopword(string w)
    {
        return char.IsPunctuation(w[0]);
    }


    Dictionary<string, int> word2index = new Dictionary<string, int>();
    List<string> all_words = new List<string>();
    int RegisterWord(string word)
    {
        int index = 0;
        if (!word2index.TryGetValue(word, out index))
        {
            index = all_words.Count;
            all_words.Add(word);
            word2index.Add(word, index);
        }


        int freq = 0;
        if (word2freq.TryGetValue(word, out freq))
        {
            word2freq[word] = freq + 1;
        }
        else
        {
            word2freq.Add(word, 1);
        }


        return index;
    }


    static UInt64 Words2Pair(int i1, int i2)
    {
        return ((UInt64)i1) | (((UInt64)i2) << 32); // вариант с сохранением порядка слов

/*
 // вариант без сохранения порядка слов
        if (i1 < i2)
        {
            return ((UInt64)i1) | (((UInt64)i2) << 32);
        }
        else
        {
            return ((UInt64)i2) | (((UInt64)i1) << 32);
        }
*/
    }

    static SoftFluent.Int128 Words2Triple(int i1, int i2, int i3)
    {
        SoftFluent.Int128 x1 = i1;
        SoftFluent.Int128 x2 = i2;
        SoftFluent.Int128 x3 = i3;

        return x1 | (x2 << 32) | (x3 << 64);

/*
        if (i1 < i2 && i2 < i3) // i1,i2,i3
        {
            return x1 | (x2 << 32) | (x3 << 64);
        }
        else if (i1 < i3 && i3 < i2) // i1,i3,i2
        {
            return x1 | (x3 << 32) | (x2 << 64);
        }
        else if (i2 < i1 && i1 < i3) // i2,i1,i3
        {
            return x2 | (x1 << 32) | (x3 << 64);
        }
        else if (i2 < i3 && i3 < i1) // i2,i3,i1
        {
            return x2 | (x3 << 32) | (x1 << 64);
        }
        else if (i3 < i1 && i1 < i2) // i3,i1,i2
        {
            return x3 | (x1 << 32) | (x2 << 64);
        }
        else // i3,i2,i1
        {
            return x3 | (x2 << 32) | (x1 << 64);
        }
*/
    }

    static SoftFluent.Int128[] ix = new SoftFluent.Int128[4];
    static SoftFluent.Int128 Words2Foursome(int i1, int i2, int i3, int i4)
    {
        ix[0] = i1;
        ix[1] = i2;
        ix[2] = i3;
        ix[3] = i4;

        return ix[0] | (ix[1] << 32) | (ix[2] << 64) | (ix[3] << 96);

        //var iy = ix.OrderBy(z => z);
        //return iy[0] | (iy[1] << 32) | (iy[2] << 64) | (iy[3] << 96);
    }

    Dictionary<string, int> word2freq = new Dictionary<string, int>();

    Dictionary<UInt64, int> pair2counts0 = new Dictionary<UInt64, int>();
    Dictionary<UInt64, int> pair2counts1 = new Dictionary<UInt64, int>();

    Dictionary<UInt64, int> pair2counts0_buf = new Dictionary<UInt64, int>();
    Dictionary<UInt64, int> pair2counts1_buf = new Dictionary<UInt64, int>();

    Dictionary<SoftFluent.Int128, int> triple2counts0 = new Dictionary<SoftFluent.Int128, int>();
    Dictionary<SoftFluent.Int128, int> triple2counts1 = new Dictionary<SoftFluent.Int128, int>();

    Dictionary<SoftFluent.Int128, int> triple2counts0_buf = new Dictionary<SoftFluent.Int128, int>();
    Dictionary<SoftFluent.Int128, int> triple2counts1_buf = new Dictionary<SoftFluent.Int128, int>();

    Dictionary<SoftFluent.Int128, int> four2counts0 = new Dictionary<SoftFluent.Int128, int>();
    Dictionary<SoftFluent.Int128, int> four2counts1 = new Dictionary<SoftFluent.Int128, int>();

    Dictionary<SoftFluent.Int128, int> four2counts0_buf = new Dictionary<SoftFluent.Int128, int>();
    Dictionary<SoftFluent.Int128, int> four2counts1_buf = new Dictionary<SoftFluent.Int128, int>();

    void FlushNGrams(int n_max_ngrams)
    {
        Console.WriteLine("\nFlushing...");

        #region PAIRS
        foreach (var q in pair2counts0_buf)
        {
            int c;
            if (pair2counts0.TryGetValue(q.Key, out c))
            {
                pair2counts0[q.Key] = c + q.Value;
            }
            else
            {
                pair2counts0.Add(q.Key, q.Value);
            }
        }

        pair2counts0_buf.Clear();


        if (pair2counts0.Count > n_max_ngrams)
        {
            Console.Write("Removing rare ngrams from pair2counts0... ");
            Dictionary<UInt64, int> dst = new Dictionary<ulong, int>();
            foreach (var q in pair2counts0.Where(z => z.Value > 1))
            {
                dst.Add(q.Key, q.Value);
            }

            Console.WriteLine("{0}->{1}", pair2counts0.Count, dst.Count);
            pair2counts0 = dst;
        }



        foreach (var q in pair2counts1_buf)
        {
            int c;
            if (pair2counts1.TryGetValue(q.Key, out c))
            {
                pair2counts1[q.Key] = c + q.Value;
            }
            else
            {
                pair2counts1.Add(q.Key, q.Value);
            }
        }

        pair2counts1_buf.Clear();

        if (pair2counts1.Count > n_max_ngrams)
        {
            Console.Write("Removing rare ngrams from pair2counts1... ");
            Dictionary<UInt64, int> dst = new Dictionary<ulong, int>();
            foreach (var q in pair2counts1.Where(z => z.Value > 1))
            {
                dst.Add(q.Key, q.Value);
            }

            Console.WriteLine("{0}->{1}", pair2counts1.Count, dst.Count);
            pair2counts1 = dst;
        }

        #endregion PAIRS

        #region TRIPLES
        foreach (var q in triple2counts0_buf)
        {
            int c;
            if (triple2counts0.TryGetValue(q.Key, out c))
            {
                triple2counts0[q.Key] = c + q.Value;
            }
            else
            {
                triple2counts0.Add(q.Key, q.Value);
            }
        }

        triple2counts0_buf.Clear();

        if (triple2counts0.Count > n_max_ngrams)
        {
            Console.Write("Removing rare ngrams from triple2counts0... ");
            Dictionary<SoftFluent.Int128, int> dst = new Dictionary<SoftFluent.Int128, int>();
            foreach (var q in triple2counts0.Where(z => z.Value > 1))
            {
                dst.Add(q.Key, q.Value);
            }

            Console.WriteLine("{0}->{1}", triple2counts0.Count, dst.Count);
            triple2counts0 = dst;
        }



        foreach (var q in triple2counts1_buf)
        {
            int c;
            if (triple2counts1.TryGetValue(q.Key, out c))
            {
                triple2counts1[q.Key] = c + q.Value;
            }
            else
            {
                triple2counts1.Add(q.Key, q.Value);
            }
        }

        triple2counts1_buf.Clear();

        if (triple2counts1.Count > n_max_ngrams)
        {
            Console.Write("Removing rare ngrams from triple2counts1... ");
            Dictionary<SoftFluent.Int128, int> dst = new Dictionary<SoftFluent.Int128, int>();
            foreach (var q in triple2counts1.Where(z => z.Value > 1))
            {
                dst.Add(q.Key, q.Value);
            }

            Console.WriteLine("{0}->{1}", triple2counts1.Count, dst.Count);
            triple2counts1 = dst;
        }

        #endregion TRIPLES

        #region FOURSOME
        foreach (var q in four2counts0_buf)
        {
            int c;
            if (four2counts0.TryGetValue(q.Key, out c))
            {
                four2counts0[q.Key] = c + q.Value;
            }
            else
            {
                four2counts0.Add(q.Key, q.Value);
            }
        }

        four2counts0_buf.Clear();

        if (four2counts0.Count > n_max_ngrams)
        {
            Console.Write("Removing rare 4-grams from four2counts0... ");
            Dictionary<SoftFluent.Int128, int> dst = new Dictionary<SoftFluent.Int128, int>();
            foreach (var q in four2counts0.Where(z => z.Value > 1))
            {
                dst.Add(q.Key, q.Value);
            }

            Console.WriteLine("{0}->{1}", four2counts0.Count, dst.Count);
            four2counts0 = dst;
        }



        foreach (var q in four2counts1_buf)
        {
            int c;
            if (four2counts1.TryGetValue(q.Key, out c))
            {
                four2counts1[q.Key] = c + q.Value;
            }
            else
            {
                four2counts1.Add(q.Key, q.Value);
            }
        }

        four2counts1_buf.Clear();

        if (four2counts1.Count > n_max_ngrams)
        {
            Console.Write("Removing rare 4-grams from triple2counts1... ");
            Dictionary<SoftFluent.Int128, int> dst = new Dictionary<SoftFluent.Int128, int>();
            foreach (var q in four2counts1.Where(z => z.Value > 1))
            {
                dst.Add(q.Key, q.Value);
            }

            Console.WriteLine("{0}->{1}", four2counts1.Count, dst.Count);
            four2counts1 = dst;
        }

        #endregion FOURSOME

        return;
    }

    int MAX_IN_BUF = 1000000;

    // --------------------------------------------------
    void StoreWordPairLink(int max_pair_count, string word1, string word2, bool linked)
    {
        int i1 = RegisterWord(word1);
        int i2 = RegisterWord(word2);

        UInt64 i12 = Words2Pair(i1, i2);

        int counts;

        if (linked)
        {
            if (pair2counts1_buf.TryGetValue(i12, out counts))
            {
                counts++;
                pair2counts1_buf[i12] = counts;
            }
            else
            {
                pair2counts1_buf.Add(i12, 1);

                if (pair2counts1_buf.Count > MAX_IN_BUF)
                {
                    FlushNGrams(max_pair_count);
                }
            }
        }
        else
        {
            if (pair2counts0_buf.TryGetValue(i12, out counts))
            {
                pair2counts0_buf[i12] = counts + 1;
            }
            else
            {
                pair2counts0_buf.Add(i12, 1);

                if (pair2counts0_buf.Count > MAX_IN_BUF)
                {
                    FlushNGrams(max_pair_count);
                }
            }
        }


        return;
    }

    int GetPairCount0(UInt64 i12)
    {
        int c0 = 0;
        pair2counts0.TryGetValue(i12, out c0);
        return c0;
    }

    int GetPairCount1(UInt64 i12)
    {
        int c1 = 0;
        pair2counts1.TryGetValue(i12, out c1);
        return c1;
    }

    int GetPairCount(UInt64 i12)
    {
        return GetPairCount0(i12) + GetPairCount1(i12);
    }

    // --------------------------------------------------

    void StoreWordTripleLink(int max_triple_count, string word1, string word2, string word3, bool linked)
    {
        int i1 = RegisterWord(word1);
        int i2 = RegisterWord(word2);
        int i3 = RegisterWord(word3);

        SoftFluent.Int128 i123 = Words2Triple(i1, i2, i3);

        int counts;

        if (linked)
        {
            if (triple2counts1_buf.TryGetValue(i123, out counts))
            {
                triple2counts1_buf[i123] = counts + 1;
            }
            else
            {
                triple2counts1_buf.Add(i123, 1);

                if (triple2counts1_buf.Count > MAX_IN_BUF)
                {
                    FlushNGrams(max_triple_count);
                }
            }
        }
        else
        {
            if (triple2counts0_buf.TryGetValue(i123, out counts))
            {
                triple2counts0_buf[i123] = counts + 1;
            }
            else
            {
                triple2counts0_buf.Add(i123, 1);

                if (triple2counts0_buf.Count > MAX_IN_BUF)
                {
                    FlushNGrams(max_triple_count);
                }
            }
        }

        return;
    }

    int GetTripleCount0(SoftFluent.Int128 i123)
    {
        int c;
        triple2counts0.TryGetValue(i123, out c);
        return c;
    }

    int GetTripleCount1(SoftFluent.Int128 i123)
    {
        int c;
        triple2counts1.TryGetValue(i123, out c);
        return c;
    }

    int GetTripleCount(SoftFluent.Int128 i123)
    {
        return GetTripleCount0(i123) + GetTripleCount1(i123);
    }

    // --------------------------------------------------

    void StoreWordFourLink(int max_four_count, string word1, string word2, string word3, string word4, bool linked)
    {
        int i1 = RegisterWord(word1);
        int i2 = RegisterWord(word2);
        int i3 = RegisterWord(word3);
        int i4 = RegisterWord(word4);

        SoftFluent.Int128 i1234 = Words2Foursome(i1, i2, i3, i4);

        int counts;

        if (linked)
        {
            if (four2counts1_buf.TryGetValue(i1234, out counts))
            {
                four2counts1_buf[i1234] = counts + 1;
            }
            else
            {
                four2counts1_buf.Add(i1234, 1);

                if (four2counts1_buf.Count > MAX_IN_BUF)
                {
                    FlushNGrams(max_four_count);
                }
            }
        }
        else
        {
            if (four2counts0_buf.TryGetValue(i1234, out counts))
            {
                four2counts0_buf[i1234] = counts + 1;
            }
            else
            {
                four2counts0_buf.Add(i1234, 1);

                if (four2counts0_buf.Count > MAX_IN_BUF)
                {
                    FlushNGrams(max_four_count);
                }
            }
        }

        return;
    }

    int GetFourCount0(SoftFluent.Int128 i1234)
    {
        int c;
        four2counts0.TryGetValue(i1234, out c);
        return c;
    }

    int GetFourCount1(SoftFluent.Int128 i1234)
    {
        int c;
        four2counts1.TryGetValue(i1234, out c);
        return c;
    }

    int GetFourCount(SoftFluent.Int128 i1234)
    {
        return GetFourCount0(i1234) + GetFourCount1(i1234);
    }

    // --------------------------------------------------

    static int GetHiWordIndex(UInt64 pair)
    {
        return (int)(UInt32)(pair >> 32);
    }

    static int GetLoWordIndex(UInt64 pair)
    {
        return (int)(UInt32)(pair);
    }

    // ---------------------------------------------------------------------------------------------
    Dictionary<UInt64, float> pair2proximity = new Dictionary<ulong, float>();
    Dictionary<UInt64, Int64> pair2count = new Dictionary<ulong, long>();

    public void StorePairProximity(int max_pair_count, string word1, string word2, float proximity)
    {
        int i1 = RegisterWord(word1);
        int i2 = RegisterWord(word2);

        UInt64 i12 = Words2Pair(i1, i2);

        float sum_prox = 0;
        if (!pair2proximity.TryGetValue(i12, out sum_prox))
        {
            // TODO: можно убрать пары с единичной встречаемостью ...

            if (pair2count.Count < max_pair_count)
            {
                pair2count.Add(i12, 1);
                pair2proximity.Add(i12, proximity);
            }
        }
        else
        {
            pair2proximity[i12] = sum_prox + proximity;
            pair2count[i12] = pair2count[i12] + 1;
        }

        return;
    }

    // ---------------------------------------------------------------------------------------------

    Dictionary<SoftFluent.Int128, float> triple2proximity = new Dictionary<SoftFluent.Int128, float>();
    Dictionary<SoftFluent.Int128, Int64> triple2count = new Dictionary<SoftFluent.Int128, long>();
    public void StoreTripleProximity(int max_triple_count, string word1, string word2, string word3, float proximity)
    {
        int i1 = RegisterWord(word1);
        int i2 = RegisterWord(word2);
        int i3 = RegisterWord(word3);

        SoftFluent.Int128 i123 = Words2Triple(i1, i2, i3);

        float sum_prox = 0;
        if (!triple2proximity.TryGetValue(i123, out sum_prox))
        {
            // TODO: можно убрать тройки с единичной встречаемостью, если предел кол-ва достигнут ...

            if (triple2count.Count < max_triple_count)
            {
                triple2count.Add(i123, 1);
                triple2proximity.Add(i123, proximity);
            }
        }
        else
        {
            triple2proximity[i123] = sum_prox + proximity;
            triple2count[i123] = triple2count[i123] + 1;
        }

        return;
    }

    // ---------------------------------------------------------------------------------------------

    Dictionary<SoftFluent.Int128, float> four2proximity = new Dictionary<SoftFluent.Int128, float>();
    Dictionary<SoftFluent.Int128, Int64> four2count = new Dictionary<SoftFluent.Int128, long>();
    public void StoreFourProximity(int max_four_count, string word1, string word2, string word3, string word4, float proximity)
    {
        int i1 = RegisterWord(word1);
        int i2 = RegisterWord(word2);
        int i3 = RegisterWord(word3);
        int i4 = RegisterWord(word4);

        SoftFluent.Int128 i1234 = Words2Foursome(i1, i2, i3, i4);

        float sum_prox = 0;
        if (!four2proximity.TryGetValue(i1234, out sum_prox))
        {
            if (four2count.Count < max_four_count)
            {
                four2count.Add(i1234, 1);
                four2proximity.Add(i1234, proximity);
            }
        }
        else
        {
            four2proximity[i1234] = sum_prox + proximity;
            four2count[i1234] = four2count[i1234] + 1;
        }

        return;
    }

    // ---------------------------------------------------------------------------------------------

    public void StorePairsProximityDataset(string result_path)
    {
        Console.WriteLine("Storing {0} pairs as dataset {1}...", pair2count.Count, result_path);

        double N2 = pair2count.Select(z => (double)z.Value).Sum(); // общая частота всех пар
        double N1 = word2freq.Select(z => (double)z.Value).Sum(); // общая частота всех слов

        List<Tuple<UInt64, float>> pair_mi = new List<Tuple<ulong, float>>();
        foreach (UInt64 pair in (pair2count.Select(z => z.Key)))
        {
            float n12 = pair2proximity[pair];

            int i1 = (int)(UInt32)(pair);
            int i2 = (int)(UInt32)(pair >> 32);

            // mutual information для этой пары слов
            double a = n12 / N2;

            double f1 = word2freq[all_words[i1]] / N1;
            double f2 = word2freq[all_words[i2]] / N1;
            double mutual_information = a * Math.Log(a / (f1 * f2));

            pair_mi.Add(new Tuple<ulong, float>(pair, (float)mutual_information));
        }


        using (System.IO.StreamWriter wrt = new System.IO.StreamWriter(result_path))
        {
            foreach (var rec in pair_mi.OrderByDescending(z => z.Item2))
            {
                UInt64 pair = rec.Item1;
                float n12 = pair2proximity[pair];

                int i1 = (int)(UInt32)(pair);
                int i2 = (int)(UInt32)(pair >> 32);

                double mutual_information = rec.Item2;

                wrt.WriteLine("{0}\t{1}\t{2}", all_words[i1], all_words[i2]
                                                , mutual_information.ToString(System.Globalization.CultureInfo.InvariantCulture)
                                             );
            }
        }

        return;
    }

    public void StoreTriplesProximityDataset(string result_path)
    {
        Console.WriteLine("Storing {0} triples as dataset {1}...", triple2count.Count, result_path);

        double N3 = triple2count.Select(z => (double)z.Value).Sum(); // общая частота всех троек
        double N1 = word2freq.Select(z => (double)z.Value).Sum(); // общая частота всех слов

        List<Tuple<SoftFluent.Int128, float>> triple_mi = new List<Tuple<SoftFluent.Int128, float>>();
        foreach (SoftFluent.Int128 triple in (triple2count.Select(z => z.Key)))
        {
            float n123 = triple2proximity[triple];

            int i3 = (int)(triple >> 64).GetLow32();
            int i2 = (int)(triple >> 32).GetLow32();
            int i1 = (int)(triple).GetLow32();

            // mutual information для этой пары слов
            double a = n123 / N3;

            double f1 = word2freq[all_words[i1]] / N1;
            double f2 = word2freq[all_words[i2]] / N1;
            double f3 = word2freq[all_words[i3]] / N1;
            double mutual_information = a * Math.Log(a / (f1 * f2 * f3));

            triple_mi.Add(new Tuple<SoftFluent.Int128, float>(triple, (float)mutual_information));
        }


        using (System.IO.StreamWriter wrt = new System.IO.StreamWriter(result_path))
        {
            foreach (var rec in triple_mi.OrderByDescending(z => z.Item2))
            {
                SoftFluent.Int128 triple = rec.Item1;
                float n123 = triple2proximity[triple];

                int i3 = (int)(triple >> 64).GetLow32();
                int i2 = (int)(triple >> 32).GetLow32();
                int i1 = (int)(triple).GetLow32();

                double mutual_information = rec.Item2;

                wrt.WriteLine("{0}\t{1}\t{2}\t{3}", all_words[i1], all_words[i2], all_words[i3]
                                                , mutual_information.ToString(System.Globalization.CultureInfo.InvariantCulture)
                                             );
            }
        }

        return;
    }

    public void StoreFoursProximityDataset(string result_path)
    {
        Console.WriteLine("Storing {0} quadruples as dataset {1}...", four2count.Count, result_path);

        double N4 = four2count.Select(z => (double)z.Value).Sum(); // общая частота всех четверок
        double N1 = word2freq.Select(z => (double)z.Value).Sum(); // общая частота всех слов

        List<Tuple<SoftFluent.Int128, float>> four_mi = new List<Tuple<SoftFluent.Int128, float>>();
        foreach (SoftFluent.Int128 four in (four2count.Select(z => z.Key)))
        {
            float n1234 = four2proximity[four];

            int i4 = (int)(four >> 96).GetLow32();
            int i3 = (int)(four >> 64).GetLow32();
            int i2 = (int)(four >> 32).GetLow32();
            int i1 = (int)(four).GetLow32();

            // mutual information для этой пары слов
            double a = n1234 / N4;

            double f1 = word2freq[all_words[i1]] / N1;
            double f2 = word2freq[all_words[i2]] / N1;
            double f3 = word2freq[all_words[i3]] / N1;
            double f4 = word2freq[all_words[i4]] / N1;
            double mutual_information = a * Math.Log(a / (f1 * f2 * f3 * f4));

            four_mi.Add(new Tuple<SoftFluent.Int128, float>(four, (float)mutual_information));
        }


        using (System.IO.StreamWriter wrt = new System.IO.StreamWriter(result_path))
        {
            foreach (var rec in four_mi.OrderByDescending(z => z.Item2))
            {
                SoftFluent.Int128 four = rec.Item1;
                float n1234 = four2proximity[four];

                int i4 = (int)(four >> 96).GetLow32();
                int i3 = (int)(four >> 64).GetLow32();
                int i2 = (int)(four >> 32).GetLow32();
                int i1 = (int)(four).GetLow32();

                double mutual_information = rec.Item2;

                wrt.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", all_words[i1], all_words[i2], all_words[i3], all_words[i4]
                                                , mutual_information.ToString(System.Globalization.CultureInfo.InvariantCulture)
                                             );
            }
        }

        return;
    }

    public void PurgeRarePairs()
    {
        // Оставим пары с частотой > 1
        Dictionary<UInt64, long> pair2count_new = new Dictionary<ulong, long>();
        Dictionary<UInt64, float> pair2proximity_new = new Dictionary<ulong, float>();

        foreach (var q in pair2count.Where(z => z.Value > 1))
        {
            pair2count_new.Add(q.Key, q.Value);
            pair2proximity_new.Add(q.Key, pair2proximity[q.Key]);
        }

        pair2count = pair2count_new;
        pair2proximity = pair2proximity_new;

        return;
    }

    public void PurgeRareTriples()
    {
        // Оставим тройки с частотой > 1
        Dictionary<SoftFluent.Int128, long> triple2count_new = new Dictionary<SoftFluent.Int128, long>();
        Dictionary<SoftFluent.Int128, float> triple2proximity_new = new Dictionary<SoftFluent.Int128, float>();

        foreach (var q in triple2count.Where(z => z.Value > 1))
        {
            triple2count_new.Add(q.Key, q.Value);
            triple2proximity_new.Add(q.Key, triple2proximity[q.Key]);
        }

        triple2count = triple2count_new;
        triple2proximity = triple2proximity_new;

        return;
    }

    public void PurgeRareFours()
    {
        // Оставим частотой с частотой > 1
        Dictionary<SoftFluent.Int128, long> four2count_new = new Dictionary<SoftFluent.Int128, long>();
        Dictionary<SoftFluent.Int128, float> four2proximity_new = new Dictionary<SoftFluent.Int128, float>();

        foreach (var q in four2count.Where(z => z.Value > 1))
        {
            four2count_new.Add(q.Key, q.Value);
            four2proximity_new.Add(q.Key, four2proximity[q.Key]);
        }

        four2count = four2count_new;
        four2proximity = four2proximity_new;

        return;
    }


    public int GetPairsCount() => pair2count.Count;
    public int GetTriplesCount() => triple2count.Count;
    public int GetFoursomeCount() => four2count.Count;
}



