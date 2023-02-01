using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tiled2Dmap.CLI.Utility
{
    internal class ProgressBar
    {
        private int total;
        private int lastUpdate = 0;
        private readonly int _goal;
        private readonly int _updateStep;

        public int Progress { get; set; }

        public ProgressBar(int goal, int updateStep) 
        {
            _goal = goal;
            _updateStep = updateStep;
        }
        public bool Increment(int increment)
        {
            total++;
            Progress = (total * 100) / _goal;

            if(Progress >= lastUpdate + _updateStep)
            {
                lastUpdate = Progress;
                return true;
            }
            else
                return false;
        }
    }
}
