﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmearsMaker.Common;
using SmearsMaker.Common.BaseTypes;

namespace SmearsMaker.ImageProcessing.Clustering
{
	public abstract class Kmeans : IClusterizer
	{
		protected readonly List<Point> Points;
		protected readonly List<Cluster> Clusters;
		private readonly double _precision;
		private readonly int _maxIteration;

		protected Kmeans(int clustersCount, double precision, int maxIteration)
		{
			Points = new List<Point>();
			Clusters = new List<Cluster>();
			for (int i = 0; i < clustersCount; i++)
			{
				Clusters.Add(new Cluster());
			}
			_precision = precision;
			_maxIteration = maxIteration;
		}
		public List<Cluster> Clustering(PointCollection points)
		{
			Points.AddRange(points.Select(p => p.Clone()));
			FillCentroidsWithInitialValues();

			double delta;
			var counter = 0;
			do
			{
				UpdateMeans();
				UpdateCentroids();
				delta = Clusters.Sum(cluster => cluster.DistanceBeetweenCentroids);
				counter++;
			}
			while (delta > _precision && counter < _maxIteration);

			MergingSmallClusters();

			return Clusters;
		}
		protected virtual void MergingSmallClusters()
		{
			var smallData = new List<Point>();

			for (int i = 0; i < Clusters.Count; i++)
			{
				if (Clusters[i].Points.Count != 0) continue;
				smallData.AddRange(Clusters[i].Points);
				Clusters.Remove(Clusters[i]);
			}
			Parallel.ForEach(smallData, d =>
			{
				var index = NearestCentroid(d.Pixels[Layers.Filtered]);
				lock (smallData)
				{
					Clusters[index].Points.Add(d);
				}
			});
		}
		private void UpdateMeans()
		{
			foreach (var cluster in Clusters)
			{
				cluster.Points.Clear();
			}
			Parallel.ForEach(Points, d =>
			{
				var index = NearestCentroid(d.Pixels[Layers.Original]);
				lock (Points)
				{
					Clusters[index].Points.Add(d);
				}
			});

			Clusters.RemoveAll(c => c.Points.Count == 0);
		}
		private void UpdateCentroids()
		{
			Parallel.ForEach(Clusters, UpdateCentroid);
		}
		protected abstract void FillCentroidsWithInitialValues();
		protected abstract void UpdateCentroid(Cluster cluster);
		protected abstract int NearestCentroid(Pixel pixel);
		protected abstract double Distance(IReadOnlyList<double> left, IReadOnlyList<double> right);
	}
}