using System.Drawing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tiled2Dmap.CLI.Dmap
{
    public class CoordConverter
    {
        /// <summary>
        /// Size in umber of tiles
        /// </summary>
        public Size dmapSize;

        public Size backgroundSize;

        public CoordConverter(Size dmapSize, Size backgroundSize)
        {
            this.dmapSize = dmapSize;
            this.backgroundSize = backgroundSize;
        }

        public Point GetBackgroundWorldPos()
        {
            Point origin = new Point(64 * (dmapSize.Width / 2), 32 / 2);
            Point worldPos = new Point();

            worldPos.X = origin.X - backgroundSize.Width / 2;
            worldPos.Y = origin.Y + 32 * dmapSize.Height / 2 - backgroundSize.Height / 2;
            worldPos.Y -= ((dmapSize.Height + 1) % 2) * 16;

            return worldPos;
        }

        public Point Cell2World(Point cell)
        {
            Point world = new Point(0, 0);
            Point mposOrigin = new Point(64 * (dmapSize.Width / 2), 32 / 2);

            world.X = 32 * (cell.X - cell.Y) + mposOrigin.X;
            world.Y = 16 * (cell.X + cell.Y) + mposOrigin.Y;
            return world;
        }
        public Point World2Cell(Point posWorld)
        {
            Point cell = new Point(0, 0);
            Point mposOrigin = new Point(64 * (dmapSize.Width / 2), 32 / 2);

            double dwx = posWorld.X - mposOrigin.X;
            double dwy = posWorld.Y - mposOrigin.Y;
            double dch = 32.0;
            double dcw = 64.0;

            double dX = (dwx / dcw) + (dwy / dch);
            double dY = (double)(dwy / dch) - (double)(dwx / dcw);


            cell.X = (int)System.Math.Round(dX);

            cell.Y = (int)System.Math.Round(dY);

            return cell;
        }

        public Point Cell2Bg(Point cell)
        {
            return World2Bg(Cell2World(cell));
        }

        public Point World2Bg(Point posWorld)
        {
            Point bgPos =  new Point();
            Point bgWorldPos = GetBackgroundWorldPos();
            bgPos.X = posWorld.X - bgWorldPos.X;
            bgPos.Y = posWorld.Y - bgWorldPos.Y;

            return bgPos;
        }
    }
}
