using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


class Clusterizer
{
    private int nb_clusters;

    public Clusterizer(int nb_clusters)
    {
        this.nb_clusters = nb_clusters;
    }


    float[][] clusters_num;
    float[] clusters_denom;
    float[][] clusters;
    Dictionary<int, int> ngram_2_cluster;
    int ngram_dim;
    int sample_counter;

    public void Fit(
                    int ngram_arity,
                    int veclen,
                    string[] ngram_texts,
                    float[][] ngram_vectors,
                    float[] ngram_scores,
                    float[] ngram_scores0,
                    int max_nb_probes,
                    float tolerance,
                    int checkpoint_interval,
                    string result_path
                   )
    {
        ngram_dim = ngram_vectors[0].Length;

        if (ngram_dim != (ngram_arity * veclen))
        {
            throw new ArgumentException();
        }

        float sum_scores = ngram_scores0.Sum();


        Random rnd = new Random();

        // Инициализация кластеров
        clusters_num = new float[nb_clusters][];
        clusters_denom = new float[nb_clusters];
        clusters = new float[nb_clusters][];
        for (int i = 0; i < nb_clusters; ++i)
        {
            clusters[i] = new float[ngram_dim];
            clusters_num[i] = new float[ngram_dim];
            clusters_denom[i] = 0;

            // Случайная инициализация центроидов
            for (int j = 0; j < ngram_dim; ++j)
            {
                float f = (float)(-1 + rnd.NextDouble() * 2);
                clusters[i][j] = f;
                clusters_num[i][j] = 0; // f;
            }
        }

        // привязка N-грамм к кластерам
        ngram_2_cluster = new Dictionary<int, int>();

        // Начинаем сэмплинг n-грамм и обновление кластеров
        float avg_cluster_change = 1.0f;
        sample_counter = 0;
        while (true)
        {
            sample_counter++;

            float sample_score = (float)(sum_scores * rnd.NextDouble());

            int i_left = 0, i_right = ngram_scores.Length - 1;
            while ((i_right - i_left) > 1)
            {
                int i_mid = (i_right + i_left) / 2;
                if (ngram_scores[i_mid] < sample_score)
                {
                    i_left = i_mid;
                }
                else if (ngram_scores[i_mid] >= sample_score)
                {
                    i_right = i_mid;
                }
            }

            int sample_index = i_right;

            // Ищем, к какому кластеру будет относиться этот вектор
            int best_cluster = -1;
            float min_distance = float.MaxValue;
            for (int icluster = 0; icluster < nb_clusters; ++icluster)
            {

                /*
                float dist2 = 0.0f;
                // ЕВКЛИДОВО РАССТОЯНИЕ 
                                    for (int k = 0; k < ngram_dim; ++k)
                                    {
                                        float z = (ngram_vectors[sample_index, k] - clusters[icluster][k]);
                                        dist2 += z * z;
                                    }*/

                float sim = 1.0f;
                for (int iterm = 0; iterm < ngram_arity; ++iterm)
                {
                    float AB = 0, A = 0, B = 0;
                    for (int j = 0, k = iterm * veclen; j < veclen; ++j, ++k)
                    {
                        float a = ngram_vectors[sample_index][k];
                        float b = clusters[icluster][k];
                        AB += a * b;
                        A += a * a;
                        B += b * b;
                    }

                    float term_sim = AB / ((float)Math.Sqrt(A * B) + 1e-38f);
                    if (term_sim < -1.0001f || term_sim > 1.0001f)
                    {
                        throw new ArgumentOutOfRangeException($"term_sim={term_sim}");
                    }
                    sim *= term_sim;
                }
                float dist2 = 0.5f - sim / 2.0f;


                if (dist2 < min_distance)
                {
                    best_cluster = icluster;
                    min_distance = dist2;
                }
            }

            if (best_cluster == -1)
            {
                throw new ApplicationException();
            }

            // Если данная n-грамма ранее относилась к другому кластеру, то удалим ее оттуда.
            int old_cluster = -1;

            if (ngram_2_cluster.TryGetValue(sample_index, out old_cluster))
            {
                if (old_cluster != best_cluster)
                {
                    clusters_denom[old_cluster] -= 1.0f;
                    float denom = clusters_denom[old_cluster] + 1e-38f;
                    for (int k = 0; k < ngram_dim; ++k)
                    {
                        clusters_num[old_cluster][k] -= ngram_vectors[sample_index][k];
                        clusters[old_cluster][k] = clusters_num[old_cluster][k] / denom;
                    }
                }
            }

            float cluster_change = 0.0f;
            if (old_cluster != best_cluster)
            {
                cluster_change = 1.0f;

                // Обновляем координаты нового кластера
                ngram_2_cluster[sample_index] = best_cluster;
                clusters_denom[best_cluster] += 1.0f;
                float denom2 = clusters_denom[best_cluster];
                for (int k = 0; k < ngram_dim; ++k)
                {
                    clusters_num[best_cluster][k] += ngram_vectors[sample_index][k];
                    clusters[best_cluster][k] = clusters_num[best_cluster][k] / denom2;
                }
            }



            float avg_moment = 1e-3f;
            avg_cluster_change = (1.0f - avg_moment) * avg_cluster_change + avg_moment * cluster_change;
            if ((sample_counter % 10000) == 0)
            {
                HashSet<int> nonempty_clusters = new HashSet<int>();
                foreach (var q in ngram_2_cluster)
                {
                    nonempty_clusters.Add(q.Value);
                }

                Console.Write($"{sample_counter} ngrams sampled, {nonempty_clusters.Count} nonempty clusters, {avg_cluster_change} changes in clusters\r");

                if ((sample_counter % checkpoint_interval) == 0)
                {
                    Console.WriteLine($"\nCheckpoint of model at {sample_counter} samples");
                    StoreClusters( ngram_vectors, ngram_texts, ngram_scores0, result_path);
                }
            }

            if (sample_counter > max_nb_probes)
            {
                Console.WriteLine("Iteration limit reached");
                break;
            }

            if (avg_cluster_change < tolerance)
            {
                Console.Write("Minimal change in solution has been detected, stop the clusterization now.");
                break;
            }
        }

        Console.WriteLine("\nClusterization completed.");

        StoreClusters(ngram_vectors, ngram_texts, ngram_scores0, result_path);

        return;
    }

