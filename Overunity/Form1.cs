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

        DataSet dsPlugins = new DataSet();
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

            dsPlugins.Tables.Add(dtActivePlugins);

            StringWriter sWriter = new StringWriter();
            dtActivePlugins.WriteXmlSchema(sWriter);
            strPluginTable_Schema = sWriter.GetStringBuilder().ToString();

            // Create a LinqDataView from a LINQ to DataSet query and bind it 
            // to the Windows forms control.
            EnumerableRowCollection<DataRow> pluginsQuery = from row in dtActivePlugins.AsEnumerable()
                                                            where row.Field<string>("Plugin Name") != null
                                                            orderby row.Field<string>("Plugin Name")
                                                            select row;
            dvActivePlugins = pluginsQuery.AsDataView();

            lvPlugins = new ListView
            {
                Name = "ListView_Plugins",
                Location = new System.Drawing.Point(0, 0),
                Size = new System.Drawing.Size(245, 200),
                GridLines = true,
                AllowColumnReorder = true,
                LabelEdit = true,
                FullRowSelect = true,
                Sorting = SortOrder.Ascending,
                View = View.Details,
                HeaderStyle = ColumnHeaderStyle.Clickable,
                AllowDrop = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom | AnchorStyles.Left,
                Dock = DockStyle.Fill
            };

            lvPlugins.DragDrop += new DragEventHandler(ListView_DragDrop);
            lvPlugins.DragEnter += new DragEventHandler(ListView_DragEnter);
            lvPlugins.ColumnClick += new ColumnClickEventHandler(ListView_ColumnClick);
            lvPlugins.Resize += new EventHandler(ListView_Resize);


            foreach (DataColumn dc in dtActivePlugins.Columns)
            {
                lvPlugins.Columns.Add(dc.Caption);
            }

            //Controls.Add(lvPlugins);

            Panel pMain = new Panel
            {
                Name = "Panel_Main",
                Location = new System.Drawing.Point(0, 0),
                Size = new System.Drawing.Size(245, 200),
                Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom | AnchorStyles.Left,
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 0, 64)
            };

            Controls.Add(pMain);
            pMain.Controls.Add(lvPlugins);

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
            lvPlugins.BeginUpdate();
            lvPlugins.Items.Clear();
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
            lvPlugins.EndUpdate();
            //lvPlugins.Update();
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
                            dsPlugins.Tables["ActivePlugins"].ImportRow(dr);
                        }
                    }

                    dsPlugins.Tables["ActivePlugins"].AcceptChanges();
                    ListView_Update();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }

            }
        }

        private void ListView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            Int32 colIndex = Convert.ToInt32(e.Column.ToString());
            lvPlugins.Columns[colIndex].Text = "new text";
        }

        private void ListView_Resize(object sender, EventArgs e)
        {
            bool scrollbar_visible = false;
            if ((lvPlugins.Items.Count > 0) && (ActiveForm != null))
                scrollbar_visible = ((lvPlugins.GetItemRect(0).Height * (lvPlugins.Items.Count + 2)) > lvPlugins.Bounds.Height);

            lvPlugins.BeginUpdate();
            foreach (ColumnHeader col in lvPlugins.Columns)
            {
                if (scrollbar_visible)
                    col.Width = (lvPlugins.Width / lvPlugins.Columns.Count) - (System.Windows.Forms.SystemInformation.VerticalScrollBarWidth / lvPlugins.Columns.Count) - 2;
                else
                    col.Width = (lvPlugins.Width / lvPlugins.Columns.Count) - 2;
            }
            lvPlugins.EndUpdate();
        }
    }
}
