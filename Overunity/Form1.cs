using Overunity.Handlers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace Overunity
{
    public partial class Form1 : Form
    {
        ListView lvPlugins;

        DataSet dsActivePlugins = new DataSet();
        DataView dvActivePlugins;
        string strPluginTable_Schema = "";

        List<Tuple<string, string, IHandler>> fileImportHandlers = new List<Tuple<string, string, IHandler>>();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            DataTable dtActivePlugins = new DataTable("ActivePlugins");
            dtActivePlugins.Columns.Add("Plugin Name", typeof(string));
            dtActivePlugins.Columns.Add("Date Modified", typeof(DateTime));
            dtActivePlugins.Columns.Add("Author", typeof(string));
            dtActivePlugins.Columns.Add("Size", typeof(int));
            dtActivePlugins.Columns.Add("Priority", typeof(int));

            dsActivePlugins.Tables.Add(dtActivePlugins);

            StringWriter sWriter = new StringWriter();
            dtActivePlugins.WriteXmlSchema(sWriter);
            strPluginTable_Schema = sWriter.GetStringBuilder().ToString();

            //test data
            for (int i = 0; i < 10; i++)
            {
                DataRow row = dtActivePlugins.NewRow();
                row["Plugin Name"] = "blargh " + i;
                dtActivePlugins.Rows.Add(row);
            }
            dtActivePlugins.AcceptChanges();

            // Create a LinqDataView from a LINQ to DataSet query and bind it 
            // to the Windows forms control.
            EnumerableRowCollection<DataRow> pluginsQuery = from row in dtActivePlugins.AsEnumerable()
                                                            where row.Field<string>("Plugin Name") != null
                                                            orderby row.Field<string>("Plugin Name")
                                                            select row;
            dvActivePlugins = pluginsQuery.AsDataView();

            lvPlugins = new ListView
            {
                Location = new System.Drawing.Point(12, 12),
                Name = "ListView1",
                Size = new System.Drawing.Size(245, 200),
                GridLines = true,
                AllowColumnReorder = true,
                LabelEdit = true,
                FullRowSelect = true,
                Sorting = SortOrder.Ascending,
                View = View.Details,
                HeaderStyle = ColumnHeaderStyle.Clickable,
                AllowDrop = true
            };

            lvPlugins.DragDrop += new DragEventHandler(ListView_DragDrop);
            lvPlugins.DragEnter += new DragEventHandler(ListView_DragEnter);


            foreach (DataColumn dc in dtActivePlugins.Columns)
            {
                lvPlugins.Columns.Add(dc.Caption);
            }

            Controls.Add(lvPlugins);

            int j = 0;
            j += 1;

            ListView_Update();

            j += 1;

            fileImportHandlers = new List<Tuple<string, string, IHandler>>() {
                Tuple.Create( ".esp", "Plugin", (IHandler)new PluginHandler()),
                Tuple.Create( ".esm", "Master", (IHandler)new PluginHandler()),
                Tuple.Create( ".ess", "Save", (IHandler)new SaveHandler())
            };
        }

        void ListView_Update()
        {
            foreach (DataRow dr in dvActivePlugins.ToTable().Rows)
            {
                ListViewItem lvi = new ListViewItem(dr.ItemArray.First().ToString());

                bool isFirst = true;
                foreach (object column in dr.ItemArray)
                {
                    if (isFirst)
                        isFirst = false;
                    else
                        lvi.SubItems.Add(column.ToString());
                }

                lvPlugins.Items.Add(lvi);
            }
            lvPlugins.Update();
        }

        void ListView_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        void ListView_DragDrop(object sender, DragEventArgs e)
        {
            // Handle FileDrop data.
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                try
                {
                    files = files.Where(x => fileImportHandlers.Select(y => y.Item1.ToString()).Contains(x.Substring(x.Length - 4, 4))).ToArray();
                    foreach(string file in files)
                    {

                        DataTable imported_files = fileImportHandlers.Where(x => file.Substring(file.Length - 4, 4) == x.Item1).First().Item3.Import(file, strPluginTable_Schema);
                        foreach (DataRow dr in imported_files.Rows)
                        {
                            dsActivePlugins.Tables["ActivePlugins"].ImportRow(dr);
                        }
                    }

                    ListView_Update();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }

            }
        }
    }
}
