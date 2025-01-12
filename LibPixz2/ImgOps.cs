using System;

namespace LibPixz2
{
    public class ImgOps
    {
        public const int blkSize = 8;

        public static float[,] coefYU = new float[8, 8];

        public static float[,] coefUY = new float[8, 8];

        public static float[,] coefUV = new float[8, 8];

        public static ArraySlice<float> coefYUS = new ArraySlice<float>(coefYU);

        public static ArraySlice<float> coefUYS = new ArraySlice<float>(coefUY);

        public static ArraySlice<float> coefUVS = new ArraySlice<float>(coefUV);

        public static float[,] tCosXU = GetTablaICos(8);

        public static float[,] tCosYV = GetTablaICos(8);

        public static int[] bitsPorNum = new int[65]
        {
            0, 0, 1, 0, 2, 0, 0, 0, 3, 0,
            0, 0, 0, 0, 0, 0, 4, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 5, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 6
        };

        public static float[,] GetTablaICos(int tam)
        {
            float[,] array = new float[tam, tam];
            for (int i = 0; i < tam; i++)
            {
                for (int j = 0; j < tam; j++)
                {
                    array[i, j] = (float)Math.Cos((double)((2 * j + 1) * i) * Math.PI / (double)(2 * tam));
                    array[i, j] *= (float)Math.Sqrt(2.0 / (double)tam);
                    if (i == 0)
                    {
                        array[i, j] /= (float)Math.Sqrt(2.0);
                    }
                }
            }

            return array;
        }

        public static void Ifct8(ArraySlice<float> bloque, ArraySlice<float> res, float[,] tIcos)
        {
            float num = bloque[0] * tIcos[0, 0];
            float num2 = 0f;
            num2 += bloque[4] * tIcos[4, 0];
            float num3 = 0f;
            num3 += bloque[2] * tIcos[2, 0];
            num3 += bloque[6] * tIcos[6, 0];
            float num4 = 0f;
            num4 += bloque[2] * tIcos[2, 1];
            num4 += bloque[6] * tIcos[6, 1];
            float num5 = 0f;
            num5 += bloque[1] * tIcos[1, 0];
            num5 += bloque[3] * tIcos[3, 0];
            num5 += bloque[5] * tIcos[5, 0];
            num5 += bloque[7] * tIcos[7, 0];
            float num6 = 0f;
            num6 += bloque[1] * tIcos[1, 1];
            num6 += bloque[3] * tIcos[3, 1];
            num6 += bloque[5] * tIcos[5, 1];
            num6 += bloque[7] * tIcos[7, 1];
            float num7 = 0f;
            num7 += bloque[1] * tIcos[1, 2];
            num7 += bloque[3] * tIcos[3, 2];
            num7 += bloque[5] * tIcos[5, 2];
            num7 += bloque[7] * tIcos[7, 2];
            float num8 = 0f;
            num8 += bloque[1] * tIcos[1, 3];
            num8 += bloque[3] * tIcos[3, 3];
            num8 += bloque[5] * tIcos[5, 3];
            num8 += bloque[7] * tIcos[7, 3];
            float num9 = num + num2;
            float num10 = num - num2;
            float num11 = num9 + num3;
            float num12 = num9 - num3;
            float num13 = num10 + num4;
            float num14 = num10 - num4;
            res[0] = num11 + num5;
            res[7] = num11 - num5;
            res[3] = num12 + num8;
            res[4] = num12 - num8;
            res[1] = num13 + num6;
            res[6] = num13 - num6;
            res[2] = num14 + num7;
            res[5] = num14 - num7;
        }

        public static void Fidct(float[,] bloque, float[,] bloqueDct, int tamX, int tamY)
        {
            ArraySlice<float> arraySlice = new ArraySlice<float>(bloque);
            for (int i = 0; i < tamY; i++)
            {
                Ifct8(arraySlice.GetSlice(i), coefYUS.GetSlice(i), tCosXU);
            }

            Common.Transpose(coefYU, coefUY, tamX, tamY);
            for (int j = 0; j < tamX; j++)
            {
                Ifct8(coefUYS.GetSlice(j), coefUVS.GetSlice(j), tCosYV);
            }

            for (int k = 0; k < tamY; k++)
            {
                for (int l = 0; l < tamX; l++)
                {
                    bloqueDct[k, l] = (float)Math.Round(coefUV[l, k]);
                }
            }
        }

        public static void Idct(float[,] bloque, float[,] bloqueDct, int tamX, int tamY)
        {
            for (int i = 0; i < tamY; i++)
            {
                for (int j = 0; j < tamX; j++)
                {
                    int k = 0;
                    float num = 0f;
                    for (; k < tamY; k++)
                    {
                        int l = 0;
                        float num2 = 0f;
                        for (; l < tamX; l++)
                        {
                            num2 += bloque[k, l] * tCosXU[l, j];
                        }

                        num += num2 * tCosYV[k, i];
                    }

                    bloqueDct[i, j] = (float)Math.Round(num);
                }
            }
        }

        public static void MostrarBordes(float[,] coefDct, int tam)
        {
            for (int i = 0; i < tam; i++)
            {
                coefDct[i, tam - 1] = 96f;
            }

            for (int j = 0; j < tam; j++)
            {
                coefDct[tam - 1, j] = 96f;
            }
        }

        public static void ResizeAndInsertBlock(ImgInfo imgInfo, float[,] block, float[,] imagen, int tamX, int tamY, int ofsX, int ofsY, int scaleX, int scaleY)
        {
            if (ofsX >= imgInfo.width || ofsY >= imgInfo.height)
            {
                return;
            }

            for (int i = 0; i < tamY; i++)
            {
                for (int j = 0; j < tamX; j++)
                {
                    for (int k = 0; k < scaleY; k++)
                    {
                        for (int l = 0; l < scaleX; l++)
                        {
                            int num = i * scaleY + ofsY + k;
                            int num2 = j * scaleX + ofsX + l;
                            if (num < imgInfo.height && num2 < imgInfo.width)
                            {
                                imagen[num, num2] = block[i, j];
                            }
                        }
                    }
                }
            }
        }

        public static void Dequant(short[,] pixQnt, float[,] coefDct, ushort[] matriz, int tam)
        {
            for (int i = 0; i < tam; i++)
            {
                for (int j = 0; j < tam; j++)
                {
                    coefDct[i, j] = pixQnt[i, j] * matriz[i * tam + j];
                }
            }
        }
    }
}