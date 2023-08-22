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
        ContextMenu cmPlugins;
        ListViewItem heldDownItem;
        Point heldDownPoint;
        int columnLastSorted = 0;

        DataSet dsPlugins = new DataSet();
        DataView dvActivePlugins;
        string strPluginTable_Schema = "";

        List<Tuple<string, string, IHandler>> fileHandlers = new List<Tuple<string, string, IHandler>>();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Data_Init();

            ListView_Init();

            ContextMenu_Init();

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

        }

        #region DATA
        private void Data_Init()
        {
            DataTable dtActivePlugins = new DataTable("ActivePlugins");
            dtActivePlugins.Columns.Add("Id", typeof(Guid));
            dtActivePlugins.Columns.Add("Priority", typeof(int));
            dtActivePlugins.Columns.Add("FullPath", typeof(string));
            dtActivePlugins.Columns.Add("PluginName", typeof(string));
            dtActivePlugins.Columns.Add("DateModified", typeof(DateTime));
            dtActivePlugins.Columns.Add("Size", typeof(int));
            dtActivePlugins.Columns.Add("Author", typeof(string));
            dtActivePlugins.PrimaryKey = new[] { dtActivePlugins.Columns["FullPath"] };

            dsPlugins.Tables.Add(dtActivePlugins);

            StringWriter sWriter = new StringWriter();
            dtActivePlugins.WriteXmlSchema(sWriter);
            strPluginTable_Schema = sWriter.GetStringBuilder().ToString();

            // Create a LinqDataView from a LINQ to DataSet query and bind it 
            // to the Windows forms control.
            // TEST
            /*EnumerableRowCollection<DataRow> pluginsQuery = from row in dtActivePlugins.AsEnumerable()
                                                            where row.Field<string>("PluginName") != null
                                                            orderby row.Field<string>("PluginName")
                                                            select row;*/

            EnumerableRowCollection<DataRow> pluginsQuery = dtActivePlugins.AsEnumerable()
                .Where<DataRow>(x => x.Field<string>("PluginName") != null)
                .OrderBy(y => y.Field<string>("PluginName"))
                .Select(y => y);

            dvActivePlugins = pluginsQuery.AsDataView();

            fileHandlers = new List<Tuple<string, string, IHandler>>() {
                Tuple.Create( ".esp", "Plugin", (IHandler)new PluginHandler()),
                Tuple.Create( ".esm", "Master", (IHandler)new PluginHandler()),
                Tuple.Create( ".ess", "Save", (IHandler)new SaveHandler())
            };
        }

        private EnumerableRowCollection<DataRow> Get_Rows(List<string> Ids)
        {
            return dsPlugins.Tables["ActivePlugins"].AsEnumerable()
                .Where(x => Ids.Contains(x.Field<object>("Id").ToString()))
                .Select(y => y);
        }
        #endregion

        #region LISTVIEW
        void ListView_Init()
        {
            lvPlugins = new ListView
            {
                Name = "ListView_Plugins",
                Location = new System.Drawing.Point(0, 0),
                Size = new System.Drawing.Size(245, 200),
                GridLines = true,
                AllowColumnReorder = true,
                LabelEdit = false,
                FullRowSelect = true,
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
            lvPlugins.MouseUp += new MouseEventHandler(ListView_MouseClick);

            foreach (DataColumn dc in dvActivePlugins.ToTable(true, "PluginName", "DateModified", "Author", "Size", "Priority", "Id").Columns)
                lvPlugins.Columns.Add(dc.Caption);

            lvPlugins.Columns[5].Dispose(); // hide Id column

            typeof(Control).GetProperty("DoubleBuffered",
                                         System.Reflection.BindingFlags.NonPublic |
                                         System.Reflection.BindingFlags.Instance)
                           .SetValue(lvPlugins, true, null);

            ListView_Update();
        }

        void ListView_Update()
        {
            int topItemIndex = 0;
            try
            {
                topItemIndex = lvPlugins.TopItem.Index;
            }
            catch (Exception ex)
            { }

            lvPlugins.BeginUpdate();
            lvPlugins.Items.Clear();
            foreach (DataRow dr in dvActivePlugins.ToTable(true, "PluginName", "DateModified", "Author", "Size", "Priority", "Id").Rows)
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

            try
            {
                lvPlugins.TopItem = lvPlugins.Items[topItemIndex];
            }
            catch (Exception ex)
            { }
        }

        void ListView_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        void ListView_DragDrop(object sender, DragEventArgs e)
        {
            // Handle FileDrop data.

            DataTable dtActivePlugins = dsPlugins.Tables["ActivePlugins"];
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                try
                {
                    files = files.Where(x => fileHandlers.Select(y => y.Item1.ToString()).Contains(x.Substring(x.Length - 4, 4))).ToArray();
                    foreach(string file in files)
                    {

                        DataTable imported_files = fileHandlers.Where(x => file.Substring(file.Length - 4, 4) == x.Item1).First().Item3.Import(file, strPluginTable_Schema);
                        foreach (DataRow dr in imported_files.Rows)
                            if (!dtActivePlugins.Rows.Contains(dr["FullPath"]))
                                dtActivePlugins.ImportRow(dr);
                    }

                    dtActivePlugins.AcceptChanges();
                    ListView_Update();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

            }
        }

        private void ListView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            Int32 colIndex = Convert.ToInt32(e.Column.ToString());
            EnumerableRowCollection<DataRow> pluginsQuery;
            if (colIndex != columnLastSorted)
            {
                pluginsQuery = dsPlugins.Tables["ActivePlugins"].AsEnumerable()
                    .OrderBy(y => y.Field<object>(lvPlugins.Columns[colIndex].Text))
                    .Select(y => y);
                columnLastSorted = colIndex;
            }
            else
            {
                pluginsQuery = dsPlugins.Tables["ActivePlugins"].AsEnumerable()
                    .OrderByDescending(y => y.Field<object>(lvPlugins.Columns[colIndex].Text))
                    .Select(y => y);
                columnLastSorted = 0;
            }

            dvActivePlugins = pluginsQuery.AsDataView();
            ListView_Update();
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

        private void ListView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (lvPlugins.FocusedItem != null && lvPlugins.FocusedItem.Bounds.Contains(e.Location) == true)
                {
                    cmPlugins.Show(lvPlugins, new Point(e.X, e.Y));
                }
            }
        }
        #endregion

        #region CONTEXT MENU
        private void ContextMenu_Init()
        {
            cmPlugins = new ContextMenu();
            List<Tuple<string, Action<object, EventArgs>>> menu_items = new List<Tuple<string, Action<object, EventArgs>>>
            {
                Tuple.Create("Description", new Action<object, EventArgs>(Context_Description)),
                Tuple.Create("Set to Load Last", new Action<object, EventArgs>(Context_Description)),
                Tuple.Create("Remove from Active Plugins", new Action<object, EventArgs>(Context_Remove)),
                Tuple.Create("Uninstall Selected", new Action<object, EventArgs>(Context_Description)),
                Tuple.Create("Clear Active Plugin List", new Action<object, EventArgs>(Context_RemoveAll)),
                Tuple.Create("-", new Action<object, EventArgs>(Context_Description)),
                Tuple.Create("Find Multiple Versions", new Action<object, EventArgs>(Context_Description)),
                Tuple.Create("Find String in Plugins", new Action<object, EventArgs>(Context_FindString)),
                Tuple.Create("-", new Action<object, EventArgs>(Context_Description)),
                Tuple.Create("Scan for Missing Resources", new Action<object, EventArgs>(Context_Description)),
                Tuple.Create("Scan for Shared Resources", new Action<object, EventArgs>(Context_Description)),
                Tuple.Create("Compile for Distribution", new Action<object, EventArgs>(Context_Description)),
                Tuple.Create("Export Object Definitions", new Action<object, EventArgs>(Context_Description)),
                Tuple.Create("Merge Leveled Lists", new Action<object, EventArgs>(Context_Description)),
            };

            foreach (Tuple<string, Action<object, EventArgs>> menu_item in menu_items)
            {

                MenuItem menuItem = new MenuItem(menu_item.Item1);
                menuItem.Click += delegate (object sender2, EventArgs e2) {
                    menu_item.Item2(lvPlugins, e2);
                };
                cmPlugins.MenuItems.Add(menuItem);
            }
        }

        private void Context_Remove(object sender, EventArgs e)
        {
            ListView ListViewControl = sender as ListView;
            List<string> Ids = new List<string>();

            foreach (ListViewItem tmpLstView in ListViewControl.SelectedItems)
                Ids.Add(tmpLstView.SubItems[5].Text); // hidden column


            List<DataRow> toRemove = Get_Rows(Ids).ToList();

            foreach (DataRow dr in toRemove)
                dsPlugins.Tables["ActivePlugins"].Rows.Remove(dr);

            ListView_Update();
        }

        private void Context_RemoveAll(object sender, EventArgs e)
        {
            ListView ListViewControl = sender as ListView;
            List<string> Ids = new List<string>();

            foreach (ListViewItem tmpLstView in ListViewControl.Items)
                Ids.Add(tmpLstView.SubItems[5].Text); // hidden column


            List<DataRow> toRemove = Get_Rows(Ids).ToList();

            foreach (DataRow dr in toRemove)
                dsPlugins.Tables["ActivePlugins"].Rows.Remove(dr);

            ListView_Update();
        }

        private void Context_Description(object sender, EventArgs e)
        {
            ListView ListViewControl = sender as ListView;
            List<string> Ids = new List<string>();

            foreach (ListViewItem tmpLstView in ListViewControl.SelectedItems)
                Ids.Add(tmpLstView.SubItems[5].Text); // hidden column

            foreach (DataRow dr in Get_Rows(Ids))
                MessageBox.Show(dr.ItemArray[2].ToString());


        }

        private void Context_FindString(object sender, EventArgs e)
        {
            using (Prompt prompt = new Prompt(@"Enter string (case sensitive) to find in all active plugins", "Find String in Plugins"))
            {
                string result = prompt.Result;
            }
        }
        #endregion
    }
}
