// Лемматизация текстового корпуса.
//
// На входе берется текстовый файл в кодировке utf-8. Каждая строка этого файла загружается отдельно,
// бьется на предложения. Затем выполняется частеречный анализ. Леммы слов для всех предложений в строке
// сохраняются в одной строке выходного файла.
//
// Для выполнения частеречного анализа используется предварительно обученная вероятностная модель, которая
// присваивает метки части речи каждому слову в предложении, таким образом снимая почти все неоднозначности
// при лемматизации.
//
// (c) Koziev Ilya inkoziev@gmail.com https://github.com/Koziev

using System;
using System.Collections.Generic;
using SolarixGrammarEngineNET;
using System.Diagnostics;

class Program
{
    static void LemmatizeTextFile(string dictionary_xml, string corpus_path, string result_path)
    {
        int LanguageID = SolarixGrammarEngineNET.GrammarEngineAPI.RUSSIAN_LANGUAGE;
        // int Constraints = 60000 | (30 << 22); // 1 минута и 30 альтернатив

        using (GrammarEngine2 gren = new GrammarEngine2())
        {
            gren.Load(DictionaryXmlPath: dictionary_xml, LazyLexicon: true);
            Console.WriteLine($"Словарь {dictionary_xml} успешно загружен");

            int pronoun_class = gren.FindPartOfSpeech("МЕСТОИМЕНИЕ");
            Debug.Assert(pronoun_class != -1);
            int person_coord = gren.FindCoord("ЛИЦО");
            Debug.Assert(person_coord != -1);
            int person1 = gren.FindState(person_coord, "1");
            int person2 = gren.FindState(person_coord, "2");
            int person3 = gren.FindState(person_coord, "3");

            Dictionary<int, string> id_entry2lemma = new Dictionary<int, string>();

            using (System.IO.StreamWriter wrt = new System.IO.StreamWriter(result_path))
            {
                int line_count = 0;
                using (System.IO.StreamReader rdr = new System.IO.StreamReader(corpus_path))
                {
                    while (!rdr.EndOfStream)
                    {
                        string line = rdr.ReadLine();
                        if (line == null) break;

                        line_count++;
                        if ((line_count % 100) == 0)
                        {
                            Console.Write($"{line_count} lines parsed\r");
                        }

                        List<string> sentences = gren.SplitText(line, LanguageID);

                        List<string> line_lemmas = new List<string>(capacity: line.Length / 5 + 1);

                        foreach (string sentence in sentences)
                        {
                            AnalysisResults anares = gren.AnalyzeMorphology(sentence, LanguageID, SolarixGrammarEngineNET.GrammarEngine.MorphologyFlags.SOL_GREN_MODEL_ONLY);
                            for (int itoken = 1; itoken < anares.Count - 1; ++itoken)
                            {
                                SyntaxTreeNode node = anares[itoken];
                                string lemma = null;
                                int id_entry = node.GetEntryID();
                                if (id_entry != -1)
                                {
                                    if (pronoun_class == gren.GetEntryClass(id_entry))
                                    {
                                        // В словарной базе Solarix местоимения НАС, ВАМИ etc. приводятся к базовой форме Я
                                        // Таким образом, словоизменение по лицам устраняется. Такая агрессивная лемматизация
                                        // высокочастотных слов может быть нежелательна для семантического анализа, поэтому
                                        // сделаем ручную лемматизацию.
                                        int person = node.GetCoordState(person_coord);
                                        if (person == person1)
                                        {
                                            lemma = "я";
                                        }
                                        else if (person == person2)
                                        {
                                            lemma = "ты";
                                        }
                                        else if (person == person3)
                                        {
                                            lemma = "он";
                                        }
                                        else
                                        {
                                            Debug.Fail("Unknown person tag");
                                        }
                                    }
                                    else
                                    {
                                        if (id_entry2lemma.ContainsKey(id_entry))
                                        {
                                            lemma = id_entry2lemma[id_entry];
                                        }
                                        else
                                        {

                                            lemma = gren.GetEntryName(id_entry);
                                            if (lemma.Equals("unknownentry", StringComparison.OrdinalIgnoreCase))
                                            {
                                                lemma = node.GetWord();
                                            }
                                            else if (lemma.Equals("???"))
                                            {
                                                lemma = node.GetWord();
                                            }
                                            else
                                            {
                                                lemma = lemma.Replace(' ', '_'); // для MWU типа "в конце концов"
                                            }
                                        }

                                        id_entry2lemma[id_entry] = lemma;
                                    }
                                }
                                else
                                {
                                    lemma = node.GetWord();
                                }

                                lemma = lemma.ToLower();
                                line_lemmas.Add(lemma);
                            }
                        }

                        wrt.WriteLine(string.Join(" ", line_lemmas));
                    }
                }
            }
        }

        return;
    }

    static int Main(string[] args)
    {
        string dictionary_xml = args[0]; // пути к словарной базе под Windows
        string corpus_path = args[1]; // текстовый файл, содержимое которого будем лемматизировать
        string result_path = args[2]; // сюда запишем результат лемматизации

        LemmatizeTextFile(dictionary_xml, corpus_path, result_path);

        Console.WriteLine($"Corpus {corpus_path} has been lemmatized.");
        return 0;
    }
}
