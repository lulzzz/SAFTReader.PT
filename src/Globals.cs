﻿using Programatica.Saft.Models;
using SAFT_Reader.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SAFT_Reader
{
    public static class Globals
    {
        public static AuditFile AuditFile { get; set; }
        public static string Filepath { get; set; }

        public static string VersionLabel
        {
            get
            {
                if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)
                {
                    Version ver = System.Deployment.Application.ApplicationDeployment.CurrentDeployment.CurrentVersion;
                    return string.Format("Versão: {0}.{1}.{2}.{3} (NetworkDeployed)", 
                        ver.Major, ver.Minor, ver.Build, ver.Revision, 
                        Assembly.GetEntryAssembly().GetName().Name);
                }
                else
                {
                    var ver = Assembly.GetExecutingAssembly().GetName().Version;
                    return string.Format("Versão: {0}.{1}.{2}.{3} (Debug)", 
                        ver.Major, ver.Minor, ver.Build, ver.Revision, 
                        Assembly.GetEntryAssembly().GetName().Name);
                }
            }
        }

        public static List<InvoiceLine> LoadInvoiceLines()
        {
            var invoiceLines = new List<InvoiceLine>();
            var audit = Globals.AuditFile;

            var invoices = audit.SourceDocuments
                            .SalesInvoices
                            .Invoice
                            .Where(x => x.DocumentStatus.InvoiceStatus.Equals("N"));

            foreach (var invoice in invoices)
            {
                foreach (var line in invoice.Line)
                {
                    {
                        var tp = float.Parse(line.Tax.TaxPercentage.Replace(".", ","));
                        var invoiceLine = new InvoiceLine
                        {
                            InvoiceNo = invoice.InvoiceNo,
                            InvoiceDate = invoice.InvoiceDate,
                            InvoiceType = invoice.InvoiceType,
                            CustomerTaxID = audit.MasterFiles
                                                .Customer
                                                .Where(x => x.CustomerID.Equals(invoice.CustomerID))
                                                .FirstOrDefault()
                                                .CustomerTaxID,
                            CompanyName = audit.MasterFiles
                                                .Customer
                                                .Where(x => x.CustomerID.Equals(invoice.CustomerID))
                                                .FirstOrDefault()
                                                .CompanyName,
                            LineNumber = line.LineNumber,
                            ProductDescription = line.ProductDescription,
                            Quantity = float.Parse(line.Quantity.Replace(".", ",")),
                            UnitPrice = float.Parse(line.UnitPrice.Replace(".", ",")),
                            TaxCode = line.Tax.TaxCode,
                            TaxPercentage = tp
                        };
                        if (line.CreditAmount != null)
                        {
                            var ca = float.Parse(line.CreditAmount.Replace(".", ","));
                            invoiceLine.CreditAmount = ca;
                            invoiceLine.TaxPayable = ca * (tp / 100);
                        }
                        if (line.DebitAmount != null)
                        {
                            var da = float.Parse(line.DebitAmount.Replace(".", ","));
                            invoiceLine.DebitAmount = da;
                            invoiceLine.TaxPayable = da * (tp / 100);
                        }
                        invoiceLines.Add(invoiceLine);
                    }
                }
            }

            return invoiceLines;
        }

        public static List<InvoiceEntry> LoadInvoiLoadDocuments()
        {
            var audit = Globals.AuditFile;
            return audit
                    .SourceDocuments
                    .SalesInvoices
                    .Invoice
                    .Select(i => new InvoiceEntry
                    {
                        InvoiceNo = i.InvoiceNo,
                        Period = i.Period,
                        InvoiceDate = i.InvoiceDate,
                        InvoiceType = i.InvoiceType,
                        SourceID = i.SourceID,
                        CustomerID = i.CustomerID,
                        CompanyName = audit.MasterFiles
                                                .Customer
                                                .Where(x => x.CustomerID.Equals(i.CustomerID))
                                                .FirstOrDefault()
                                                .CompanyName,
                        InvoiceStatus = i.DocumentStatus.InvoiceStatus,
                        TaxPayable = float.Parse(i.DocumentTotals.TaxPayable.Replace(".", ",")),
                        NetTotal = float.Parse(i.DocumentTotals.NetTotal.Replace(".", ",")),
                        GrossTotal = float.Parse(i.DocumentTotals.GrossTotal.Replace(".", ","))
                    }).ToList();
        }

        public static List<TaxTableEntryTotal> LoadTaxTableEntryTotals(List<InvoiceLine> invoiceLines)
        {
            var audit = Globals.AuditFile;

            var totals = invoiceLines
                .GroupBy(g => new { g.TaxCode, g.TaxPercentage })
                .Select(cl => new TaxTableEntryTotal
                {
                    TaxCode = cl.First().TaxCode,
                    TaxDescription = audit
                                        .MasterFiles
                                        .TaxTable
                                        .TaxTableEntry
                                        .Where(x => x.TaxCode.Equals(cl.First().TaxCode))
                                        .FirstOrDefault()
                                        .Description,
                    TaxPercentage = cl.First().TaxPercentage,
                    CreditAmount = cl.Sum(c => c.CreditAmount),
                    DebitAmount = cl.Sum(d => d.DebitAmount),
                    CreditTaxPayable = cl.Sum(c => c.CreditAmount) * (cl.First().TaxPercentage / 100),
                    DebitTaxPayable = cl.Sum(d => d.DebitAmount) * (cl.First().TaxPercentage / 100)
                }).ToList();

            return totals;
        }
    }
}