    public void StoreClusters( float[][] ngram_vectors, string[] ngram_texts, float[] ngram_scores0, string clusters_path)
    {
        Dictionary<int, List<int>> cluster_2_ngrams = new Dictionary<int, List<int>>();
        for (int icluster = 0; icluster < nb_clusters; ++icluster)
        {
            cluster_2_ngrams[icluster] = new List<int>();
        }

        foreach (var q in ngram_2_cluster)
        {
            cluster_2_ngrams[q.Value].Add(q.Key);
        }

        using (System.IO.StreamWriter wrt = new System.IO.StreamWriter(clusters_path))
        {
            for (int icluster = 0; icluster < nb_clusters; ++icluster)
            {
                if (cluster_2_ngrams[icluster].Count > 0)
                {
                    wrt.WriteLine($"\n\ncluster #{icluster} - {cluster_2_ngrams[icluster].Count} ngrams:");

                    foreach (int ngram_index in cluster_2_ngrams[icluster].OrderByDescending(z => ngram_scores0[z]*W2V_Lib.VectorMath.CosineSimilarity(ngram_vectors[z],clusters[icluster]) ))
                    {
                        float score = ngram_scores0[ngram_index] * W2V_Lib.VectorMath.CosineSimilarity(ngram_vectors[ngram_index], clusters[icluster]);
                        wrt.WriteLine($"{ngram_texts[ngram_index]}\t{score}");
                    }
                }
            }
        }

    }
}



class Clusterize_MutualInfo
{
    static void Main(string[] args)
    {
        string w2v_path = @"f:\Word2Vec\word_vectors_cbow=1_win=5_dim=32.txt";
        string ngrams_path = @"f:\tmp\mutual_info_2_ru.dat";
        string clusters_path = @"f:\tmp\clusters.txt";

        int max_nb_ngrams = 2000000;
        int nb_clusters = 1000;

        Console.WriteLine($"Load w2v model from {w2v_path}");
        W2V_Lib.W2V_Model w2v = new W2V_Lib.W2V_Model(w2v_path, lazy_load: true);
        int veclen = w2v.GetVectorLen();

        int ngram_arity = 0;
        using (System.IO.StreamReader rdr = new System.IO.StreamReader(ngrams_path))
        {
            ngram_arity = rdr.ReadLine().Split('\t').Length - 1;
        }


        Console.WriteLine($"Load and vectorize ngrams from {ngrams_path}");
        int ngram_dim = ngram_arity * veclen;
        string[] ngram_texts = new string[max_nb_ngrams];
        float[][] ngram_vectors = new float[max_nb_ngrams][];
        for (int i = 0; i < max_nb_ngrams; ++i)
            ngram_vectors[i] = new float[ngram_dim];

        float[] ngram_scores = new float[max_nb_ngrams];
        float[] ngram_scores0 = new float[max_nb_ngrams];
        float sum_scores = 0.0f;

        using (System.IO.StreamReader rdr = new System.IO.StreamReader(ngrams_path))
        {
            int idata = 0;
            while (!rdr.EndOfStream && idata < max_nb_ngrams)
            {
                string line = rdr.ReadLine();
                string[] toks = line.Split('\t');

                ngram_texts[idata] = string.Join(" ", toks.Take(ngram_arity));

                for (int j = 0; j < ngram_arity; ++j)
                {
                    string word = toks[j];
                    float[] word_vec = w2v[word];
                    for (int k = 0; k < veclen; ++k)
                    {
                        ngram_vectors[idata][j * veclen + k] = word_vec[k];
                    }
                }

                float score = float.Parse(toks[ngram_arity], System.Globalization.CultureInfo.InvariantCulture);
                ngram_scores0[idata] = score;
                sum_scores += score;
                ngram_scores[idata] = sum_scores;
                idata++;
            }

            if (idata != max_nb_ngrams)
            {
                throw new ApplicationException($"Not enough ngrams in datafile {ngrams_path}");
            }
        }


        Console.WriteLine("Clusterize");
        Clusterizer clusterizer = new Clusterizer(nb_clusters);
        clusterizer.Fit(
                        ngram_arity,
                        veclen,
                        ngram_texts,
                        ngram_vectors,
                        ngram_scores,
                        ngram_scores0,
                        10000000,
                        2e-2f,
                        1000000,
                        clusters_path
                       );
        Console.WriteLine("All done.");

        return;
    }
}
