using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Overunity.Handlers
{
    class SaveHandler : IHandler
    {
        public DataTable Import(string filePath, string tableFormat)
        {
            Console.WriteLine("Save File!");

            DataTable tblTmp = new DataTable();

            StringReader sReader = new StringReader(tableFormat);
            tblTmp.ReadXmlSchema(sReader);

            DataRow row = tblTmp.NewRow();
            row["PluginName"] = new FileInfo(filePath).Name;
            tblTmp.Rows.Add(row);

            tblTmp.AcceptChanges();

            return tblTmp;
        }
    }
}
