﻿using System;
using System.ComponentModel.DataAnnotations;

namespace NonFactors.Mvc.Lookup.Tests.Objects
{
    public class TestModel
    {
        [LookupColumn(-3, Hidden = true)]
        public String Id { get; set; }

        [LookupColumn(8)]
        [Display(Name = "Count's value", ShortName = "Value")]
        public Int32 Count { get; set; }

        [LookupColumn]
        public String Value { get; set; }

        public String ParentId { get; set; }

        [Display(Name = "Date")]
        [LookupColumn(3, Format = "{0:d}")]
        public DateTime Date { get; set; }

        public String[] Values { get; set; }
    }
}
