﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using SmearsMaker.ClusterAnalysis.Kmeans;
using SmearsMaker.Concatenation;
using SmearsMaker.Filtering;
using SmearsMaker.Model;
using SmearsMaker.Model.Helpers;
using SmearsMaker.Segmentation;
using SmearsMaker.Segmentation.SimpleSegmentsSplitter;
using SmearsMaker.Segmentation.SuperpixelSplitter;

namespace SmearsMaker.Logic
{
	public class SmearTracer
	{
		private readonly ImageContext _context;
		private readonly List<Smear> _smears;
		public SmearTracer(ImageContext context)
		{
			_context = context ?? throw new NullReferenceException("context");
			_smears = new List<Smear>();
		}

		public Task Execute(ProgressBar progress)
		{
			if (progress == null)
			{
				throw new NullReferenceException(nameof(progress));
			}

			progress.NewProgress("Фильтрация...", 1, 22, 1);
			var model = ImageModel.ConvertBitmapToImage(_context.Image, ColorModel.Rgb);
			var filter = new MedianFilter(_context.FilterRank, _context.Width, _context.Height);
			filter.Filter(model);

			//model.ChangeColorModel(ColorModel.GrayScale);
			var kmeans = new KmeansClassic(_context.ClustersCount, _context.ClustersPrecision, model, _context.ClusterMaxIteration);
			var splitter = new SimpleSegmentsSplitter();
			var supPixSplitter = new SuperpixelSplitter(_context.MinSizeSuperpixel, _context.MaxSizeSuperpixel, _context.ClustersPrecision);
			var bsm = new BsmPair(_context.MaxSmearDistance);

			return Task.Run(() =>
			{
				progress.NewProgress("Кластеризация...", 1, 22, 1);
				var clusters = kmeans.Clustering();

				Parallel.ForEach(clusters, (cluster) =>
				{
					var segments = splitter.Split(cluster.Data);

					Parallel.ForEach(segments, segment =>
					{
						var superPixels = supPixSplitter.Splitting(segment);

						var smears = bsm.Execute(superPixels.ToList<IObject>());

						foreach (var smear in smears)
						{
							var newSmear = new Smear(smear)
							{
								Cluster = cluster,
								Segment = segment
							};

							_smears.Add(newSmear);

						}
					});
				});
			});
		}

		public BitmapSource SuperPixels()
		{
			var data = new List<Point>();
			foreach (var smear in _smears)
			{
				foreach (var obj in smear.BrushStroke.Objects)
				{
					obj.Data.ForEach(d => d.SetFilteredPixel(obj.Centroid.Original.Data));
					data.AddRange(obj.Data);
				}
			}
			return ImageHelper.ConvertRgbToRgbBitmap(_context.Image, data);
		}

		public BitmapSource Clusters()
		{
			var data = new List<Point>();
			foreach (var smear in _smears)
			{
				foreach (var obj in smear.BrushStroke.Objects)
				{
					obj.Data.ForEach(d => d.SetFilteredPixel(smear.Cluster.Centroid.Data));
					data.AddRange(obj.Data);
				}
			}
			return ImageHelper.ConvertRgbToRgbBitmap(_context.Image, data);
		}

		public BitmapSource Segments()
		{
			var data = new List<Point>();
			foreach (var smear in _smears)
			{
				foreach (var obj in smear.BrushStroke.Objects)
				{
					obj.Data.ForEach(d => d.SetFilteredPixel(smear.Segment.Centroid.Original.Data));
					data.AddRange(obj.Data);
				}
			}
			return ImageHelper.ConvertRgbToRgbBitmap(_context.Image, data);
		}

		public BitmapSource BrushStrokes()
		{
			var data = new List<Point>();
			foreach (var smear in _smears)
			{
				var objs = smear.BrushStroke.Objects;
				var center = objs.OrderBy(p => p.Centroid.Original.Sum).ToList()[objs.Count / 2].Centroid.Original.Data;
				foreach (var obj in objs)
				{
					obj.Data.ForEach(d => d.SetFilteredPixel(center));
					data.AddRange(obj.Data);
				}
			}
			return ImageHelper.ConvertRgbToRgbBitmap(_context.Image, data);
		}

		public string GetPlt()
		{
			const int height = 5200;
			const int width = 7600;

			var delta = (float)(height / _context.Height);
			var widthImage = _context.Width * delta;
			if (widthImage > width)
			{
				delta *= width / widthImage;
			}

			//building string
			var plt = new StringBuilder().Append("IN;");

			var clusterGroups = _smears.GroupBy(s => s.Cluster);

			foreach (var cluster in clusterGroups.OrderByDescending(c => c.Key.Centroid.Sum))
			{
				plt.Append($"PC1,{(uint)cluster.Key.Centroid.Data[2]},{(uint)cluster.Key.Centroid.Data[1]},{(uint)cluster.Key.Centroid.Data[0]};");
				var segmentsGroups = cluster.GroupBy(c => c.Segment); ;

				foreach (var segment in segmentsGroups.OrderByDescending(s => s.Key.Centroid.Original.Sum))
				{
					var brushstrokeGroup = segment.GroupBy(b => b.BrushStroke);

					foreach (var brushStroke in brushstrokeGroup.OrderByDescending(b => b.Key.Objects.Count))
					{
						plt.Append($"PU{(uint)(brushStroke.Key.Objects.First().Centroid.Position.X * delta)},{(uint)(height - brushStroke.Key.Objects.First().Centroid.Position.Y * delta)};");

						for (int i = 1; i < brushStroke.Key.Objects.Count - 1; i++)
						{
							plt.Append($"PD{(uint)(brushStroke.Key.Objects[i].Centroid.Position.X * delta)},{(uint)(height - brushStroke.Key.Objects[i].Centroid.Position.Y * delta)};");
						}

						plt.Append($"PU{(uint)(brushStroke.Key.Objects.Last().Centroid.Position.X * delta)},{(uint)(height - brushStroke.Key.Objects.Last().Centroid.Position.Y * delta)};");
					}
				}
			}

			return plt.ToString();
		}
	}
}
