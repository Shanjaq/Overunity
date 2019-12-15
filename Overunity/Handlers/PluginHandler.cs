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
            FileStream fs = fi.Open(FileMode.Open, FileAccess.Read);

            string pluginAuthor = "";

            byte[] buffer = new byte[fi.Length];

            //file header
            fs.Read(buffer, 0, 4);
            string fileSignature= System.Text.Encoding.UTF8.GetString(buffer, 0, 4);

            fs.Read(buffer, 0, 4);
            int fileHeaderSize = BitConverter.ToInt32(buffer, 0);

            byte[] header = new byte[fileHeaderSize];

            fs.Seek(0, SeekOrigin.Begin);
            fs.Read(header, 0, fileHeaderSize);

            string fieldName = "";

            //plugin header
            if (fileSignature == "TES3")
                fieldName = System.Text.Encoding.UTF8.GetString(header, 16, 4);
            else if (fileSignature == "TES4")
                fieldName = System.Text.Encoding.UTF8.GetString(header, 24, 4);
            //string header_text = System.Text.Encoding.UTF8.GetString(header, 0, header_size);

            switch (fieldName)
            {
                case ("HEDR"):
                    {
                        int pluginHeaderSize = BitConverter.ToInt32(header, 20);
                        int pluginVersion = BitConverter.ToInt32(header, 24);
                        if (fileSignature == "TES3")
                            pluginAuthor = System.Text.Encoding.UTF8.GetString(header, 32, 32).Trim('\0');
                        else if(fileSignature == "TES4")
                        {
                            fieldName = System.Text.Encoding.UTF8.GetString(header, 42, 4);
                            int fieldLength = BitConverter.ToInt16(header, 46); ;

                            pluginAuthor = System.Text.Encoding.UTF8.GetString(header, 48, fieldLength).Trim('\0');
                            break;
                        }
                        break;
                    }
            }

            DataTable tblTmp = new DataTable();
            StringReader sReader = new StringReader(tableFormat);
            tblTmp.ReadXmlSchema(sReader);
            DataRow row = tblTmp.NewRow();
            row["FullPath"] = fi.FullName;
            row["PluginName"] = fi.Name;
            row["DateModified"] = fi.LastWriteTime;
            row["Author"] = pluginAuthor;
            row["Size"] = fi.Length;
            row["Id"] = Guid.NewGuid();
            tblTmp.Rows.Add(row);

            tblTmp.AcceptChanges();

            return tblTmp;
        }
    }
}
