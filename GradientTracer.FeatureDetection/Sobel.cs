﻿using System;
using System.Collections.Generic;
using System.Linq;
using GradientTracer.Model;
using SmearsMaker.Common;
using SmearsMaker.Common.BaseTypes;


namespace GradientTracer.FeatureDetection
{
	public class Sobel
	{
		private readonly int _width;
		private readonly int _height;
		public Sobel(int width, int height)
		{
			_width = width;
			_height = height;
		}

		public List<Point> Compute(List<Point> points)
		{
			var result = new List<Point>();
			var arrLength = points.First().Pixels[GtConsts.Original].Length;
			for (int coordX = 0; coordX < _width; coordX++)
			{
				for (int coordY = 0; coordY < _height; coordY++)
				{
					var mask = GetMask(points, coordX, coordY);
					var pos = coordX * _height + coordY;
					var gradient = new float[arrLength];
					var curve = new float[arrLength];
					var gx = (mask[6].GrayScale + mask[7].GrayScale * 2 + mask[8].GrayScale) - (mask[0].GrayScale + mask[1].GrayScale * 2 + mask[2].GrayScale);
					var gy = (mask[2].GrayScale + mask[5].GrayScale * 2 + mask[8].GrayScale) - (mask[0].GrayScale + mask[3].GrayScale * 2 + mask[6].GrayScale);
					var norm = (float) Math.Sqrt(gx * gx + gy * gy);
					var tetta = (float)(Math.Atan(gy / gx) * 180 / 3.14);
					if (gx<0)
					{
						tetta += 180;
					}

					if (gx == 0)
					{
						tetta = 0;
					}

					for (int i = 0; i < gradient.Length - 1; i++)
					{
						gradient[i] = tetta;
						curve[i] = norm;
					}
					gradient[3] = 255;
					curve[3] = 255;

					var p = new Point(points[pos]);
					p.Pixels[GtConsts.Gradient] = new Pixel(gradient);
					p.Pixels[GtConsts.Curves] = new Pixel(curve);
					result.Add(p);
				}
			}

			return result;
		}

		private List<Pixel> GetMask(IList<Point> units, int x, int y)
		{
			var mask = new List<Pixel>();
			var size = units.First().Pixels[Consts.Original].Length;

			for (int coordMaskX = x - 1; coordMaskX <= x + 1; coordMaskX++)
			{
				for (int coordMaskY = y - 1; coordMaskY <= y + 1; coordMaskY++)
				{
					var idx = coordMaskX * _height + coordMaskY;
					if (idx < _height * _width && coordMaskX >= 0 && coordMaskY >= 0)
					{
						mask.Add(units[idx].Pixels[Consts.Filtered]);
					}
					else
					{
						mask.Add(new Pixel(new float[size]));
					}
				}
			}
			return mask;
		}
	}
}
