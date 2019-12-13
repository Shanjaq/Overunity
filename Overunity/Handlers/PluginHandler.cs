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

            DataTable tblTmp = new DataTable();

            StringReader sReader = new StringReader(tableFormat);
            tblTmp.ReadXmlSchema(sReader);

            DataRow row = tblTmp.NewRow();
            row["Plugin Name"] = new FileInfo(filePath).Name;
            tblTmp.Rows.Add(row);

            /*
            //test data
            for (int i = 0; i < 10; i++)
            {
                DataRow row = tblTmp.NewRow();
                row["Plugin Name"] = "blargh " + i;
                tblTmp.Rows.Add(row);
            }
            */
            tblTmp.AcceptChanges();


            return tblTmp;
        }
    }
}
