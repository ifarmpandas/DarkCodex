﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ionic.Zip;

namespace BlueprintPurge
{
    public partial class BlueprintPurge : Form
    {
        private string pathSave;
        private ZipFile zip;
        private HashSet<Guid> blueprints = new();
        private BindingList<PurgeRange> purges = new();
        private BindingSource binding = new();

        public BlueprintPurge()
        {
            InitializeComponent();

            purges.RaiseListChangedEvents = true;
            binding.DataSource = purges;
            dataGridView1.DataSource = binding;

            //DataGridViewCheckBoxColumn { Name = Enabled, Index = 0 }
            dataGridView1.Columns[0].ReadOnly = false;
            dataGridView1.Columns[0].Width = 55;
            dataGridView1.Columns[0].Resizable = DataGridViewTriState.False;

            //DataGridViewTextBoxColumn { Name = Guid, Index = 1 }
            dataGridView1.Columns[1].ReadOnly = true;
            dataGridView1.Columns[1].Width = 225;
            dataGridView1.Columns[1].Resizable = DataGridViewTriState.False;

            //DataGridViewTextBoxColumn { Name = Type, Index = 2 }
            dataGridView1.Columns[2].ReadOnly = true;
            dataGridView1.Columns[2].Width = 200;

            //DataGridViewTextBoxColumn { Name = File, Index = 3 }
            dataGridView1.Columns[3].ReadOnly = true;
            dataGridView1.Columns[3].Width = 100;

            //DataGridViewTextBoxColumn { Name = Start, Index = 4 }
            dataGridView1.Columns[4].ReadOnly = true;
            dataGridView1.Columns[4].Width = 64;

            //DataGridViewTextBoxColumn { Name = End, Index = 5 }
            dataGridView1.Columns[5].ReadOnly = true;
            dataGridView1.Columns[5].Width = 64;

            //DataGridViewCheckBoxColumn { Name = Null, Index = 6 }
            dataGridView1.Columns[6].ReadOnly = true;
            dataGridView1.Columns[6].Width = 35;
            dataGridView1.Columns[6].Resizable = DataGridViewTriState.False;

            //DataGridViewTextBoxColumn { Name = Ref, Index = 7 }
            dataGridView1.Columns[7].ReadOnly = true;
            dataGridView1.Columns[7].Width = 48;

            //DataGridViewTextBoxColumn { Name = Peek, Index = 8 }
            dataGridView1.Columns[8].ReadOnly = true;
            dataGridView1.Columns[8].Width = 100;
            dataGridView1.Columns[8].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;


            radioWhitelist.Enabled = false; // todo: implement whitelist mode -> add type check
        }

        private void ButtonBrowse_Click(object sender, EventArgs e)
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            path = path.Substring(0, path.LastIndexOf("Roaming")) + "LocalLow";

            openFileDialogSave.InitialDirectory = Path.Combine(path, "Owlcat Games", "Pathfinder Wrath Of The Righteous", "Saved Games");
            if (openFileDialogSave.ShowDialog(this) == DialogResult.OK)
                textBoxSavePath.Text = openFileDialogSave.FileName;
        }

