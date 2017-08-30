using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace W2V_Lib
{
    public static class VectorMath
    {
        public static float[] Zeros(int dim)
        {
            float[] res = new float[dim];
            Array.Clear(res, 0, dim);
            return res;
        }

        public static void Clear(double[] v)
        {
            Array.Clear(v, 0, v.Length);
        }

        public static void Clear(float[] v)
        {
            Array.Clear(v, 0, v.Length);
        }

        public static void Copy(float[] src, double[] dst)
        {
            for (int i = 0; i < src.Length; ++i)
                dst[i] = src[i];
        }

        public static bool IsNull(float[] v)
        {
            foreach (float x in v)
                if (Math.Abs(x) > 1e-8f)
                    return false;

            return true;
        }

        public static float EuclidDist2(float[] a, float[] b)
        {
            float d2 = 0;

            for (int i = 0; i < a.Length; ++i)
            {
                d2 += (a[i] - b[i]) * (a[i] - b[i]);
            }

            return d2;
        }

        public static float CosineSimilarity(float[] a, float[] b)
        {
            float p = 0, norma1 = 0, norma2 = 0;

            for (int i = 0; i < a.Length; ++i)
            {
                norma1 += a[i] * a[i];
                norma2 += b[i] * b[i];
                p += a[i] * b[i];
            }

            if (norma1 < 1e-20f && norma2 < 1e-20f)
                return 1.0f;

            if (norma1 < 1e-20f || norma2 < 1e-20f)
                return 0f;

            return p / (float)Math.Sqrt(norma1 * norma2);
        }

        public static double CosineSimilarity(double[] a, double[] b)
        {
            double p = 0, norma1 = 0, norma2 = 0;

            for (int i = 0; i < a.Length; ++i)
            {
                norma1 += a[i] * a[i];
                norma2 += b[i] * b[i];
                p += a[i] * b[i];
            }

            if (norma1 < 1e-20 && norma2 < 1e-20)
                return 1.0;

            if (norma1 < 1e-20 || norma2 < 1e-20)
                return 0;

            return p / Math.Sqrt(norma1 * norma2);
        }

        public static void Add(float[] accum, float[] add)
        {
            for (int i = 0; i < accum.Length; ++i)
                accum[i] += add[i];

            return;
        }

        public static void Sub(float[] accum, float[] sub)
        {
            for (int i = 0; i < accum.Length; ++i)
                accum[i] -= sub[i];

            return;
        }

        public static void Scale(float[] v, float k)
        {
            for (int i = 0; i < v.Length; ++i)
                v[i] *= k;

            return;
        }

        public static float Length(float[] a)
        {
            float s2 = 0;
            for (int i = 0; i < a.Length; ++i) { s2 += a[i] * a[i]; }
            return (float)Math.Sqrt(s2);
        }

        public static float Sum(float[] a)
        {
            float sum = 0;
            for (int i = 0; i < a.Length; ++i)
            {
                sum += a[i];
            }

            return sum;
        }
    }

}
