﻿using Programatica.Saft.Models;
using SAFT_Reader.Adapter;
using Syncfusion.Data;
using Syncfusion.WinForms.Controls;
using Syncfusion.WinForms.DataGrid;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SAFT_Reader.UI
{
    public partial class AttachedFilesFormDialog : SfForm
    {
        public SfDataGrid DataGrid { get; set; }

        private readonly IFileStreamAdapter _fileStreamAdapter;
        private readonly IXmlSerializerAdapter _xmlSerializerAdapter;

        public AttachedFilesFormDialog(IFileStreamAdapter fileStreamAdapter, IXmlSerializerAdapter xmlSerializerAdapter)
        {
            _fileStreamAdapter = fileStreamAdapter;
            _xmlSerializerAdapter = xmlSerializerAdapter;

            InitializeComponent();
            InitializeView();


        }

        private void InitializeView()
        {
            this.txtFilePath.Text = Globals.AttachedFiles
                                            .Where(x => x.IsPrincipal == true)
                                            .FirstOrDefault()
                                            .FilePath;
            RefreshGridView();
        }

        private AuditFile OpenFile(string path)
        {
            var model = _fileStreamAdapter.Read(path);
            return _xmlSerializerAdapter.ConvertXml<AuditFile>(model);
        }

        private void cmdOK_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;

            Cursor.Current = Cursors.Default;
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            Globals.AuditFile = MergeAudits();
            this.DialogResult = DialogResult.OK;
            Cursor.Current = Cursors.Default;
        }

        private void RefreshGridView()
        {
            this.listView1.Items.Clear();
            foreach (var file in Globals.AttachedFiles.Where(x => x.IsPrincipal == false))
            {
                var i = listView1.Items.Add(file.FilePath);
                i.SubItems.Add(file.AuditFile.Header.StartDate);
                i.SubItems.Add(file.AuditFile.Header.EndDate);
            }
            if (Globals.AttachedFiles.Count > 0) { cmdDelete.Enabled = true; }
        }

        public AuditFile MergeAudits()
        {
            AuditFile audit = OpenFile(Globals.AttachedFiles
                                                .Where(x => x.IsPrincipal == true)
                                                .FirstOrDefault().FilePath);

            foreach (var file in Globals.AttachedFiles.Where(x => x.IsPrincipal == false))
            {
                var subaudit = OpenFile(file.FilePath);

                audit.MasterFiles.Customer = audit.MasterFiles.Customer
                                                                .Concat(subaudit.MasterFiles.Customer)
                                                                .ToList();

                audit.MasterFiles.TaxTable.TaxTableEntry = audit.MasterFiles.TaxTable.TaxTableEntry
                                                .Concat(subaudit.MasterFiles.TaxTable.TaxTableEntry)
                                                .ToList();

                audit.MasterFiles.Product = audit.MasterFiles.Product
                                .Concat(subaudit.MasterFiles.Product)
                                .ToList();

                audit.SourceDocuments.SalesInvoices.Invoice = audit.SourceDocuments.SalesInvoices.Invoice
                                                            .Concat(subaudit.SourceDocuments.SalesInvoices.Invoice)
                                                            .ToList();

                audit.SourceDocuments.SalesInvoices.TotalCredit = (float.Parse(audit.SourceDocuments.SalesInvoices.TotalCredit.Replace(".", ",")) +
                    float.Parse(subaudit.SourceDocuments.SalesInvoices.TotalCredit.Replace(".", ","))).ToString();

                audit.SourceDocuments.SalesInvoices.TotalDebit = (float.Parse(audit.SourceDocuments.SalesInvoices.TotalDebit.Replace(".", ",")) +
                    float.Parse(subaudit.SourceDocuments.SalesInvoices.TotalDebit.Replace(".", ","))).ToString();

                audit.SourceDocuments.SalesInvoices.NumberOfEntries = (int.Parse(audit.SourceDocuments.SalesInvoices.NumberOfEntries) +
                    int.Parse(subaudit.SourceDocuments.SalesInvoices.NumberOfEntries)).ToString();
            }

            return audit;
        }

        private void cmdAdd_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                var subaudit = OpenFile(openFileDialog1.FileName);

                if (ValidateFile(subaudit) == true)
                {

                    Globals.AttachedFiles.Add(new AttachedFile
                    {
                        ID = Guid.NewGuid(),
                        AuditFile = subaudit,
                        FilePath = openFileDialog1.FileName,
                        IsPrincipal = false
                    });

                    RefreshGridView();
                }
                else
                {
                    MessageBox.Show("Ocorreu um erro ao abrir o ficheiro Saft-PT. \n\r" +
                        "Garanta que se trata de um ficheiro válido, no formato 1.04_01 " +
                        "e que corresponde à mesma empresa do ficheiro principal",
                        "Erro ao abrir ficheiro Saft-PT",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            Cursor.Current = Cursors.Default;
        }

        private bool ValidateFile(AuditFile b)
        {
            bool r = true;
            var a = Globals.AttachedFiles.Where(x => x.IsPrincipal == true).FirstOrDefault().AuditFile;
              if (a.Header.CompanyID != b.Header.CompanyID)
            {
                r = false;
            }
            return r;
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            var itemToRemove = listView1.SelectedItems[0];
            if (itemToRemove != null)
            {
                var item = Globals.AttachedFiles.Where(x => x.FilePath.Equals(itemToRemove.Text)).FirstOrDefault();
                Globals.AttachedFiles.Remove(item);
            }
            RefreshGridView();
            Cursor.Current = Cursors.Default;
        }
    }
}
