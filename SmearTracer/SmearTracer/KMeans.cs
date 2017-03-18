﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SmearTracer
{
    public class KMeans
    {
        public Cluster[] Clusters{ get; }
        public int CountClusters { get; }
        
        public KMeans (int countClusters)
        {
            Clusters = new Cluster[countClusters];

            CountClusters = countClusters;
        }

        public BitmapSource Compute(BitmapSource source)
        {
            double[][] data = BitmapImageToDoubleArray(source);

            for(int i = 0; i < Clusters.Length; i++)
            {
                Clusters[i] = new Cluster(data[0].Length);
            }

            InitialCentroids(data);

            double delta;
            double e = 1;
            int maxIteration = 100;
            int counter = 0;
            do
            {
                delta = 0;
                UpdateMeans(data);
                UpdateCentroids();
               
                foreach (Cluster cluster in Clusters)
                {
                    delta += Distance(cluster.Centroid, cluster.LastCentroid);
                }
                counter++;
            }
            while (delta > e && counter < maxIteration);

            data = ApplyCentroids(data);

            BitmapSource image = DoubleArrayToBitmapImage(source, data);

            return image;
        }

        private double[][] BitmapImageToDoubleArray(BitmapSource source)
        {
            double[][] inputData = new double[source.PixelWidth * source.PixelHeight][];
            const int pixelFormatSize = 4;
            int stride = source.PixelWidth * pixelFormatSize;
            int size = source.PixelHeight * stride;
            int counter = 0;
            int indexPixel;
            byte[] data = new byte[size];
            source.CopyPixels(data, stride, 0);

            for(int x = 0; x < source.PixelWidth; x++)
            {
                for(int y = 0; y < source.PixelHeight; y++)
                {
                    indexPixel = y * stride + pixelFormatSize * x;
                    inputData[counter] = new double[pixelFormatSize];
                    for(int i = 0; i < pixelFormatSize; i++)
                    {
                        inputData[counter][i] = data[indexPixel + i];
                    }
                    counter++;
                }          
            }

            return inputData;
        }

        private BitmapSource DoubleArrayToBitmapImage(BitmapSource source, double[][] inputData)
        { 
            const int pixelFormatSize = 4;
            int stride = source.PixelWidth * pixelFormatSize;
            int size = source.PixelHeight * stride;
            int counter = 0;
            int indexPixel;
            byte[] data = new byte[size];

            for (int x = 0; x < source.PixelWidth; x++)
            {
                for (int y = 0; y < source.PixelHeight; y++)
                {
                    indexPixel = y * stride + pixelFormatSize * x;
                    for (int i = 0; i < pixelFormatSize; i++)
                    {
                        data[indexPixel + i] = (byte)inputData[counter][i];
                    }
                    counter++;
                }
            }

            BitmapSource image = BitmapSource.Create(source.PixelWidth, source.PixelHeight, source.DpiX, 
                source.DpiY, source.Format, source.Palette, data, stride);

            return image;
        }

        private void UpdateMeans(double[][] image)
        {
            int index;
            foreach (Cluster cluster in Clusters)
            {
                cluster.Data = new List<double[]>();
            }
            for (int i = 0; i < image.GetLength(0); i++)
            {
                index = NearestCentroid(image[i]);
                Clusters[index].Data.Add(image[i]);              
            }
        } 

        private void InitialCentroids(double[][] image)
        {
            double[][] sortedArray = image;
            //double[][] sortedArray = image.OrderBy(p=>p.Sum()).ToArray();
            int step = (image.GetLength(0)) / (CountClusters);

            for (int i = 0; i < Clusters.Length; i++)
            {
                Clusters[i].Centroid = sortedArray[i * step / 2];
            }
        }

        private void UpdateCentroids()
        {
            double[] centroid;

            foreach (Cluster cluster in Clusters)
            {
                centroid = new double[Clusters[0].Centroid.Length];
                if (cluster.Data.Count > 0)
                {
                    for (int i = 0; i < cluster.Data.Count; i++)
                    {
                        for (int j = 0; j < cluster.Data[i].Length; j++)
                        {
                            centroid[j] += cluster.Data[i][j];
                        }
                    }

                    for (int i = 0; i < centroid.Length; i++)
                    {
                        centroid[i] /= cluster.Data.Count;
                    }

                    cluster.LastCentroid = cluster.Centroid;
                    cluster.Centroid = centroid;
                }
            }         
        }

        private double[][] ApplyCentroids(double[][] data)
        {
            double[][] results = new double[data.GetLength(0)][];

            for (int i = 0; i < results.GetLength(0); i++)
            {
                results[i] = Clusters[NearestCentroid(data[i])].Centroid;
            }

            return results;
        }

        private int NearestCentroid(double[] data)
        {
            double distance = Distance(data, Clusters[0].Centroid);
            double minDistance = distance;
            int index = 0;
            for(int i = 0; i < Clusters.Length; i++)
            {
                distance = Distance(data, Clusters[i].Centroid);

                if (minDistance > distance)
                {
                    minDistance = distance;
                    index = i;
                }             
            }
            return index;
        }

        private double Distance(double[] x, double[] u)
        {
            double dictance = 0;

            for(int i = 0; i < x.Length; i++)
            {
                //if (dictance < Math.Abs(x[i] - u[i]))
                //{
                    dictance += Math.Abs(x[i] - u[i]);
                //}
            }
            return dictance;
        }
    }
}
