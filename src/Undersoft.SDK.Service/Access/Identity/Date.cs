// *************************************************
//   Copyright (c) Undersoft. All Rights Reserved.
//   Licensed under the MIT License. 
//   author: Dariusz Hanc
//   email: dh@undersoft.pl
//   library: Undersoft.SCC.Service
// *************************************************

using Undersoft.SDK.Rubrics.Attributes;
using Undersoft.SDK.Service.Data.Contract;
using Undersoft.SDK.Service.Data.Object;

namespace Undersoft.SDK.Service.Access.Identity
{
    public class Date : DataObject, IContract
    {
        [VisibleRubric]
        public DateType Type { get; set; }

        [VisibleRubric]
        public DateTime? Value { get; set; } = DateTime.Parse("01.01.1990");
    }
}