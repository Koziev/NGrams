using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace W2V_Lib
{
    public class W2V_Model
    {
        Dictionary<string, long> token2index;
        Dictionary<string, float[]> token2vector = new Dictionary<string, float[]>();

        System.IO.FileStream vector_reader;

        bool lazy_load;
        int nword, veclen;

        string txt_vectors_path;

        static byte[] read_buffer;

        static string ReadLine(System.IO.FileStream rdr)
        {
            if (read_buffer == null)
            {
                read_buffer = new byte[65000];
            }

            int line_len = 0;
            int bufpos = 0;
            while (true)
            {
                int b = rdr.ReadByte();
                if (b == -1)
                {
                    break;
                }
                else if (b == 10)
                {
                    break;
                }
                else
                {
                    read_buffer[bufpos++] = (byte)b;
                    line_len++;
                }
            }

            return System.Text.Encoding.UTF8.GetString(read_buffer, 0, line_len);
        }


        public W2V_Model(string txt_vectors_path, bool lazy_load)
        {
            this.txt_vectors_path = txt_vectors_path;
            this.lazy_load = lazy_load;
            token2index = new Dictionary<string, long>();

            vector_reader = new System.IO.FileStream(txt_vectors_path, System.IO.FileMode.Open, System.IO.FileAccess.Read);

            string[] toks = ReadLine(vector_reader).Split(' ');
            nword = int.Parse(toks[0]);
            veclen = int.Parse(toks[1]);

            for (int i = 0; i < nword; ++i)
            {
                long pos = vector_reader.Position;
                string line = ReadLine(vector_reader);
                if (string.IsNullOrEmpty(line))
                    break;

                string[] tx = line.Split(' ');
                string word = tx[0];

                if (lazy_load)
                {
                    token2index[word] = pos;
                }
                else
                {
                    float[] v = new float[veclen];
                    for (int j = 0; j < veclen; ++j)
                    {
                        v[j] = float.Parse(tx[j + 1], System.Globalization.CultureInfo.InvariantCulture);
                    }

                    token2vector[word] = v;
                }
            }

            if (!lazy_load)
            {
                vector_reader.Close();
                vector_reader = null;
            }
        }

        public int GetVectorLen() { return veclen; }


        public bool ContainsWord(string normalized_token)
        {
            return token2index.ContainsKey(normalized_token);
        }

        public float[] this[string token]
        {
            get
            {
                if (lazy_load)
                {
                    float[] vec = null;

                    if( token2vector.TryGetValue(token, out vec) )
                    {
                        return vec;
                    }

                    vec = new float[veclen];
                    Array.Clear(vec, 0, veclen);

                    System.Globalization.NumberFormatInfo nfi = new System.Globalization.NumberFormatInfo();
                    nfi.NumberDecimalSeparator = ".";

                    long vecpos;
                    if (token2index.TryGetValue(token, out vecpos))
                    {
                        vector_reader.Position = vecpos;

                        string line = ReadLine(vector_reader);
                        string[] toks = line.Split(' ');

                        for (int i = 0; i < veclen; ++i)
                        {
                            string xi = toks[i + 1];
                            vec[i] = float.Parse(xi, nfi);
                        }
                    }

                    token2vector[token] = vec;
                    return vec;
                }
                else
                {
                    return token2vector[token];
                }
            }
        }


        static string Undress(string word)
        {
            string w = word;

            char[] front = "«`'\"".ToCharArray();
            char[] rear = "»`'\"".ToCharArray();

            if (front.Contains(w[0]))
                w = w.Substring(1);

            if (w.Length > 2 && rear.Contains(w[w.Length - 1]))
                w = w.Substring(0, w.Length - 1);

            return w;
        }


        static System.Text.RegularExpressions.Regex rx_romnum = new System.Text.RegularExpressions.Regex("[IXVCMLD]+");
        public static string Normalize(string word)
        {
            if (string.IsNullOrEmpty(word))
                return word;
            else if (char.IsDigit(word[0]))
                return "_num_";
            else if (rx_romnum.Match(word).Success)
                return "_num_";
            /*
            else if (word.Length > 1 && char.IsUpper(word[0]) && char.IsUpper(word[1]))
            {
                return Undress(word).Replace('ё', 'е');
            }*/
            else
            {
                return Undress(word).Replace(" ", "_").Replace("-", "_").ToLower().Replace('ё', 'е');
            }
        }

        public static bool IsStopWord(string word)
        {
            return char.IsPunctuation(word[0]) || "«»;:`~^.,?><[]{}-()=+*&%$#@'\"!|/\\".Contains(word[0]);
        }


        public List<KeyValuePair<string, float>> Match(float[] v)
        {
            List<KeyValuePair<string, float>> res = new List<KeyValuePair<string, float>>();

            using (System.IO.StreamReader rdr = new System.IO.StreamReader(txt_vectors_path))
            {
                int veclen = int.Parse(rdr.ReadLine().Split(' ')[1]);
                float[] vx = new float[veclen];

                while (!rdr.EndOfStream)
                {
                    string line = rdr.ReadLine();
                    string[] tx = line.Split(' ');

                    for (int j = 0; j < veclen; ++j)
                        vx[j] = float.Parse(tx[j + 1], System.Globalization.CultureInfo.InvariantCulture);

                    float cos = VectorMath.CosineSimilarity(v, vx);
                    res.Add(new KeyValuePair<string, float>(tx[0], cos));
                }
            }

            return res;
        }
    }
}
