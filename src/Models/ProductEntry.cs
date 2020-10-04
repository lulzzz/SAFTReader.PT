﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAFT_Reader.Models
{
    public class ProductEntry
    {
        [Display(Name = "Tipo")]
        public string ProductType { get; set; }
        [Display(Name = "ID")]
        public string ProductCode { get; set; }
        [Display(Name = "Descrição")]
        public string ProductDescription { get; set; }
        [Display(Name = "Código")]
        public string ProductNumberCode { get; set; }

        [DataType(DataType.Currency)]
        [Display(Name = "Total Créd.")]
        public float TotalCreditAmmount { get; set; }
        [DataType(DataType.Currency)]
        [Display(Name = "Total Déb.")]
        public float TotalDebitAmmount { get; set; }
    }
}
