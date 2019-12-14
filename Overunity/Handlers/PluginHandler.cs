using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Overunity.Handlers
{
    class PluginHandler : IHandler
    {
        public DataTable Import(string filePath, string tableFormat)
        {
            Console.WriteLine("Plugin!");

            FileInfo fi = new FileInfo(filePath);

            DataTable tblTmp = new DataTable();
            StringReader sReader = new StringReader(tableFormat);
            tblTmp.ReadXmlSchema(sReader);
            DataRow row = tblTmp.NewRow();
            row["Plugin Name"] = fi.Name;
            row["Date Modified"] = fi.LastWriteTime;
            row["Author"] = "Someone";
            row["Size"] = fi.Length;
            tblTmp.Rows.Add(row);

            tblTmp.AcceptChanges();

            return tblTmp;
        }
    }
}
