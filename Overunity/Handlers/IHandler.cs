using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Overunity.Handlers
{
    interface IHandler
    {
        /// <summary>
        /// File Import Handler for Drag and Drop operations
        /// </summary>
        DataTable Import(string filePath, string tableFormat);

    }
}