        private void ButtonSearch_Click(object sender, EventArgs e)
        {
            try
            {
                Clear();

                // check target save exists
                pathSave = textBoxSavePath.Text;
                if (pathSave == null || pathSave == "" || !File.Exists(pathSave))
                {
                    MessageBox.Show("File does not exist", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // read blueprints; or if empty, read from file
                string bps = textBoxBlueprints.Text;
                if (bps == "")
                {
                    if (openFileDialogBlueprints.ShowDialog(this) != DialogResult.OK)
                        return;
                    if (!File.Exists(openFileDialogBlueprints.FileName))
                        return;
                    bps = File.ReadAllText(openFileDialogBlueprints.FileName);
                }

                //parse blueprints
                foreach (var bp in bps.Split('\n', '\t', ' ', ';', ','))
                    if (Guid.TryParse(bp, out var guid))
                        blueprints.Add(guid);
                if (blueprints.Count == 0)
                {
                    MessageBox.Show("Could not parse any blueprints", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // open save file
                zip = ZipFile.Read(pathSave);
                foreach (var entry in zip.Entries.ToArray())
                {
                    if (!entry.FileName.EndsWith(".json"))
                        continue;

                    Debug.WriteLine(entry.FileName);
                    Search(entry);
                }
                Cleanup();
                //zip.Save(pathSave + ".TEST.zks");
                buttonPurge.Enabled = true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void Search(ZipEntry entry)
        {
            var sw = new MemoryStream();
            entry.Extract(sw);
            byte[] data = sw.ToArray();

            char last = default;                 // escape detection
            char last2 = default;                // escape detection
            bool isQuote = false;                // true while 'c' is inside a quote
            bool isId = false;                   // true if last quote was "$id"
            var lastId = new Dictionary<int, string>(); // value of last id by depth
            bool isRef = false;                  // true if last quote was "$ref"
            var refs = new HashSet<string>();    // ref ids by detected segments
            var sb = new StringBuilder();        // stringbuilder used for quote reconstruction
            var stack = new Stack<int>();        // stack of '{' indexes which points to the segment start
            var depth = new Stack<int>();        // stack of 'stack'-depth which so we can distinguish nested segments
            var purge = new Stack<PurgeRange>(); // stack of unfinished segments waiting for their end point (which is at '}' and the correct depth)
            for (int i = 0; i < data.Length; i++)
            {
                char c = (char)data[i];

                if (last == '\\' && last2 != '\\') // filter escape sequence
                {
                    goto end;
                }
                if (c == '\"') // detect quotes
                {
                    isQuote = !isQuote;
                    if (!isQuote) // if end of quote
                    {
                        string quote = sb.ToString();

                        if (isId)
                            //lastId = quote;
                            lastId[stack.Count] = quote;

                        else if (isRef && refs.Contains(quote))
                        {
                            var x = purges.First(f => f.Ref == quote && f.File == entry.FileName);
                            purge.Push(new PurgeRange { File = entry.FileName, Guid = x.Guid, Data = data, Ref = quote, Type = "$ref" });
                            depth.Push(stack.Count);
                        }

                        else if (Guid.TryParse(quote, out var guid)
                           && this.blueprints.Contains(guid) ^ this.radioWhitelist.Checked)
                        {
                            if (lastId.TryGetValue(stack.Count, out var id))
                                refs.Add(id);
                            purge.Push(new PurgeRange { File = entry.FileName, Guid = guid, Data = data, Ref = id! });
                            depth.Push(stack.Count);
                        }

                        isId = quote == "$id";
                        isRef = quote == "$ref";
                        sb.Clear();
                    }
                    goto end;
                }
                if (isQuote) // remember quote
                {
                    sb.Append(c);
                    goto end;
                }
                if (c == '{')
                {
                    stack.Push(i);
                    goto end;
                }
                if (c == '}')
                {
                    if (purge.Count > 0 && stack.Count == depth.Peek())
                    {
                        depth.Pop();
                        var p = purge.Pop();
                        p.Start = stack.Pop();
                        p.End = i;
                        p.Peek = Encoding.Default.GetString(data[p.Start..(p.End + 1)]);
                        purges.Add(p);
                    }
                    else
                        stack.Pop();
                    goto end;
                }

            end:
                last2 = last;
                last = c;
            }

            if (purge.Count > 0)
                throw new FormatException("bad json formatting, leftover purge");
            if (stack.Count > 0)
                throw new FormatException("bad json formatting, stack not 0");
        }

        private Regex rxType = new("([A-z\\.]*), Assembly-CSharp\"");
        private Regex rxType2 = new("([A-z]*), Assembly-CSharp");
        private Regex rxId = new("\"\\$id\"[\\s:]*\"([0-9]+)\"");
        private void Cleanup()
        {
            int lastStart = 0;
            int lastEnd = 0;
            for (int i = purges.Count - 1; i >= 0; i--)
            {
                var purge = purges[i];

                // remove entries that are contained by its predecessor
                if (i > 0)
                {
                    lastStart = purges[i - 1].Start;
                    lastEnd = purges[i - 1].End;
                    if (purge.Start <= lastStart && lastEnd <= purge.End)
                    {
                        purges.RemoveAt(i - 1);
                        continue;
                    }
                }

                // should replace with null instead
                for (int j = purge.Start - 1; j >= 0; j--)
                {
                    char c = (char)purge.Data[j];
                    if (char.IsWhiteSpace(c))
                        continue;
                    if (c == ':')
                        purge.Null = true;
                    break;
                }

                // include tailing comma
                for (int j = purge.End + 1; j < purge.Data.Length; j++)
                {
                    char c = (char)purge.Data[j];
                    if (char.IsWhiteSpace(c))
                        continue;
                    if (c == ',')
                        purge.End = j;
                    break;
                }

                // parse Type
                if (purge.Type == null)
                {
                    var match = rxType2.Match(purge.Peek);
                    if (match.Success)
                        purge.Type = match.Groups[1].Value;
                }

                purge.Enabled = true;
            }

            // TODO: sanity json check
        }

        private void Clear()
        {
            zip?.Dispose();
            zip = null;
            blueprints.Clear();
            purges.Clear();
            buttonPurge.Enabled = false;
        }

        private Regex rxHeader = new("(\"AreaNameOverride\":).*?([,\\{\\[\\]\\}])");
        private void UpdateHeader()
        {
            var sw = new MemoryStream();
            zip["header.json"].Extract(sw);

            string header = Encoding.Default.GetString(sw.ToArray());
            header = rxHeader.Replace(header, "$1\"PURGED!\"$2");
            zip.UpdateEntry("header.json", header);
        }

        private void ButtonPurge_Click(object sender, EventArgs e)
        {
            if (zip == null)
                return;

            if (MessageBox.Show("You are about to purge your save file. This might corrupt your save without you noticing immediately. If this is an auto save, make a manual backup. If this is a manual save, do not delete the original.\nThis process is dangerous. Use at own risk and don't blame me.", "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
                return;

            // process purge entries
            var edited = new Dictionary<string, byte[]>();
            int count = 0;
            foreach (var purge in purges)
            {
                if (!purge.Enabled)
                    continue;

                count++;
                edited[purge.File] = purge.Data;

                for (int i = purge.Start; i <= purge.End; i++)
                    purge.Data[i] = (byte)' ';

                if (purge.Null)
                {
                    int i = purge.Start;
                    purge.Data[i++] = (byte)'n';
                    purge.Data[i++] = (byte)'u';
                    purge.Data[i++] = (byte)'l';
                    purge.Data[i++] = (byte)'l';
                }
            }

            // save changes and update header
            if (edited.Count > 0)
            {
                foreach (var (file, data) in edited)
                {
                    zip.UpdateEntry(file, data);
                }

                UpdateHeader();
                zip.Save(pathSave + ".purged.zks");
                MessageBox.Show($"Removed {count} entries in {edited.Count} files. Saved in new file.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("No match found. Canceled.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Clear();
        }

        private void ButtonHelp_Click(object sender, EventArgs e)
        {
            MessageBox.Show($"Warning: This is just a test version. It may create invalid save data which could immediately or during your playthrough fail.\n"
                + "This app will never override your original save. It will instead create a duplicate with the description 'PURGED!'.\n"
                + "How to use: Enter file path to save data you want to purge. List blueprint guids you want to purge from your save. Click 'Search'. "
                + "Review the entries in the list. You may deselect entries, but it is advised to instead change the blacklist. Click 'Purge Now!'.",

                "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);

            var list = (System.Collections.IList)dataGridView1.Columns;
            for (int i = 0; i < list.Count; i++)
            {
                var column = (DataGridViewColumn)list[i];
                Debug.WriteLine($"{i} : {column.Width}");
            }
        }
    }
}
