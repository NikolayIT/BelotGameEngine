using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustBelot.Common
{
    public struct Trick
    {
        public IPlayer WinnerPlayer { get; set; }

        public IPlayer FirstPlayer { get; set; }
    }
}
