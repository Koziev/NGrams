// Сборка mutual information для пар, троек и четверок слов по сырым текстам.
// Собранная статистика сохраняется в виде файлов, пригодных для использования
// питоновскими моделями на сетках.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;


class CollectMutualInfo
{
    static void CollectLinkabilityFromEols(string[] args)
    {
        #region CommandLineOptions
        List<string> files = new List<string>();
        string result_path = null;
        bool collect_pairs = false;
        bool collect_triples = false;
        bool collect_fours = false;
        int N_MAX_NGRAMS = int.MaxValue;
        int WINDOW = 5; // макс. расстояние между словами.
        int MAX_SENT_COUNT = int.MaxValue; // макс. число обрабатываемых предложений

        IBaseSegmenter segmenter = null;
        IBaseTokenizer tokenizer = null;

        for (int i = 0; i < args.Length; ++i)
        {
            if (args[i] == "-raw")
            {
                segmenter = new Segmenter();
            }
            else if (args[i] == "-eols")
            {
                segmenter = new DummySegmenter();
            }
            else if (args[i] == "-o")
            {
                result_path = args[i + 1];
                i++;
            }
            else if (args[i] == "-win")
            {
                WINDOW = int.Parse(args[i + 1]);
                i++;
            }
            else if (args[i] == "-i")
            {
                string p = args[i + 1];
                i++;

                if (p.Contains('*'))
                {
                    string folder = System.IO.Path.GetDirectoryName(p);
                    string filemask = System.IO.Path.GetFileName(p);

                    foreach (var f in System.IO.Directory.GetFiles(folder, filemask))
                    {
                        if (!files.Contains(f))
                        {
                            files.Add(f);
                        }
                    }
                }
                else
                {
                    if (!files.Contains(p))
                    {
                        files.Add(p);
                    }
                }
            }
            else if (args[i] == "-maxngrams")
            {
                N_MAX_NGRAMS = int.Parse(args[i + 1]);
                i++;
            }
            else if (args[i] == "-maxsentcount")
            {
                MAX_SENT_COUNT = int.Parse(args[i + 1]);
                i++;
            }
            else if (args[i] == "linkability2")
            {
                // будем сохранять в датасете вероятность связывания слов в парах
                collect_pairs = true;
            }
            else if (args[i] == "linkability3")
            {
                // будем сохранять в датасете вероятность связывания слов в тройках
                collect_triples = true;
            }
            else if (args[i] == "linkability4")
            {
                // будем сохранять в датасете вероятность связывания слов в четверках
                collect_fours = true;
            }
            else if (args[i] == "-eols")
            {
            }
            else
            {
                throw new ArgumentException(string.Format("Unknown option {0}", args[i]));
            }
        }

        if (tokenizer == null)
        {
            tokenizer = new Tokenizer();
        }

        #endregion CommandLineOptions

        #region CollectingTheStat

        MutualInfoCollector ngrams = new MutualInfoCollector();

        long total_sent_count = 0;
        int sent_count = 0;

        // для удобства визуального мониторинга подсчитаем общее кол-во предложений в обрабатываемом корпусе.
        Console.WriteLine("Counting the total number of lines in source texts...");
        foreach (string text_path in files)
        {
            using (System.IO.StreamReader rdr = new System.IO.StreamReader(text_path))
            {
                while (!rdr.EndOfStream)
                {
                    string line = rdr.ReadLine();
                    if (line == null) break;
                    total_sent_count++;
                }
            }
        }
        Console.WriteLine("total_sent_count={0}", total_sent_count);


        // Теперь сбор статистики
        foreach (string text_path in files)
        {
            Console.WriteLine("Processing {0}...", text_path);

            using (System.IO.StreamReader rdr = new System.IO.StreamReader(text_path))
            {
                while (!rdr.EndOfStream)
                {
                    string text = rdr.ReadLine();
                    if (text == null) break;

                    if (sent_count >= MAX_SENT_COUNT)
                    {
                        Console.WriteLine("Max allowed number of sentence {0} reached, so stopping.", MAX_SENT_COUNT);
                        break;
                    }

                    foreach (string line in segmenter.Split(text))
                    {
                        string[] words = tokenizer.Tokenize(line).ToArray();
                        int nword = words.Length;

                        if (collect_pairs)
                        {
                            for (int i1 = 0; i1 < words.Length - 1; ++i1)
                            {
                                int m2 = i1 + WINDOW + 1;
                                for (int i2 = i1 + 1; i2 < words.Length && i2 < m2; ++i2)
                                {
                                    float proximity = (float)Math.Exp(-Math.Abs(i2 - i1 - 1));
                                    ngrams.StorePairProximity(N_MAX_NGRAMS, words[i1], words[i2], proximity);
                                }
                            }
                        }
                        else if (collect_triples)
                        {
                            for (int i1 = 0; i1 < nword - 1; ++i1)
                            {
                                int m2 = i1 + WINDOW + 1;
                                for (int i2 = i1 + 1; i2 < nword && i2 < m2; ++i2)
                                {
                                    int m3 = i2 + WINDOW + 1;
                                    for (int i3 = i2 + 1; i3 < nword && i3 < m3; ++i3)
                                    {
                                        float dist = Math.Abs(i2 - i1) + Math.Abs(i3 - i2) - 2;
                                        float proximity = (float)Math.Exp(-dist);
                                        ngrams.StoreTripleProximity(N_MAX_NGRAMS, words[i1], words[i2], words[i3], proximity);
                                    }
                                }
                            }
                        }
                        else if (collect_fours)
                        {
                            for (int i1 = 0; i1 < nword - 1; ++i1)
                            {
                                int m2 = i1 + WINDOW + 1;
                                for (int i2 = i1 + 1; i2 < nword && i2 < m2; ++i2)
                                {
                                    int m3 = i2 + WINDOW + 1;
                                    for (int i3 = i2 + 1; i3 < nword && i3 < m3; ++i3)
                                    {
                                        int m4 = i3 + WINDOW + 1;
                                        for (int i4 = i3 + 1; i4 < nword && i4 < m4; ++i4)
                                        {
                                            float dist = Math.Abs(i2 - i1) + Math.Abs(i3 - i2) + Math.Abs(i4 - i3) - 3;
                                            float proximity = (float)Math.Exp(-dist);
                                            ngrams.StoreFourProximity(N_MAX_NGRAMS, words[i1], words[i2], words[i3], words[i4], proximity);
                                        }
                                    }
                                }
                            }
                        }

                        sent_count++;

                        if ((sent_count % 10000) == 0)
                        {
                            if (collect_pairs)
                            {
                                Console.Write("sent_count={0}/{1}\t\tpairs={2}\r",
                                    sent_count.ToString("#,##0", System.Globalization.CultureInfo.InvariantCulture),
                                    total_sent_count.ToString("#,##0", System.Globalization.CultureInfo.InvariantCulture),
                                    ngrams.GetPairsCount().ToString("n0", System.Globalization.CultureInfo.InvariantCulture));
                            }
                            else if (collect_triples)
                            {
                                Console.Write("sent_count={0}/{1}\t\ttriples={2}\r",
                                    sent_count.ToString("#,##0", System.Globalization.CultureInfo.InvariantCulture),
                                    total_sent_count.ToString("#,##0", System.Globalization.CultureInfo.InvariantCulture),
                                    ngrams.GetTriplesCount().ToString("n0", System.Globalization.CultureInfo.InvariantCulture));
                            }
                            else if (collect_fours)
                            {
                                Console.Write("sent_count={0}/{1}\t\tquadruples={2}\r",
                                    sent_count.ToString("#,##0", System.Globalization.CultureInfo.InvariantCulture),
                                    total_sent_count.ToString("#,##0", System.Globalization.CultureInfo.InvariantCulture),
                                    ngrams.GetFoursomeCount().ToString("n0", System.Globalization.CultureInfo.InvariantCulture));
                            }



                            if ((sent_count % 1000000) == 0)
                            {
                                // Сохраняем промежуточные результаты, чтобы можно было оценить текущее качество, не
                                // дожидаясь обработки целого корпуса.
                                Console.WriteLine("");

                                if (collect_pairs)
                                {
                                    ngrams.PurgeRarePairs();
                                    ngrams.StorePairsProximityDataset(result_path);
                                }
                                else if (collect_triples)
                                {
                                    ngrams.PurgeRareTriples();
                                    ngrams.StoreTriplesProximityDataset(result_path);
                                }
                                else if (collect_fours)
                                {
                                    ngrams.PurgeRareFours();
                                    ngrams.StoreFoursProximityDataset(result_path);
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine("");
        }

        Console.WriteLine("\nAll treebanks are processed.");

        #endregion CollectingTheStat

        #region DumpResults
        if (collect_pairs)
        {
            ngrams.StorePairsProximityDataset(result_path);
        }
        else if (collect_triples)
        {
            ngrams.StoreTriplesProximityDataset(result_path);
        }
        else if (collect_fours)
        {
            ngrams.StoreFoursProximityDataset(result_path);
        }
        #endregion DumpResults

        Console.WriteLine("All done.");

        return;
    }


    static void Main(string[] args)
    {
        CollectLinkabilityFromEols(args);
        return;
    }
}



